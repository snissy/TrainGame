using UnityEngine;

namespace AmbientOcclusion.Geometry
{
    public static class MathUtils {
        public static float CrossProduct(Vector2 a, Vector2 b) {
            return a.x * b.y - a.y * b.x;
        }

        public static float TriangleArea(Vector2 a, Vector2 b, Vector2 c) {
            // Calculate vectors AB, AC, and BC
            Vector2 ab = b - a;
            Vector2 ac = c - a;
            return Mathf.Abs(CrossProduct(ab, ac)) * 0.5f;
        }
    }
}