using UnityEngine;

namespace AmbientOcclusion.Geometry.Scripts
{
    public static class BoundsExtensions
    {
        public static Bounds InverseTransform(this Bounds bounds, Transform transform)
        {
            return new Bounds(transform.InverseTransformPoint(bounds.center), transform.InverseTransformVector(bounds.size));
        }

        public static Bounds Transform(this Bounds bounds, Transform transform)
        {
            return new Bounds(transform.TransformPoint(bounds.center), transform.TransformVector(bounds.size));
        }

        public static float Volume(this Bounds bounds)
        {
            return bounds.size.x * bounds.size.y * bounds.size.z;
        }

        public static float SurfaceArea(this Bounds bounds)
        {
            Vector3 size = bounds.size;
            return 2 * (size.x * size.y + size.x * size.z + size.y * size.z);
        }

        public static float SgnDistance(Bounds a, Bounds b)
        {
            Vector3 u = b.min - a.max;
            Vector3 v = a.min - b.max;

            Vector3 q = Vector3.Max(u, v);

            float maxComponent = Mathf.Max(q.x, q.y, q.z);
            float negativeDistance = Mathf.Min(0, maxComponent);

            Vector3 clampedU = Vector3.Max(Vector3.zero, u);
            Vector3 clampedV = Vector3.Max(Vector3.zero, v);

            float positiveDistance = Mathf.Sqrt(clampedU.sqrMagnitude + clampedV.sqrMagnitude);
            
            return negativeDistance + positiveDistance;
        }
    }
}