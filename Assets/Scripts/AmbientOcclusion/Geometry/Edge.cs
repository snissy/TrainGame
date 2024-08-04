using UnityEngine;

namespace AmbientOcclusion.Geometry
{
    public struct Edge {
        
        public Vector2 start;
        public Vector2 dir;
        
        public Edge(Vector2 start, Vector2 end) {
            this.start = start;
            this.dir = end - start;
        }
    }
}