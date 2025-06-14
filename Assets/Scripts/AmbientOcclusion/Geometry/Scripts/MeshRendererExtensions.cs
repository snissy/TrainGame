using System.Collections.Generic;
using UnityEngine;

namespace AmbientOcclusion.Geometry.Scripts
{
    public static class MeshRendererExtensions
    {
        public static bool IntersectRay(this MeshRenderer meshRenderer, Ray ray, out float hitLambda)
        {
            Triangle[] triangles = meshRenderer.TriangleArray();
            hitLambda = float.MaxValue;
            bool hitMesh = false;
            foreach (Triangle triangle in triangles)
            {
                if (triangle.IntersectRay(ray, out float lambda))
                {
                    hitLambda = Mathf.Min(hitLambda, lambda);
                    hitMesh = true;
                }
            }

            return hitMesh;
        }

        public static Triangle[] TriangleArray(this MeshRenderer meshRenderer)
        {
            Mesh meshInstance = meshRenderer.GetComponent<MeshFilter>().sharedMesh;
            Triangle[] result = new Triangle[meshInstance.triangles.Length / 3];
            int i = 0;
            foreach (Triangle triangle in meshRenderer.GetTriangleEnumerator())
            {
                result[i] = triangle;
                i++;
            }

            return result;
        }

        public static IEnumerable<Triangle> GetTriangleEnumerator(this MeshRenderer meshRenderer)
        {
            Matrix4x4 localToWorld = meshRenderer.localToWorldMatrix;
            Mesh meshInstance = meshRenderer.GetComponent<MeshFilter>().sharedMesh;

            Vector3[] vertices = meshInstance.vertices;
            int[] triangles = meshInstance.triangles;
            Vector2[] uvs = meshInstance.uv;

            Vector3[] worldVertices = new Vector3[vertices.Length];
            bool[] worldVerticesActive = new bool[vertices.Length];

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0Index = triangles[i];
                int v1Index = triangles[i + 1];
                int v2Index = triangles[i + 2];

                if (!worldVerticesActive[v0Index])
                {
                    worldVerticesActive[v0Index] = true;
                    worldVertices[v0Index] = localToWorld.MultiplyPoint3x4(vertices[v0Index]);
                }

                if (!worldVerticesActive[v1Index])
                {
                    worldVerticesActive[v1Index] = true;
                    worldVertices[v1Index] = localToWorld.MultiplyPoint3x4(vertices[v1Index]);
                }

                if (!worldVerticesActive[v2Index])
                {
                    worldVerticesActive[v2Index] = true;
                    worldVertices[v2Index] = localToWorld.MultiplyPoint3x4(vertices[v2Index]);
                }

                yield return new Triangle(
                    worldVertices[v0Index],
                    worldVertices[v1Index],
                    worldVertices[v2Index],
                    uvs[v0Index],
                    uvs[v1Index],
                    uvs[v2Index]
                );
            }
        }

        public static IEnumerable<Vertex> GetVertexEnumerator(this MeshRenderer meshRenderer)
        {
            Matrix4x4 localToWorld = meshRenderer.localToWorldMatrix;
            Mesh meshInstance = meshRenderer.GetComponent<MeshFilter>().sharedMesh;

            Vector3[] vertices = meshInstance.vertices;
            Vector3[] normals = meshInstance.normals;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = localToWorld.MultiplyPoint3x4(vertices[i]);
                Vector3 normal = localToWorld.MultiplyVector(normals[i]);

                yield return new Vertex(vertex, normal, i);
            }
        }
    }
}