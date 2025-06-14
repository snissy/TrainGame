using AmbientOcclusion.Geometry.Scripts.OcclusionTool;
using UnityEngine;

namespace AmbientOcclusion.Geometry.Scripts.ComputeShaders
{
    public class ComputeShaderHandler : MonoBehaviour
    {

        [SerializeField, Range(8, 8192)] private int imgSize;
        [SerializeField, Range(0.5f, 5f)] private float focalLenght;
        [SerializeField, Range(1/60f, 20)] private float drawTimeLenght;
        
        [SerializeField] private Transform ray;
        [SerializeField] private MeshRenderer meshToTest;
        [SerializeField] private ComputeShader computeShader;
        [SerializeField] private Gradient gradient;
        
        private int kernel;
        
        private static readonly int N_RAYS = Shader.PropertyToID("_N_Rays");
        private static readonly int CAMERA_DEFINITION = Shader.PropertyToID("CameraDefinition");
        
        private static readonly int TRIANGLE_INDICES = Shader.PropertyToID("_Triangle_Indices");
        private static readonly int TRIANGLES = Shader.PropertyToID("_Triangles");
        private static readonly int BVH_NODES = Shader.PropertyToID("_BVH_Nodes");
        
        private static readonly int RESULT_VALUES = Shader.PropertyToID("_Result_Values");
        private static readonly int CAMERA_CONSTANTS = Shader.PropertyToID("_CAMERA_DEFINITION");
        
        private BVHMesh bvhMesh;
        private BVHMeshGPUInstance bvhGPUInstance;
        
        [ContextMenu("SET KERNEL INDEX")]

        private void SetKernelIndex() {
            Debug.Log("Finding Kernel");
            kernel = computeShader.FindKernel("CSMain");
        }
        
        [ContextMenu("Create BVH MESH")]

        private void CreateBvhMesh() {
            bvhMesh = new BVHMesh(meshToTest);
            bvhGPUInstance = bvhMesh.GetGPUInstance();
            
            ComputeBuffer triangleIndicesBuffer = new ComputeBuffer(bvhGPUInstance.triangleIndices.Length, SizeOfDefinitions.INT, ComputeBufferType.Structured);
            triangleIndicesBuffer.SetData(bvhGPUInstance.triangleIndices);

            ComputeBuffer triangleBuffer = new ComputeBuffer(bvhGPUInstance.triangleArray.Length, SizeOfDefinitions.TRIANGLE_SIZE, ComputeBufferType.Structured);
            triangleBuffer.SetData(bvhGPUInstance.triangleArray);

            ComputeBuffer bvhNodeBuffer = new ComputeBuffer(bvhGPUInstance.Nodes.Length, SizeOfDefinitions.BVH_ARRAY_NODE, ComputeBufferType.Structured);
            bvhNodeBuffer.SetData(bvhGPUInstance.Nodes);
            
            computeShader.SetBuffer(kernel, TRIANGLE_INDICES, triangleIndicesBuffer);
            computeShader.SetBuffer(kernel, TRIANGLES, triangleBuffer);
            computeShader.SetBuffer(kernel, BVH_NODES, bvhNodeBuffer);
            
            Debug.Log("BVH created");
        }
        
        [ContextMenu("Run TestComputeShader")]
        private void TestComputeShader() {

            if (bvhMesh == null) {
                Debug.Log("Make BVH MESH First");
                return;
            }       
            
            float startTime = Time.realtimeSinceStartup;
            
            CameraModel cameraModel = new CameraModel(ray.Pose(), imgSize, focalLenght);
            CameraModel.CameraDefinition cameraDefinition = cameraModel.GetCameraDefinition();
            Texture2D texture = new(cameraModel.ImageWidth, cameraModel.ImageHeight);
            int nRays = cameraModel.NPixels;
            
            ComputeBuffer cameraBuffer = new ComputeBuffer(1, SizeOfDefinitions.CAMERA_DEFINITION_SIZE, ComputeBufferType.Structured);
            cameraBuffer.SetData(new[] { cameraDefinition });
            
            float[] resultSpace = new float[nRays];
            ComputeBuffer resultBuffer = new ComputeBuffer(resultSpace.Length, SizeOfDefinitions.FLOAT, ComputeBufferType.Structured);
            
            computeShader.SetInt(N_RAYS, nRays);
            computeShader.SetBuffer(kernel, CAMERA_CONSTANTS, cameraBuffer);
            computeShader.SetBuffer(kernel, RESULT_VALUES, resultBuffer);
            computeShader.GetKernelThreadGroupSizes(kernel, out uint xDim, out uint yDim, out _);
            
            int threadGroups = Mathf.CeilToInt((float) nRays / xDim);
            computeShader.Dispatch(kernel, threadGroups, 1, 1);
            resultBuffer.GetData(resultSpace);
            
            Debug.Log($"Done {Time.realtimeSinceStartup - startTime}");
            
            startTime = Time.realtimeSinceStartup;
            
            for (var i = 0; i < resultSpace.Length; i++) {
                int pixelHeight = i / cameraDefinition.imageWidth;
                int pixelWidth = i % cameraDefinition.imageWidth;
                texture.SetPoint(pixelWidth, pixelHeight, gradient.Evaluate(resultSpace[i]));
            }

            texture.Apply();
            
            // Save the image
            texture.SaveImage(@"C:\Users\Nils\Desktop\", "computeShaderTest");
            
            Debug.Log($"Making IMG {Time.realtimeSinceStartup - startTime}");
            
            resultBuffer.Dispose();
            cameraBuffer.Dispose();
        }

        private float GammaFunction(float x, float p) {
            float fx = Mathf.Pow(x, p);
            float fOneMinusX = Mathf.Pow(1 - x, p);
            return fx / (fx + fOneMinusX);
        }
        
        private void DrawLine(Vector3 p0, Vector3 p1, Color c) {
            Debug.DrawLine(p0, p1, c, drawTimeLenght);
        }

        private void OnDrawGizmos()
        {
            CameraModel cameraModel = new CameraModel(ray.Pose(), imgSize, focalLenght);

            CameraModel.ViewDefinition viewDefinition = cameraModel.GetViewDefinition();
            
            DrawLine(viewDefinition.center, viewDefinition.p00, Color.yellow);
            DrawLine(viewDefinition.center, viewDefinition.p01, Color.yellow);
            DrawLine(viewDefinition.center, viewDefinition.p10, Color.yellow);
            DrawLine(viewDefinition.center, viewDefinition.p11, Color.yellow);
            
            DrawLine(viewDefinition.p00, viewDefinition.p01, Color.yellow);
            DrawLine(viewDefinition.p01, viewDefinition.p11, Color.yellow);
            DrawLine(viewDefinition.p11, viewDefinition.p10, Color.yellow);
            DrawLine(viewDefinition.p10, viewDefinition.p00, Color.yellow);
            
        }
    }
}