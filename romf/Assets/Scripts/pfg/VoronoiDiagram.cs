using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace vDiagramGen
{
    [System.Serializable]
    public class VoronoiDiagram
    {
        // Bounds of the Voronoi Diagram
        public Rect Bounds;

        // Generated sites.  Filled after GenerateSites() is called
        public Dictionary<int, VoronoiDiagramGeneratedSite> GeneratedSites;

        private readonly List<VoronoiDiagramSite> _originalSites;

        // Stored added points as Sites that are currently being processed.  Ordered lexigraphically by y and then x
        private readonly List<VoronoiDiagramSite> _sites;

        // Stores the bottom most site when running GenerateEdges
        private VoronoiDiagramSite _bottomMostSite;

        // Stores the current site index when running GenerateEdges
        private int _currentSiteIndex;

        // Stores the minimum values of the points in site array.
        private Vector2 _minValues;

        // Stores the delta values of the minimum and maximum values.
        private Vector2 _deltaValues;

        public VoronoiDiagram()
        {
            Bounds = new Rect();
            GeneratedSites = new Dictionary<int, VoronoiDiagramGeneratedSite>();
            _originalSites = new List<VoronoiDiagramSite>();
            _sites = new List<VoronoiDiagramSite>();
        }

        public VoronoiDiagram(Rect bounds)
        {
            Bounds = bounds;
            GeneratedSites = new Dictionary<int, VoronoiDiagramGeneratedSite>();
            _originalSites = new List<VoronoiDiagramSite>();
            _sites = new List<VoronoiDiagramSite>();
        }

        public void AddSites(List<VoronoiDiagramSite> points)
        {
            foreach(VoronoiDiagramSite point in points)
            {
                _originalSites.Add(point);
            }
        }

        public void GenerateSites()
        {
            _sites.Clear();
            foreach(VoronoiDiagramSite site in _originalSites)
            {
                _sites.Add(new VoronoiDiagramSite(_sites.Count, site));
            }
            SortSitesAndSetValues();

            // Fortune's Algorithm
            int numGeneratedEdges = 0;
            int numGeneratedVertices = 0;
            _currentSiteIndex = 0;

            var priorityQueue = new VoronoiDiagramPriorityQueue(_sites.Count, _minValues, _deltaValues);
            var edgeList = new VoronoiDiagramEdgeList(_sites.Count, _minValues, _deltaValues);

            Vector2 currentIntersectionStar = Vector2.zero;
            VoronoiDiagramSite currentSite;

            var generatedEdges = new List<VoronoiDiagramEdge>();

            bool done = false;
            _bottomMostSite = GetNextSite();
            currentSite = GetNextSite();
            while(!done)
            {
                if(!priorityQueue.IsEmpty())
                {
                    currentIntersectionStar = priorityQueue.GetMinimumBucketFirstPoint();
                }

                VoronoiDiagramSite bottomSite;
                VoronoiDiagramHalfEdge bisector;
                VoronoiDiagramHalfEdge rightBound;
                VoronoiDiagramHalfEdge leftBound;
                VoronoiDiagramVertex vertex;
                VoronoiDiagramEdge edge;
                if(
                    currentSite != null &&
                    (
                        priorityQueue.IsEmpty() ||
                        currentSite.Coordinate.y < currentIntersectionStar.y ||
                        (
                            currentSite.Coordinate.y.IsAlmostEqualTo(currentIntersectionStar.y) &&
                            currentSite.Coordinate.x < currentIntersectionStar.x
                        )
                    )
                )
                {
                    // Current processed site is the smallest
                    leftBound = edgeList.GetLeftBoundFrom(currentSite.Coordinate);
                    rightBound = leftBound.EdgeListRight;
                    bottomSite = GetRightRegion(leftBound);

                    edge = VoronoiDiagramEdge.Bisect(bottomSite, currentSite);
                    edge.Index = numGeneratedEdges;
                    numGeneratedEdges++;

                    generatedEdges.Add(edge);

                    bisector = new VoronoiDiagramHalfEdge(edge, VoronoiDiagramEdgeType.Left);
                    edgeList.Insert(leftBound, bisector);

                    vertex = VoronoiDiagramVertex.Intersect(leftBound, bisector);
                    if(vertex != null)
                    {
                        priorityQueue.Delete(leftBound);

                        leftBound.Vertex = vertex;
                        leftBound.StarY = vertex.Coordinate.y + currentSite.GetDistanceFrom(vertex);

                        priorityQueue.Insert(leftBound);
                    }

                    leftBound = bisector;
                    bisector = new VoronoiDiagramHalfEdge(edge, VoronoiDiagramEdgeType.Right);

                    edgeList.Insert(leftBound, bisector);

                    vertex = VoronoiDiagramVertex.Intersect(bisector, rightBound);
                    if(vertex != null)
                    {
                        bisector.Vertex = vertex;
                        bisector.StarY = vertex.Coordinate.y + currentSite.GetDistanceFrom(vertex);

                        priorityQueue.Insert(bisector);
                    }

                    currentSite = GetNextSite();
                }
                else if(priorityQueue.IsEmpty() == false)
                {
                    // Current intersection is the smallest
                    leftBound = priorityQueue.RemoveAndReturnMinimum();
                    VoronoiDiagramHalfEdge leftLeftBound = leftBound.EdgeListLeft;
                    rightBound = leftBound.EdgeListRight;
                    VoronoiDiagramHalfEdge rightRightBound = rightBound.EdgeListRight;
                    bottomSite = GetLeftRegion(leftBound);
                    VoronoiDiagramSite topSite = GetRightRegion(rightBound);

                    // These three sites define a Delaunay triangle
                    // Bottom, Top, EdgeList.GetRightRegion(rightBound);
                    // Debug.Log(string.Format("Delaunay triagnle: ({0}, {1}), ({2}, {3}), ({4}, {5})"),
                    //      bottomSite.Coordinate.x, bottomSite.Coordinate.y,
                    //      topSite.Coordinate.x, topSite.Coordinate.y,
                    //      edgeList.GetRightRegion(leftBound).Coordinate.x,
                    //      edgeList.GetRightRegion(leftBound).Coordinate.y);

                    var v = leftBound.Vertex;
                    v.Index = numGeneratedVertices;
                    numGeneratedVertices++;

                    leftBound.Edge.SetEndpoint(v, leftBound.EdgeType);
                    rightBound.Edge.SetEndpoint(v, rightBound.EdgeType);

                    edgeList.Delete(leftBound);

                    priorityQueue.Delete(rightBound);
                    edgeList.Delete(rightBound);

                    var edgeType = VoronoiDiagramEdgeType.Left;
                    if(bottomSite.Coordinate.y > topSite.Coordinate.y)
                    {
                        var tempSite = bottomSite;
                        bottomSite = topSite;
                        topSite = tempSite;
                        edgeType = VoronoiDiagramEdgeType.Right;
                    }

                    edge = VoronoiDiagramEdge.Bisect(bottomSite, topSite);
                    edge.Index = numGeneratedEdges;
                    numGeneratedEdges++;

                    generatedEdges.Add(edge);

                    bisector = new VoronoiDiagramHalfEdge(edge, edgeType);
                    edgeList.Insert(leftLeftBound, bisector);

                    edge.SetEndpoint(v,
                        edgeType == VoronoiDiagramEdgeType.Left
                            ? VoronoiDiagramEdgeType.Right
                            : VoronoiDiagramEdgeType.Left);

                    vertex = VoronoiDiagramVertex.Intersect(leftLeftBound, bisector);
                    if(vertex != null)
                    {
                        priorityQueue.Delete(leftLeftBound);

                        leftLeftBound.Vertex = vertex;
                        leftLeftBound.StarY = vertex.Coordinate.y + bottomSite.GetDistanceFrom(vertex);

                        priorityQueue.Insert(leftLeftBound);
                    }

                    vertex = VoronoiDiagramVertex.Intersect(bisector, rightRightBound);
                    if(vertex != null)
                    {
                        bisector.Vertex = vertex;
                        bisector.StarY = vertex.Coordinate.y + bottomSite.GetDistanceFrom(vertex);

                        priorityQueue.Insert(bisector);
                    }
                }
                else
                {
                    done = true;
                }
            }

            GeneratedSites.Clear();
            // Bound the edges of the diagram
            foreach(VoronoiDiagramEdge currentGeneratedEdge in generatedEdges)
            {
                currentGeneratedEdge.GenerateClippedEndPoints(Bounds);
            }

            foreach(VoronoiDiagramSite site in _sites)
            {
                try
                {
                    site.GenerateCentroid(Bounds);
                }
                catch(Exception)
                {
                    Debug.Log("Coordinate");
                    Debug.Log(site.Coordinate);
                    Debug.Log("End points:");
                    foreach(var edge in site.Edges)
                    {
                        Debug.Log(edge.LeftClippedEndPoint + " , " +  edge.RightClippedEndPoint);
                    }
                    throw;
                }
                
            }

            foreach(VoronoiDiagramSite site in _sites)
            {
                var generatedSite = new VoronoiDiagramGeneratedSite(site.Index, site.Coordinate, site.Centroid, new Color(), site.IsCorner, site.IsEdge);
                generatedSite.Vertices.AddRange(site.Vertices);
                generatedSite.SiteData = site.SiteData;

                foreach(VoronoiDiagramEdge siteEdge in site.Edges)
                {
                    // Only add edges that are visible
                    // Don't need to check the Right because they will both be float.MinValue
                    if(siteEdge.LeftClippedEndPoint == new Vector2(float.MinValue, float.MinValue))
                    {
                        continue;
                    }

                    generatedSite.Edges.Add(new VoronoiDiagramGeneratedEdge(siteEdge.Index,
                        siteEdge.LeftClippedEndPoint, siteEdge.RightClippedEndPoint));

                    if(siteEdge.LeftSite != null && !generatedSite.NeighborSites.Contains(siteEdge.LeftSite.Index))
                    {
                        generatedSite.NeighborSites.Add(siteEdge.LeftSite.Index);
                    }

                    if(siteEdge.RightSite != null && !generatedSite.NeighborSites.Contains(siteEdge.RightSite.Index))
                    {
                        generatedSite.NeighborSites.Add(siteEdge.RightSite.Index);
                    }
                }
                
                GeneratedSites.Add(generatedSite.Index, generatedSite);

                // Finished with the edges, remove the references so they can be removed at the end of the method
                site.Edges.Clear();
            }

            // // Clean up
            // _bottomMostSite = null;
            // _sites.Clear();

            // // Lloyd's Algorithm
            // foreach(KeyValuePair<int, VoronoiDiagramGeneratedSite> generatedSite in GeneratedSites)
            // {
            //     var centroidPoint = 
            //         new Vector2(
            //             Mathf.Clamp(generatedSite.Value.Centroid.x, 0, Bounds.width), 
            //             Mathf.Clamp(generatedSite.Value.Centroid.y, 0, Bounds.height));
            //     var newSite = new VoronoiDiagramSite(new Vector2(centroidPoint.x, centroidPoint.y), generatedSite.Value.SiteData);

            //     if(!_sites.Any(item => item.Coordinate.x.IsAlmostEqualTo(newSite.Coordinate.x) && item.Coordinate.y.IsAlmostEqualTo(newSite.Coordinate.y)))
            //     {
            //         _sites.Add(new VoronoiDiagramSite(_sites.Count, newSite));
            //     }
            // }
            //SortSitesAndSetValues();

        }

        public Color[] Get1DSampleArray()
        {
            var returnData = new Color[(int)Bounds.width * (int)Bounds.height];

            for (int i = 0; i < returnData.Length; i++)
            {
                returnData[i] = default(Color);
            }

            foreach (KeyValuePair<int, VoronoiDiagramGeneratedSite> site in GeneratedSites)
            {
                if (site.Value.Vertices.Count == 0)
                {
                    continue;
                }

                Vector2 minimumVertex = site.Value.Vertices[0];
                Vector2 maximumVertex = site.Value.Vertices[0];

                for (int i = 1; i < site.Value.Vertices.Count; i++)
                {
                    if (site.Value.Vertices[i].x < minimumVertex.x)
                    {
                        minimumVertex.x = site.Value.Vertices[i].x;
                    }

                    if (site.Value.Vertices[i].y < minimumVertex.y)
                    {
                        minimumVertex.y = site.Value.Vertices[i].y;
                    }

                    if (site.Value.Vertices[i].x > maximumVertex.x)
                    {
                        maximumVertex.x = site.Value.Vertices[i].x;
                    }

                    if (site.Value.Vertices[i].y > maximumVertex.y)
                    {
                        maximumVertex.y = site.Value.Vertices[i].y;
                    }
                }

                if (minimumVertex.x < 0.0f)
                {
                    minimumVertex.x = 0.0f;
                }

                if (minimumVertex.y < 0.0f)
                {
                    minimumVertex.y = 0.0f;
                }

                if (maximumVertex.x > Bounds.width)
                {
                    maximumVertex.x = Bounds.width;
                }

                if (maximumVertex.y > Bounds.height)
                {
                    maximumVertex.y = Bounds.height;
                }

                for (int x = (int)minimumVertex.x; x <= maximumVertex.x; x++)
                {
                    for (int y = (int)minimumVertex.y; y <= maximumVertex.y; y++)
                    {
                        if (PointInVertices(new Vector2(x, y), site.Value.Vertices))
                        {
                            if(Bounds.Contains(new Vector2(x, y)))
                            {
                                int index = x + (y * (int)Bounds.width);
                                returnData[index] = site.Value.SiteData;
                            }
                        }
                    }
                }
            }

            return returnData;
        }

        private void SortSitesAndSetValues()
        {
            _sites.Sort(
                delegate(VoronoiDiagramSite siteA, VoronoiDiagramSite siteB)
                {
                    if(Mathf.RoundToInt(siteA.Coordinate.y) < Mathf.RoundToInt(siteB.Coordinate.y))
                    {
                        return -1;
                    }

                    if(Mathf.RoundToInt(siteA.Coordinate.y) > Mathf.RoundToInt(siteB.Coordinate.y))
                    {
                        return 1;
                    }

                    if(Mathf.RoundToInt(siteA.Coordinate.x) < Mathf.RoundToInt(siteB.Coordinate.x))
                    {
                        return -1;
                    }

                    if(Mathf.RoundToInt(siteA.Coordinate.x) > Mathf.RoundToInt(siteB.Coordinate.x))
                    {
                        return 1;
                    }

                    return 0;
                });

            var currentMin = new Vector2(float.MaxValue, float.MaxValue);
            var currentMax = new Vector2(float.MinValue, float.MinValue);
            foreach(VoronoiDiagramSite site in _sites)
            {
                if(site.Coordinate.x < currentMin.x)
                {
                    currentMin.x = site.Coordinate.x;
                }

                if(site.Coordinate.x > currentMax.x)
                {
                    currentMax.x = site.Coordinate.x;
                }

                if(site.Coordinate.y < currentMin.y)
                {
                    currentMin.y = site.Coordinate.y;
                }

                if(site.Coordinate.y > currentMax.y)
                {
                    currentMax.y = site.Coordinate.y;
                }
            }

            _minValues = currentMin;
            _deltaValues = new Vector2(currentMax.x - currentMin.x, currentMax.y - currentMin.y);

        }

        private VoronoiDiagramSite GetNextSite()
        {
            if(_currentSiteIndex < _sites.Count)
            {
                VoronoiDiagramSite nextSite = _sites[_currentSiteIndex];
                _currentSiteIndex++;
                return nextSite;
            }

            return null;
        }

        private VoronoiDiagramSite GetLeftRegion(VoronoiDiagramHalfEdge halfEdge)
        {
            if(halfEdge.Edge == null)
            {
                return _bottomMostSite;
            }

            if(halfEdge.EdgeType == VoronoiDiagramEdgeType.Left)
            {
                return halfEdge.Edge.LeftSite;
            }
            else
            {
                return halfEdge.Edge.RightSite;
            }
        }

        private VoronoiDiagramSite GetRightRegion(VoronoiDiagramHalfEdge halfEdge)
        {
            if(halfEdge.Edge == null)
            {
                return _bottomMostSite;
            }

            if(halfEdge.EdgeType == VoronoiDiagramEdgeType.Left)
            {
                return halfEdge.Edge.RightSite;
            }
            else
            {
                return halfEdge.Edge.LeftSite;
            }
        }

        private static bool PointInVertices(Vector2 point, List<Vector2> verticies)
        {
            bool check = false;
            int nvert = verticies.Count;
            int i, j = 0;
            for (i = 0, j = nvert-1; i < nvert; j = i++) {
                if ( ((verticies[i].y>point.y) != (verticies[j].y>point.y)) &&
                (point.x < (verticies[j].x-verticies[i].x) * (point.y-verticies[i].y) / (verticies[j].y-verticies[i].y) + verticies[i].x) )
                check = !check;
            }
            return check;
        }
    }
}
