
using AmbientOcclusion.Geometry;
using UnityEngine;

namespace Matoya.Common.Geometry {
    public struct Triangle
    {
        private const float ONE_OVER_THIRD = 1 / 3.0f;
        
        public const int NVector2 = 5;
        public const int NVector3 = 6;
        public const int NFLoats = 1;
        public const int NBounds = 1;
        public const int NQuaternion = 1;
        
        public readonly Vector2 uv0;
        public readonly Vector2 uv1;
        public readonly Vector2 uv2;
        
        public readonly Vector2 uvMin;
        public readonly Vector2 uvMax;
        
        public readonly float uvArea;

        public readonly Vector3 v0;
        public readonly Vector3 v1;
        public readonly Vector3 v2;

        public readonly Vector3 faceNormal;
        public readonly Vector3 faceTangent;
        public readonly Vector3 faceBinormal;

        public readonly Quaternion faceRotation;
        
        public readonly Bounds bounds;

        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2) {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;

            Vector3 edge0 = v1 - v0;
            Vector3 edge1 = v2 - v0;
            
            // Cross product of two edges gives the normal vector
            faceNormal = Vector3.Cross(edge0, edge1);
            faceNormal /= faceNormal.magnitude;
            
            // can't use normalize it clamps value below 1e-6f;
            Vector3 centroid = (v0 + v1 + v2) * ONE_OVER_THIRD;
            bounds = new Bounds(centroid, Vector3.zero);
            bounds.SetMinMax(Vector3.Min(Vector3.Min(v0, v1), v2), Vector3.Max(Vector3.Max(v0, v1), v2));

            this.uv0 = uv0;
            this.uv1 = uv1;
            this.uv2 = uv2;

            this.uvMin = Vector2.Min(uv0, Vector2.Min(uv1, uv2));
            this.uvMax = Vector2.Max(uv0, Vector2.Max(uv1, uv2));

            this.uvArea = MathUtils.TriangleArea(uv0, uv1, uv2);

            Vector2 st1 = uv1 - uv0;   // x --> S, y --> T
            Vector2 st2 = uv2 - uv0;

            float inverse = 1.0f / (st1.x * st2.y - st2.x * st1.y);

            float tX = inverse * (st2.y * edge0.x + -st1.y * edge1.x);
            float tY = inverse * (st2.y * edge0.y + -st1.y * edge1.y);
            float tZ = inverse * (st2.y * edge0.z + -st1.y * edge1.z);
            
            float bX = inverse * (-st2.x * edge0.x + st1.x * edge1.x);
            float bY = inverse * (-st2.x * edge0.y + st1.x * edge1.y);
            float bZ = inverse * (-st2.x * edge0.z + st1.x * edge1.z);
            
            this.faceTangent = new Vector3(tX, tY, tZ).normalized;
            this.faceBinormal = new Vector3(bX, bY, bZ).normalized;
            faceRotation = Quaternion.LookRotation(faceNormal, faceTangent);
        }

        public bool IntersectRay(Ray ray, out float hitLambda) {

            // https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm

            float delta = Mathf.Epsilon;
            hitLambda = float.MaxValue;

            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 rayCrossEdge2 = Vector3.Cross(ray.direction, edge2);

            float det = Vector3.Dot(edge1, rayCrossEdge2);

            if (det > -delta && det < delta) {
                return false; // This ray is parallel to this triangle.
            }

            float invDet = 1.0f / det;
            Vector3 s = ray.origin - v0;
            float u = invDet * Vector3.Dot(s, rayCrossEdge2);

            if (u < 0 || u > 1) {
                return false;
            }

            Vector3 sCrossEdge1 = Vector3.Cross(s, edge1);
            float v = invDet * Vector3.Dot(ray.direction, sCrossEdge1);

            if (v < 0 || u + v > 1) {
                return false;
            }

            // At this stage we can compute t to find out where the intersection point is on the line.
            hitLambda = invDet * Vector3.Dot(edge2, sCrossEdge1);

            if (hitLambda > delta) {// ray intersection
                return true;
            }
            // This means that there is a line intersection but not a ray intersection.
            return false;
        }
        
        public override string ToString()
        {
            return $"Triangle:\n" +
                   $"  Vertices: v0={v0}, v1={v1}, v2={v2}\n" +
                   $"  UVs: uv0={uv0}, uv1={uv1}, uv2={uv2}\n" +
                   $"  Normals: faceNormal={faceNormal}, faceTangent={faceTangent}, faceBinormal={faceBinormal}\n" +
                   $"  Bounds: min={bounds.min}, max={bounds.max}\n" +
                   $"  UV Area: {uvArea}\n" +
                   $"  Face Rotation: {faceRotation.eulerAngles}";
        }
       
    }
}
