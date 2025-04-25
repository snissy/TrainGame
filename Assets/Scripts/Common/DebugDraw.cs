using Common;
using UnityEngine;

namespace DefaultNamespace
{
    public static class DebugDraw {

        private const float TAU = 2.0f * 3.1415926f;

        public static void DrawPoint(Vector3 p, float markerSize, Color c, float duration) {

            Vector3 upVector = Vector3.up * markerSize;
            Vector3 rightVector = Vector3.right * markerSize;
            Vector3 forwardVector = Vector3.forward * markerSize;
            
            Debug.DrawLine(p + upVector, p - upVector, c, duration);
            Debug.DrawLine(p + rightVector, p - rightVector, c, duration);
            Debug.DrawLine(p + forwardVector, p - forwardVector, c, duration);
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

        public static void DrawDisk(Vector3 center, float radius, Color c) {
            
            Gizmos.color = c;
            
            float[] tSpace = MathFunctions.LinSpace(0, TAU);

            for (var i = 0; i < tSpace.Length; i++) {
                float tStep = tSpace[i];
                float tStepPlusOne = tSpace[(i + 1) % tSpace.Length];
                
                Vector3 v1 = center + new Vector3(Mathf.Cos(tStep) , 0.0f, Mathf.Sin(tStep)) * radius;
                Vector3 v2 = center + new Vector3(Mathf.Cos(tStepPlusOne) , 0.0f, Mathf.Sin(tStepPlusOne))*radius;
                
                Gizmos.DrawLine(v1, v2);
            }
        }

        public static void DrawDottedLine(Vector3 start, Vector3 end, Color c) {

            float fixSize = 0.15f;
            float magnitude = Vector3.Distance(start, end);
            int nSteps = Mathf.FloorToInt(magnitude / fixSize);
            
            Gizmos.color = c;
            float[] tSpace = MathFunctions.LinSpace(0, 1, nSteps);
            
            for (var i = 0; i < tSpace.Length - 1; i+=2) {
                float tStep = tSpace[i];
                float tStepPlusOne = tSpace[i + 1];

                Vector3 v1 = Vector3.Lerp(start, end, tStep);
                Vector3 v2 = Vector3.Lerp(start, end, tStepPlusOne);
                Gizmos.DrawLine(v1, v2);
            }
        }
    }
}