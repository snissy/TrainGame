using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Common {
    
    public struct HitResult {
        public bool exist;
        public Vector3 hitPoint;
        public HitResult(bool exist , Vector3 hitPoint) {
            this.exist = exist;
            this.hitPoint = hitPoint;
        }
    }
    
    public readonly struct LineSegment {

        private readonly Vector3 start;
        private readonly Vector3 end;
        private readonly Vector3 dir;
        private readonly Vector3 midPoint;
        private readonly Vector3 vector;
        private readonly float lenght;

        public Vector3 Start => start;
        public Vector3 End => end;
        public Vector3 Vector => vector;
        public Vector3 Dir => dir;
        public Vector3 MidPoint => midPoint;
        public float Lenght => lenght;

        public LineSegment(Vector3 start, Vector3 end) {
            this.start = start;
            this.end = end;
            this.midPoint = (start + end) * 0.5f;
            this.vector = end - start;
            this.lenght = vector.magnitude;
            this.dir = vector.normalized;
        }
        public static LineSegment operator * (Quaternion rotation , LineSegment segment) {
            return new LineSegment(rotation * segment.start, rotation * segment.end);
        }
    }

    public static class VectorExtension{
        public static Vector2 SwizzleXZ(this Vector3 vec) {
            return new Vector2(vec.x, vec.z);
        }
        
        public static Vector3 SwizzleXY0(this Vector2 vec) {
            return new Vector3(vec.x, vec.y, 0.0f);
        }

        public static Vector3 SwizzleX0Y(this Vector2 vec) {
            return new Vector3(vec.x, 0.0f, vec.y);
        }
    }
    
    public static class MathUtils {
        
        private const float DELTA = 1e-9f;
        public static float[] LinSpace(float start, float stop, int num = 50, bool endpoint = true) { 
            // TODO write function doing the reversed, submitting float and getting index
            // would be super useful!
            
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

        public static HitResult Compute2DIntersection(LineSegment l1, LineSegment l2) {
        
            float a1 = l2.Start.z - l2.End.z;
            float b1 = l2.Start.x - l2.End.x;
            float c1 = l1.Start.x - l1.End.x;
            float d1 = l1.Start.z - l1.End.z;

            float denominator  = c1*a1 - d1*b1;

            if (MathF.Abs(denominator) < 1E-6f) {
                return new HitResult(false, Vector3.zero);
            }
        
            float e1 = l1.Start.x - l2.Start.x;
            float f1 = l1.Start.z - l2.Start.z;

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
            
            return new HitResult(true, l1.Start + t*(l1.Dir));
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