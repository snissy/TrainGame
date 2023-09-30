using UnityEngine;

namespace DefaultNamespace
{
    public static class DebugDraw {

        public static void DrawPoint(Vector3 p, float markerSize, Color c) {

            Vector3 upVector = Vector3.up * markerSize;
            Vector3 rightVector = Vector3.right * markerSize;
            Vector3 forwardVector = Vector3.forward * markerSize;
            
            Debug.DrawLine(p + upVector, p - upVector, c, 25);
            Debug.DrawLine(p + rightVector, p - rightVector, c, 25);
            Debug.DrawLine(p + forwardVector, p - forwardVector, c, 25);
        }

        public static void DrawArrow(Vector3 start, Vector3 end, Color c) {

            Gizmos.color = c;

            Gizmos.DrawLine(start, end);
            float arrowSize = 0.10f;
            Vector3 direction = (end - start).normalized;

            Vector3 arrowWing1 = end + Quaternion.Euler(0, 160, 0) * direction * arrowSize;
            Vector3 arrowWing2 = end + Quaternion.Euler(0, -160, 0) * direction * arrowSize;
            Gizmos.DrawLine(end, arrowWing1);
            Gizmos.DrawLine(end, arrowWing2);
        }
    }
}