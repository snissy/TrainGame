using System;
using System.Collections.Generic;
using System.Linq;
using AmbientOcclusion.Geometry.Scripts;
using AmbientOcclusion.Geometry.Scripts.OcclusionTool;
using UnityEditor;
using UnityEngine;

public class BVHOptimizationEditorTest : MonoBehaviour
{
    [SerializeField] private Transform rayTransform;
    [SerializeField] private int nPixels;
    [SerializeField] private float focalLength;
    [SerializeField] private MeshRenderer meshToAnalyze;
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private Color updateColor = Color.white;

    [SerializeField] private Vector3 updateOffsetDraw = Vector3.up;

    [SerializeField] private float drawDuration = 25f;

    private BVHMesh bvhMesh;

    [ContextMenu("Analyze BVH")]
    public void Analyze()
    {
        CameraModel.PixelData[] rayList =
            new CameraModel(rayTransform.Pose(), nPixels, focalLength).GetRays().ToArray();

        Debug.Log($"number of rays: {rayList.Length}");

        bvhMesh = new BVHMesh(meshToAnalyze);

        float notOptimize = 0;
        TimeSpan elapsedTime = PerformanceUtils.MeasureExecutionTime(() =>
        {
            foreach (CameraModel.PixelData pixel in rayList)
            {
                bvhMesh.IntersectRay(pixel.ray, out float lambda, out int nIntersections);
                notOptimize += nIntersections;
            }
        });

        float sahCostNotOptimize = bvhMesh.SAHCostForTree();
        Debug.Log(
            $"SAHCost for tree: {sahCostNotOptimize}, rayTest: {elapsedTime.TotalSeconds:F6} s no optimization, nIntersections: {notOptimize}");

        TimeSpan computationTimeForOptimization = PerformanceUtils.MeasureExecutionTime(() =>
        {
            bvhMesh.TryToOptimize();
        });

        Debug.Log($"Time to optimize {computationTimeForOptimization.TotalSeconds:F6}");

        float forOptimize = 0;
        TimeSpan optimizedElapsedTime = PerformanceUtils.MeasureExecutionTime(() =>
        {
            foreach (CameraModel.PixelData pixel in rayList)
            {
                bvhMesh.IntersectRay(pixel.ray, out float lambda, out int nIntersections);
                forOptimize += nIntersections;
            }
        });

        float sahCostOptimized = bvhMesh.SAHCostForTree();

        Debug.Log($"SAHCost for tree: {bvhMesh.SAHCostForTree()}, rayTest: {optimizedElapsedTime.TotalSeconds:F6} s with optimization, nIntersections: {forOptimize}");
        Debug.Log($"Improvement IntersectionsTest: {forOptimize / notOptimize}");
        Debug.Log($"SAH COST Diff: {sahCostOptimized - sahCostNotOptimize}");
    }

    private void DrawBvh(BVHMesh bvh, Color boundColor, Vector3 offset)
    {
        foreach (Bounds bound in bvh.GetBounds())
        {
            DrawBound(bound, boundColor, offset);
        }
    }

    private void DrawBound(Bounds bounds, Color c, Vector3 offset)
    {
        Vector3[] boundsVertices = BVHUtils.CalculateWorldBoxCorners(bounds);
        for (int i = 0; i < 4; i++)
        {
            Vector3 c1 = boundsVertices[i];
            Vector3 c2 = boundsVertices[(i + 1) % 4];
            DrawLine(c1, c2, c, offset);

            Vector3 c3 = boundsVertices[4 + i];
            Vector3 c4 = boundsVertices[4 + (i + 1) % 4];

            DrawLine(c3, c4, c, offset);
            DrawLine(c1, c3, c, offset);
        }
    }

    private void DrawLine(Vector3 p0, Vector3 p1, Color c, Vector3 offset)
    {
        Debug.DrawLine(offset + p0, offset + p1, c, drawDuration);
    }

    private void OnDrawGizmos()
    {
        if (bvhMesh == null)
        {
            return;
        }

        CameraModel cameraModel = new CameraModel(rayTransform.Pose(), nPixels, focalLength);
        foreach (CameraModel.PixelData pixel in cameraModel.GetRays())
        {
            if (bvhMesh.IntersectRay(pixel.ray, out var lambda, out int nIntersections))
            {
                Vector3 hitpoint = pixel.ray.GetPoint(lambda);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(pixel.ray.origin, pixel.ray.GetPoint(0.25f));
                Gizmos.DrawWireSphere(hitpoint, 0.01f);
            }

            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(pixel.ray.origin, pixel.ray.GetPoint(0.25f));
            }
        }
    }

    private static void DrawTrianglesDebug(BVHNodeMesh node, Vector3 offset)
    {
        // Smoething is upp with this need to debug
        Stack<BVHNodeMesh> stack = new Stack<BVHNodeMesh>();
        stack.Push(node.left);
        while (stack.Count > 0)
        {
            var nodeToSearch = stack.Pop();

            if (nodeToSearch.leaf != null)
            {
                Triangle[] triangles = nodeToSearch.leaf.GetTriangles();

                DrawTriangles(triangles, Color.red, offset);
            }
            else
            {
                if (nodeToSearch.left != null)
                {
                    stack.Push(nodeToSearch.left);
                }

                if (nodeToSearch.right != null)
                {
                    stack.Push(nodeToSearch.right);
                }
            }
        }

        stack = new Stack<BVHNodeMesh>();
        stack.Push(node.right);
        while (stack.Count > 0)
        {
            var nodeToSearch = stack.Pop();

            if (nodeToSearch.leaf != null)
            {
                Triangle[] triangles = nodeToSearch.leaf.GetTriangles();

                DrawTriangles(triangles, Color.green, offset);
            }
            else
            {
                if (nodeToSearch.left != null)
                {
                    stack.Push(nodeToSearch.left);
                }

                if (nodeToSearch.right != null)
                {
                    stack.Push(nodeToSearch.right);
                }
            }
        }
    }

    private static void DrawTriangles(Triangle[] triangles, Color c, Vector3 offset)
    {
        foreach (Triangle triangle in triangles)
        {
            DrawLineDebug(triangle.v0, triangle.v1, c, offset);
            DrawLineDebug(triangle.v1, triangle.v2, c, offset);
            DrawLineDebug(triangle.v2, triangle.v0, c, offset);
        }
    }

    private static void DrawBoundDebug(Bounds bounds, Color c, Vector3 offset)
    {
        Vector3[] boundsVertices = BVHUtils.CalculateWorldBoxCorners(bounds);
        for (int i = 0; i < 4; i++)
        {
            Vector3 c1 = boundsVertices[i];
            Vector3 c2 = boundsVertices[(i + 1) % 4];
            DrawLineDebug(c1, c2, c, offset);

            Vector3 c3 = boundsVertices[4 + i];
            Vector3 c4 = boundsVertices[4 + (i + 1) % 4];

            DrawLineDebug(c3, c4, c, offset);
            DrawLineDebug(c1, c3, c, offset);
        }
    }

    private static void DrawLineDebug(Vector3 p0, Vector3 p1, Color c, Vector3 offset)
    {
        Debug.DrawLine(offset + p0, offset + p1, c, 30);
    }
}