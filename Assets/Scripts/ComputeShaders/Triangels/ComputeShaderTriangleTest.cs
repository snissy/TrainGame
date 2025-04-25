using System;
using Common;
using DefaultNamespace;
using Matoya.Common.Geometry;
using UnityEngine;

namespace ComputeShaders {
    public class ComputeShaderTriangleTest : MonoBehaviour {
        
        const float GOLDEN_RATIO = 1.618033988f;
        
        // This is my const
        private readonly int N_TRIANGLES_ID = Shader.PropertyToID("_N_TRIANGLES");
        private readonly int N_RAYS_PER_TRIANGLE_ID = Shader.PropertyToID("_N_RAYS_PER_TRIANGLE");
        private readonly int N_SAMPLE_POINTS_ID = Shader.PropertyToID("_N_SAMPLE_POINTS");
        private readonly int OCCLUSION_SAMPLE_SIZE_ID = Shader.PropertyToID("_N_SAMPLE_POINTS");
        
        private readonly int TRIANGLES_BUFFER_ID = Shader.PropertyToID("_Triangles");
        private readonly int GOLDEN_POINTS_ID = Shader.PropertyToID("_Golden_Points");
        private readonly int OCCLUSION_SAMPLE_PATTERN_ID = Shader.PropertyToID("_Occlusion_Sample_Pattern");
        
        private readonly int BVH_TRIANGLE_INDICES_ID = Shader.PropertyToID("_BVH_Triangle_Indices");
        private readonly int BVH_NODES_ID = Shader.PropertyToID("_BVH_Nodes");
        
        private readonly int SAMPLE_POINTS_ID = Shader.PropertyToID("_Sample_Points");
        private readonly int UV_SAMPLE_COORDINATES_ID = Shader.PropertyToID("_UV_Sample_Coordinates");
        private readonly int TRIANGLE_SAMPLE_INDEX_ID = Shader.PropertyToID("_Triangle_Sample_Index");
        private readonly int AMBIENT_SAMPLE_VALUES_ID = Shader.PropertyToID("_Ambient_Sample_Values");

        private readonly int FINAL_RESULT_TEXTURE_ID = Shader.PropertyToID("_Final_Result");
        
        // unity references
        [SerializeField] private MeshRenderer meshToTest;
        [SerializeField] private ComputeShader computeShader;
        [SerializeField, Range(1, 256)] private int nRaysPerTriangle; 
        [SerializeField, Range(1, 256)] private int nOcclusionSamples; 
        
        // fields
        private int kernel;
        private Vector3[] resultRay;
        
        [ContextMenu("Run TestComputeShader")]
        private void TestComputeShader() {
            TimeFunction(RunTriangleComputeShader);
        }

        [ContextMenu("draw ray")]
        private void DrawRays() {
            foreach (Vector3 point in resultRay) {
                DebugDraw.DrawPoint(point, 0.01f, Color.red, 12f);
            }
            Debug.Log("Draw call completed");
        }

        private void RunTriangleComputeShader() {
            
            Debug.Log("Finding Kernel");
            kernel = computeShader.FindKernel("CSMain");
            
            Debug.Log($"Making triangle list for {meshToTest.name}");
            
            BVHMesh bvhMesh = new BVHMesh(meshToTest);
            BVHMeshGPUInstance bvhGPUInstance = bvhMesh.GetGPUInstance();
            
            Triangle[] triangleArray = bvhGPUInstance.triangleArray;
            
            int nTriangles = triangleArray.Length;
            ComputeBuffer triangleBuffer = new ComputeBuffer(triangleArray.Length, SizeOfDefinitions.TRIANGLE_SIZE, ComputeBufferType.Structured);
            triangleBuffer.SetData(triangleArray);
            
            ComputeBuffer triangleIndicesBuffer = new ComputeBuffer(bvhGPUInstance.triangleIndices.Length, SizeOfDefinitions.INT, ComputeBufferType.Structured);
            triangleIndicesBuffer.SetData(bvhGPUInstance.triangleIndices);

            ComputeBuffer bvhNodeBuffer = new ComputeBuffer(bvhGPUInstance.Nodes.Length, SizeOfDefinitions.BVH_ARRAY_NODE, ComputeBufferType.Structured);
            bvhNodeBuffer.SetData(bvhGPUInstance.Nodes);
            
            Vector2[] goldenPointsArray = GetGoldenPointsArray(nRaysPerTriangle);

            ComputeBuffer goldPointBuffer = new ComputeBuffer(nRaysPerTriangle, SizeOfDefinitions.VECTOR2_SIZE, ComputeBufferType.Structured);
            goldPointBuffer.SetData(goldenPointsArray);

            Vector3[] occlusionSampleArray = GetOcclusionSamplePatter(nOcclusionSamples);
            
            ComputeBuffer occlusionSamplePatternBuffer = new ComputeBuffer(nOcclusionSamples, SizeOfDefinitions.VECTOR3_SIZE, ComputeBufferType.Structured);
            occlusionSamplePatternBuffer.SetData(occlusionSampleArray);
            
            int nSamplePoint = triangleArray.Length * nRaysPerTriangle;
            ComputeBuffer samplePointBuffer = new ComputeBuffer(nSamplePoint, SizeOfDefinitions.VECTOR3_SIZE, ComputeBufferType.Structured);
            ComputeBuffer uvSampleCoordinates = new ComputeBuffer(nSamplePoint, SizeOfDefinitions.VECTOR2_SIZE, ComputeBufferType.Structured);
            ComputeBuffer triangleSampleIndex = new ComputeBuffer(nSamplePoint, SizeOfDefinitions.INT, ComputeBufferType.Structured);
            ComputeBuffer ambientSamplePoint = new ComputeBuffer(nSamplePoint, SizeOfDefinitions.FLOAT, ComputeBufferType.Structured);

            Texture2D resultTexture = new Texture2D(1024, 1024);
            
            computeShader.SetInt(N_RAYS_PER_TRIANGLE_ID, nRaysPerTriangle);
            computeShader.SetInt(N_TRIANGLES_ID, nTriangles);
            computeShader.SetInt(N_SAMPLE_POINTS_ID, nSamplePoint);
            computeShader.SetInt(OCCLUSION_SAMPLE_SIZE_ID, nOcclusionSamples);
            
            computeShader.SetBuffer(kernel, TRIANGLES_BUFFER_ID, triangleBuffer);
            computeShader.SetBuffer(kernel, GOLDEN_POINTS_ID, goldPointBuffer);
            computeShader.SetBuffer(kernel, OCCLUSION_SAMPLE_PATTERN_ID, occlusionSamplePatternBuffer);
            
            computeShader.SetBuffer(kernel, BVH_TRIANGLE_INDICES_ID, triangleIndicesBuffer);
            computeShader.SetBuffer(kernel, BVH_NODES_ID, bvhNodeBuffer);
            
            computeShader.SetBuffer(kernel, SAMPLE_POINTS_ID, samplePointBuffer);
            computeShader.SetBuffer(kernel, UV_SAMPLE_COORDINATES_ID ,uvSampleCoordinates);
            computeShader.SetBuffer(kernel, TRIANGLE_SAMPLE_INDEX_ID ,triangleSampleIndex);
            computeShader.SetBuffer(kernel, AMBIENT_SAMPLE_VALUES_ID ,ambientSamplePoint);
            
            computeShader.SetTexture(kernel, FINAL_RESULT_TEXTURE_ID, resultTexture);
            
            computeShader.GetKernelThreadGroupSizes(kernel, out uint xDim, out uint yDim, out _);
            int threadGroups = Mathf.CeilToInt((float) nTriangles / xDim);
            Debug.Log($"Threads group {threadGroups}");
            computeShader.Dispatch(kernel, threadGroups, 1, 1);
            
            resultRay = new Vector3[nSamplePoint];
            samplePointBuffer.GetData(resultRay);
            
            
            triangleBuffer.Dispose();
            triangleIndicesBuffer.Dispose();
            bvhNodeBuffer.Dispose();
            goldPointBuffer.Dispose();
            occlusionSamplePatternBuffer.Dispose();
            samplePointBuffer.Dispose();
            uvSampleCoordinates.Dispose();
            triangleSampleIndex.Dispose();
            ambientSamplePoint.Dispose();
        }

        private Vector3[] GetOcclusionSamplePatter(int nPoints) {
            Vector3[] pointList = new Vector3[nPoints];
            for (int i = 0; i < nPoints; i++) {
                float iOverG = i / GOLDEN_RATIO;
                float xGolden = iOverG % 1.0f;
                float yGolden = (float) i / nPoints;
                float a = Mathf.Sqrt(xGolden);
                float y2Pi = Mathf.PI * 2.0f * yGolden;
                float cosYGolden = Mathf.Cos(y2Pi);
                float sinYGolden = Mathf.Sin(y2Pi);
                float b = Mathf.Sqrt(1.0f - xGolden);
                pointList[i] =  new Vector3(a * cosYGolden,a * sinYGolden, b);;
            }
            return pointList;
        }

        private Vector2[] GetGoldenPointsArray(int nPoints) {
            Vector2[] goldenPointsArray = new Vector2[nPoints];
            for (int i = 1; i <= nPoints; i++) {
                goldenPointsArray[i-1] = new Vector2(MathFunctions.Frac(i/GOLDEN_RATIO), i / (float) nPoints);
            }
            return goldenPointsArray;
        }

        private void TimeFunction(Action function) {
            string nameOfFunction = function.Method.Name;
            float startTime = Time.realtimeSinceStartup;
            Debug.Log($"Timing function:: Starting\t{nameOfFunction}");
            function.Invoke();
            Debug.Log($"Timing function:: Done\t{nameOfFunction}: {Time.realtimeSinceStartup - startTime}");
        }
    }
}