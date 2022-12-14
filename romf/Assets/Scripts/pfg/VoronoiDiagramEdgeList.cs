using System;
using UnityEngine;
using System.Collections.Generic;

namespace vDiagramGen
{
    [System.Serializable]
    public class VoronoiDiagramEdgeList
    {
        public readonly List<VoronoiDiagramHalfEdge> Hash;
        public readonly VoronoiDiagramHalfEdge LeftEnd;
        public readonly VoronoiDiagramHalfEdge RightEnd;
        public Vector2 MinimumValues;
        public Vector2 DeltaValues;

        public VoronoiDiagramEdgeList(int numberOfSites, Vector2 minimumValues, Vector2 deltaValues)
        {
            MinimumValues = minimumValues;
            DeltaValues = deltaValues;

            Hash = new List<VoronoiDiagramHalfEdge>();
            for(int i = 0; i < 2 * Mathf.Sqrt(numberOfSites); i++)
            {
                Hash.Add(null);
            }

            LeftEnd = new VoronoiDiagramHalfEdge(null, VoronoiDiagramEdgeType.None);
            RightEnd = new VoronoiDiagramHalfEdge(null, VoronoiDiagramEdgeType.None);

            LeftEnd.EdgeListLeft = null;
            LeftEnd.EdgeListRight = RightEnd;

            RightEnd.EdgeListLeft = LeftEnd;
            RightEnd.EdgeListRight = null;

            Hash[0] = LeftEnd;
            Hash[Hash.Count - 1] = RightEnd;
        }

        public void Insert(VoronoiDiagramHalfEdge leftBound, VoronoiDiagramHalfEdge newHalfEdge)
        {
            newHalfEdge.EdgeListLeft = leftBound;
            newHalfEdge.EdgeListRight = leftBound.EdgeListRight;
            leftBound.EdgeListRight.EdgeListLeft = newHalfEdge;
            leftBound.EdgeListRight = newHalfEdge;
        }

        public void Delete(VoronoiDiagramHalfEdge halfEdge)
        {
            halfEdge.EdgeListLeft.EdgeListRight = halfEdge.EdgeListRight;
            halfEdge.EdgeListRight.EdgeListLeft = halfEdge.EdgeListLeft;
            halfEdge.Edge = VoronoiDiagramEdge.Deleted;
            halfEdge.EdgeListLeft = null;
            halfEdge.EdgeListRight = null;
        }

        public VoronoiDiagramHalfEdge GetFromHash(int bucket)
        {
            VoronoiDiagramHalfEdge halfEdge;

            if(bucket < 0 || bucket >= Hash.Count)
            {
                return null;
            }

            halfEdge = Hash[bucket];
            if(halfEdge != null && halfEdge.Edge == VoronoiDiagramEdge.Deleted)
            {
                // Edge ready for deletion, return null instead
                Hash[bucket] = null;

                // Cannot delete half edge yet, so just return null at this point
                return null;
            }

            return halfEdge;
        }

        public VoronoiDiagramHalfEdge GetLeftBoundFrom(Vector2 point)
        {
            int bucket;
            VoronoiDiagramHalfEdge halfEdge;

            bucket = Mathf.RoundToInt((point.x - MinimumValues.x) / DeltaValues.x * Hash.Count);

            if(bucket < 0)
            {
                bucket = 0;
            }

            if(bucket >= Hash.Count)
            {
                bucket = Hash.Count - 1;
            }

            halfEdge = GetFromHash(bucket);
            if(halfEdge == null)
            {
                int index = 1;
                while(true)
                {
                    halfEdge = GetFromHash(bucket - index);
                    if(halfEdge != null)
                    {
                        break;
                    }

                    halfEdge = GetFromHash(bucket + index);
                    if(halfEdge != null)
                    {
                        break;
                    }

                    index++;

                    // Infinite loop check
                    if((bucket - index) < 0 && (bucket + index) >= Hash.Count)
                    {
                        Debug.LogError(
                            string.Format(
                                "(bucket - index) < 0 && (bucket + index) >= Hash.Count: {0} < 0 && {1} >= {2})",
                                bucket - index, bucket + index, Hash.Count));
                        throw new Exception("Entered infinite loop");
                    }
                }
            }

            // If we are at the left end or if we are not at the right end of the half edge is left of the passed in point
            if(halfEdge == LeftEnd || (halfEdge != RightEnd && halfEdge.IsLeftOf(point)))
            {
                do
                {
                    halfEdge = halfEdge.EdgeListRight;
                } while(halfEdge != RightEnd && halfEdge.IsLeftOf(point));
                halfEdge = halfEdge.EdgeListLeft;
            }
            else
            {
                // If we are at the right end or if we are not at the left end of the half edge is right of the passed in point
                do
                {
                    halfEdge = halfEdge.EdgeListLeft;
                } while(halfEdge != LeftEnd && halfEdge.IsRightOf(point));
            }

            // Update the hash table and reference counts. Excludes left and right end
            if(bucket > 0 && bucket < Hash.Count - 1)
            {
                Hash[bucket] = halfEdge;
            }
            return halfEdge;
        }
    }
}
