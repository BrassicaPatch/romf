using UnityEngine;
using System.Collections.Generic;

namespace vDiagramGen
{
    [System.Serializable]
    public class VoronoiDiagramPriorityQueue
    {
        private int _minimumBucket;
        private int _count;
        private readonly List<VoronoiDiagramHalfEdge> _hash;
        private Vector2 _minimumValues;
        private Vector2 _deltaValues;

        public VoronoiDiagramPriorityQueue(int numberOfSites, Vector2 minimumValues, Vector2 deltaValues)
        {
            _minimumBucket = 0;
            _count = 0;
            _minimumValues = minimumValues;
            _deltaValues = deltaValues;

            _hash = new List<VoronoiDiagramHalfEdge>();
            // Create an array full of dummies that represent the beginning of a bucket
            for(int i = 0; i < 4 * Mathf.Sqrt(numberOfSites); i++)
            {
                _hash.Add(new VoronoiDiagramHalfEdge(null, VoronoiDiagramEdgeType.None));
                _hash[i].NextInPriorityQueue = null;
            }
        }

        public int GetBucket(VoronoiDiagramHalfEdge halfEdge)
        {
            int bucket;

            bucket = Mathf.RoundToInt((halfEdge.StarY - _minimumValues.y) / _deltaValues.y * _hash.Count);
            if(bucket < 0)
            {
                bucket = 0;
            }

            if(bucket >= _hash.Count)
            {
                bucket = _hash.Count - 1;
            }

            return bucket;
        }

        public void Insert(VoronoiDiagramHalfEdge halfEdge)
        {
            VoronoiDiagramHalfEdge previous, next;
            int insertionBucket = GetBucket(halfEdge);

            if(insertionBucket < _minimumBucket)
            {
                _minimumBucket = insertionBucket;
            }

            // Start at the beginning of the bucket and find where the half edge should go
            previous = _hash[insertionBucket];
            next = previous.NextInPriorityQueue;
            while(
                next != null &&
                (
                    halfEdge.StarY > next.StarY ||
                    (halfEdge.StarY.IsAlmostEqualTo(next.StarY) &&
                     halfEdge.Vertex.Coordinate.x > next.Vertex.Coordinate.x)
                    )
                )
            {
                previous = next;
                next = previous.NextInPriorityQueue;
            }

            halfEdge.NextInPriorityQueue = previous.NextInPriorityQueue;
            previous.NextInPriorityQueue = halfEdge;
            _count++;
        }

        public void Delete(VoronoiDiagramHalfEdge halfEdge)
        {
            int removalBucket = GetBucket(halfEdge);

            if(halfEdge.Vertex != null)
            {
                VoronoiDiagramHalfEdge previous = _hash[removalBucket];
                while(previous.NextInPriorityQueue != halfEdge)
                {
                    previous = previous.NextInPriorityQueue;
                }

                previous.NextInPriorityQueue = halfEdge.NextInPriorityQueue;
                _count--;

                halfEdge.Vertex = null;
                halfEdge.NextInPriorityQueue = null;
            }
        }

        public bool IsEmpty()
        {
            return _count == 0;
        }

        public Vector2 GetMinimumBucketFirstPoint()
        {
            while(_minimumBucket < _hash.Count - 1 && _hash[_minimumBucket].NextInPriorityQueue == null)
            {
                _minimumBucket++;
            }

            return new Vector2(
                _hash[_minimumBucket].NextInPriorityQueue.Vertex.Coordinate.x,
                _hash[_minimumBucket].NextInPriorityQueue.StarY
                );
        }

        public VoronoiDiagramHalfEdge RemoveAndReturnMinimum()
        {
            VoronoiDiagramHalfEdge minEdge;

            minEdge = _hash[_minimumBucket].NextInPriorityQueue;
            _hash[_minimumBucket].NextInPriorityQueue = minEdge.NextInPriorityQueue;
            _count--;

            minEdge.NextInPriorityQueue = null;

            return minEdge;
        }
    }
}