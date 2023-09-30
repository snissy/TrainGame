using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Common
{
    public static class MathUtils {
        
        private const float DELTA = 1e-9f;
         public struct HitResult {
            public bool exist;
            public Vector3 hitPoint;
            public HitResult(bool exist , Vector3 hitPoint) {
                this.exist = exist;
                this.hitPoint = hitPoint;
            }
        }
        public static float[] LinSpace(float start, float stop, int num = 50, bool endpoint = true) { 
            
            if (num <= 0) {
                return Array.Empty<float>();
            }

            if (num == 1) {
                return new float[] { start };
            }

            float[] samples = new float[num];
            float step;

            if (endpoint) {
                step = (stop - start) / (num - 1);
                for (int i = 0; i < num; i++)
                {
                    samples[i] = start + step * i;
                }
            }
            else {
                step = (stop - start) / num;
                for (int i = 0; i < num; i++)
                {
                    samples[i] = start + step * i;
                }
            }

            return samples;
        }

        public static bool InRange(float x, float start, float end) {
            return (start < x && x < end);
        }

        public static bool NotInRange(float x, float start, float end) {
            return !InRange(x, start, end);
        }
        
        public static Vector3 GetCenterPoint(this List<Vector3> list) {
            
            Assert.AreNotEqual(list.Count, 0, "You can't get a center point from a empty list");
        
            Vector3 res = Vector3.zero;

            foreach (Vector3 point in list) {
                res += point;
            }
        
            return res/ list.Count;
        }
        
        public static void SortByAngularDifference(this List<Vector3> list, Vector3 center) {
            list.Sort((a, b) => {
            
                float angleA = Mathf.Atan2(a.z - center.z, a.x - center.x);
                float angleB = Mathf.Atan2(b.z - center.z, b.x - center.x);

                return angleA.CompareTo(angleB);
            });
        }
        
        public static float Sigmoid(float distance) {
            return 1.0f / (1.0f + Mathf.Exp(-distance));
        }

        public static HitResult Compute2DIntersection(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
        
            float a1 = v3.z - v4.z;
            float b1 = v3.x - v4.x;
            float c1 = v1.x - v2.x;
            float d1 = v1.z - v2.z;

            float denominator  = c1*a1 - d1*b1;

            if (MathF.Abs(denominator) < 1E-6f) {
                return new HitResult(false, Vector3.zero);
            }
        
            float e1 = v1.x - v3.x;
            float f1 = v1.z - v3.z;

            float tNumerator = e1*a1 - f1*b1;
            float t = tNumerator / denominator;
        
            if (NotInRange(t, DELTA, 1-DELTA)) {
                return new HitResult(false, Vector3.zero);
            }
            
            float uNumerator = e1*d1 - f1*c1;
            float u = uNumerator / denominator;
        
            if (NotInRange(u, DELTA, 1-DELTA)) {
                return new HitResult(false, Vector3.zero);
            }
            
            return new HitResult(true, v1 + t*(v2-v1));
        }

        public static Vector3 GetBoxDimensionSize(this BoxCollider box) {
            
            Transform boxTransform = box.transform;
            Vector3 boxSizeLocal = box.size;
            Vector3 boxSizeWorld = boxTransform.TransformVector(boxSizeLocal);
            
            return new Vector3(
                x:Vector3.Project(boxSizeWorld, boxTransform.right).magnitude,
                y:Vector3.Project(boxSizeWorld, boxTransform.up).magnitude,
                z:Vector3.Project(boxSizeWorld, boxTransform.forward).magnitude
            );
        }
    }
}