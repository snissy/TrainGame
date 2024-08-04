using UnityEngine;

namespace AmbientOcclusion.Geometry
{
    public struct Vertex {
        public readonly Vector3 worldPosition;
        public readonly Vector3 normal;
        public readonly int index;
        public readonly Quaternion normalLookRotiation;

        public Vertex(Vector3 worldPosition, Vector3 normal, int index) {
            this.worldPosition = worldPosition;
            this.normal = normal;
            this.normalLookRotiation = Quaternion.LookRotation(normal);
            this.index = index;
        }
    }
}