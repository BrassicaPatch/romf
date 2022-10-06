using System.Collections.Generic;
using UnityEngine;

namespace vDiagramGen
{
    [System.Serializable]
    public class VoronoiDiagramGeneratedSite
    {
        public int Index;
        public Color SiteData;
        public Vector2 Coordinate;
        public Vector2 Centroid;
        public List<VoronoiDiagramGeneratedEdge> Edges;
        public List<Vector2> Vertices;
        public List<int> NeighborSites;

        public bool IsCorner;
        public bool IsEdge;

        public VoronoiDiagramGeneratedSite(int index, Vector2 coordinate, Vector2 centroid, Color siteData, bool isCorner, bool isEdge)
        {
            Index = index;
            Coordinate = coordinate;
            Centroid = centroid;
            SiteData = siteData;
            IsCorner = isCorner;
            IsEdge = isEdge;
            Edges = new List<VoronoiDiagramGeneratedEdge>();
            Vertices = new List<Vector2>();
            NeighborSites = new List<int>();
        }
    }
}
