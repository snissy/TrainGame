using UnityEngine;

namespace Matoya.Common {
    public static class BoundsExtensions {
        public static Bounds InverseTransform(this Bounds bounds, Transform transform) {
            return new Bounds(transform.InverseTransformPoint(bounds.center), transform.InverseTransformVector(bounds.size));
        }
        public static Bounds Transform(this Bounds bounds, Transform transform) {
            return new Bounds(transform.TransformPoint(bounds.center), transform.TransformVector(bounds.size));
        }
        public static float Volume(this Bounds bounds) {
            return bounds.size.x * bounds.size.y * bounds.size.z;
        }

        public static float SurfaceArea(this Bounds bounds) {
            Vector3 size = bounds.size;
            return 2 * (size.x * size.y + size.x * size.z + size.y * size.z);
        }
    }
}
