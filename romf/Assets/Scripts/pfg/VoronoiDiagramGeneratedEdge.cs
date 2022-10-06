using UnityEngine;

namespace vDiagramGen
{
    [System.Serializable]
    public class VoronoiDiagramGeneratedEdge
    {
        public int Index;
        public Vector2 LeftEndPoint;
        public Vector2 RightEndPoint;

        public VoronoiDiagramGeneratedEdge(int index, Vector2 leftEndPoint, Vector2 rightEndPoint)
        {
            Index = index;
            LeftEndPoint = leftEndPoint;
            RightEndPoint = rightEndPoint;
        }
    }
}
