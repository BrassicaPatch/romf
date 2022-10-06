using UnityEngine;

namespace vDiagramGen
{
    [System.Serializable]
    public class VoronoiDiagramHalfEdge
    {
        public VoronoiDiagramEdge Edge;
        public VoronoiDiagramEdgeType EdgeType;
        public VoronoiDiagramVertex Vertex;
        public float StarY;

        public VoronoiDiagramHalfEdge EdgeListLeft;
        public VoronoiDiagramHalfEdge EdgeListRight;
        public VoronoiDiagramHalfEdge NextInPriorityQueue;

        public VoronoiDiagramHalfEdge(VoronoiDiagramEdge edge, VoronoiDiagramEdgeType edgeType)
        {
            Edge = edge;
            EdgeType = edgeType;
            Vertex = null;
            StarY = 0f;
            EdgeListLeft = null;
            EdgeListRight = null;
            NextInPriorityQueue = null;
        }

        public bool IsLeftOf(Vector2 point)
        {
            VoronoiDiagramSite topSite = Edge.RightSite;
            bool isRightOfSite = point.x > topSite.Coordinate.x;
            bool isAbove;

            if(isRightOfSite && EdgeType == VoronoiDiagramEdgeType.Left)
            {
                return true;
            }

            if(!isRightOfSite && EdgeType == VoronoiDiagramEdgeType.Right)
            {
                return false;
            }

            if(Edge.A.IsAlmostEqualTo(1f))
            {
                var dyp = point.y - topSite.Coordinate.y;
                var dxp = point.x - topSite.Coordinate.x;

                var isFast = false;
                if((!isRightOfSite && Edge.B < 0.0f) ||
                   (isRightOfSite && Edge.B >= 0.0f))
                {
                    isAbove = dyp >= Edge.B * dxp;
                    isFast = isAbove;
                }
                else
                {
                    isAbove = point.x + point.y * Edge.B > Edge.C;
                    if(Edge.B < 0.0f)
                    {
                        isAbove = !isAbove;
                    }
                    if(!isAbove)
                    {
                        isFast = true;
                    }
                }

                if(!isFast)
                {
                    var dxs = topSite.Coordinate.x - Edge.LeftSite.Coordinate.x;
                    isAbove = Edge.B * (dxp * dxp - dyp * dyp) < dxs * dyp * (1f + 2f * dxp / dxs + Edge.B * Edge.B);
                    if(Edge.B < 0f)
                    {
                        isAbove = !isAbove;
                    }
                }
            }
            else // edge.b == 1.0
            {
                float t1, t2, t3, yl;
                yl = Edge.C - Edge.A * point.x;
                t1 = point.y - yl;
                t2 = point.x - topSite.Coordinate.x;
                t3 = yl - topSite.Coordinate.y;
                isAbove = t1 * t1 > t2 * t2 + t3 * t3;
            }
            return EdgeType == VoronoiDiagramEdgeType.Left ? isAbove : !isAbove;
        }

        public bool IsRightOf(Vector2 point)
        {
            return !IsLeftOf(point);
        }

        public bool HasReferences()
        {
            return EdgeListLeft != null || EdgeListRight != null || NextInPriorityQueue != null;
        }
    }
}
