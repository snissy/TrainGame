using System.Collections.Generic;
using System.Linq;
using AmbientOcclusion.Geometry;
using Matoya.Common;
using Matoya.Common.Geometry;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using MathUtils = AmbientOcclusion.Geometry.MathUtils;

namespace AmbientOcclusion.OcclusionTool
{
    public class SceneAmbientOcclusion : MonoBehaviour
    {
        private const float GOLDEN_RATIO = 1.61803398875f;
        private const float DELTA = 1e-9f;
        
        private static readonly int Ambient = Shader.PropertyToID("_MainTex");

        [SerializeField, Range(2, 2048)] private int textureSize;

        [SerializeField, Range(0, 256)] private int nSamples;
        
        private BVHScene scene;
        private List<Vector3> samplePoints;
        private float pixelWidth => 1.0f / textureSize;
        private float pixelDelta => pixelWidth * 0.5f;
        
        [ContextMenu("CreateAmbientScene")]

        public void CreateAmbientScene() {
            
            List<MeshRenderer> meshRenderers = FindObjectsOfType<MeshRenderer>().ToList(); 
            
            scene = new BVHScene(meshRenderers);
            samplePoints = CreateSamplingPattern();
            
            EditorUtility.DisplayProgressBar("Making Ambient Map", "Starting map creation", 0.01f);
            
            for (var i = 0; i < meshRenderers.Count; i++) {
                MeshRenderer visual = meshRenderers[i];
                EditorUtility.DisplayProgressBar("Making Ambient Map", $"Created map for {visual.name}", (float) i / meshRenderers.Count);
                Texture2D ambientTexture = CreateAmbientTexture(visual);
                Material material = visual.sharedMaterial;
                Material instanceMaterial = new Material(material.shader);
                instanceMaterial.name = $"AmbientMaterial_{visual.name}";
                instanceMaterial.SetTexture(Ambient, ambientTexture);
                visual.material = instanceMaterial;
            }
            
            EditorUtility.ClearProgressBar();
        }
        
        [ContextMenu("CreatAmbientSceneVertex")]

        public void CreateAmbientSceneVertex() {
            
            EditorUtility.DisplayProgressBar("Making Ambient Vertex Map", "Starting creation", 0.00f);
            
            List<MeshRenderer> meshRenderers = FindObjectsOfType<MeshRenderer>().ToList(); 
            
            scene = new BVHScene(meshRenderers);
            samplePoints = CreateSamplingPattern();
            
            EditorUtility.DisplayProgressBar("Making Ambient Vertex Map", "Starting creation", 0.01f);

            for (int index = 0; index < meshRenderers.Count; index++) {
                MeshRenderer visual = meshRenderers[index];
                VertexPaintMesh(visual);
                EditorUtility.DisplayProgressBar("Making Ambient Map", $"Created map for {visual.name}", (float) index / meshRenderers.Count);
            }

            EditorUtility.ClearProgressBar();
        }

        private void VertexPaintMesh(MeshRenderer visual) {

            MeshFilter meshFilter = visual.GetComponent<MeshFilter>();
            Mesh meshInstance = meshFilter.sharedMesh;

            int nVertices = meshInstance.vertices.Length;
            Color[] colors = new Color[nVertices];
            
            
            foreach (Vertex vertex in visual.GetVertexEnumerator()) {
                
                float ambientValue = 0.0f;
                            
                foreach (Vector3 sampleVector in samplePoints) {
                                
                    Vector3 globalDir = vertex.normalLookRotiation * sampleVector;
                    
                    Ray testRay = new Ray(vertex.worldPosition + globalDir*1e-3f, globalDir);

                    float sampleValueToUse;
                    if (scene.IntersectRay(testRay, out float hitLambda, out MeshRenderer hitMesh, out int nTests) && hitLambda < 1.0f) {
                        sampleValueToUse = hitLambda;
                    }
                    else {
                        sampleValueToUse = 1.0f;
                    }
                    ambientValue += sampleValueToUse;
                }
                
                ambientValue /= nSamples;
                colors[vertex.index] = Color.white * ambientValue;
                
                EditorUtility.DisplayProgressBar("Making Ambient Map", $"Created map for {visual.name}", (float) vertex.index / nVertices);
            }
            
            meshInstance.SetColors(colors);
            meshFilter.mesh = meshInstance;
        }

        private List<Vector3> CreateSamplingPattern() {

            List<Vector3> pointList = new List<Vector3>();
            
            for (int i = 0; i < nSamples; i++) {
                float iOverG = i / GOLDEN_RATIO;
                float xGolden = iOverG % 1.0f;
                float yGolden = (float) i / nSamples;

                float a = Mathf.Sqrt(xGolden);
                float y2Pi = Mathf.PI * 2.0f * yGolden;
                float cosYGolden = Mathf.Cos(y2Pi);
                float sinYGolden = Mathf.Sin(y2Pi);
                float b = Mathf.Sqrt(1.0f - xGolden);

                Vector3 samplePoint = new Vector3(a * cosYGolden,a * sinYGolden, b);
                
                pointList.Add(samplePoint);
            }

            return pointList;
        }
        
        private Texture2D CreateAmbientTexture(MeshRenderer visual) {
            
            Texture2D texture = new Texture2D(textureSize, textureSize);
            foreach (Triangle triangle in visual.GetTriangleEnumerator()) {
                // Unity uses a clockwise winding order, 
                Edge edge01 = new Edge(triangle.uv0, triangle.uv1);
                Edge edge12 = new Edge(triangle.uv1, triangle.uv2);
                Edge edge20 = new Edge(triangle.uv2, triangle.uv0);
                
                Vector2 uvBoundSize = triangle.uvMax - triangle.uvMin;
                
                float width = uvBoundSize.x;
                float height = uvBoundSize.y;
                
                Vector2 flooredStart = new Vector2(
                    Mathf.Floor(triangle.uvMin.x * textureSize) / textureSize, 
                    Mathf.Floor(triangle.uvMin.y * textureSize) / textureSize
                    );
                
                int widthStep = Mathf.CeilToInt(width / pixelWidth);
                int heightStep = Mathf.CeilToInt(height / pixelWidth);
                
                for (int i = 0; i <= widthStep; i++) {
                    for (int j = 0; j <= heightStep; j++) {
                        Vector2 point = flooredStart + new Vector2(i * pixelWidth + pixelDelta  , j * pixelWidth + pixelDelta);
                        
                        if (PointInTriangle(edge01, edge12, edge20, point)) {
                            
                            float u = MathUtils.TriangleArea(triangle.uv2, triangle.uv0, point) / triangle.uvArea;
                            float v = MathUtils.TriangleArea(triangle.uv0, triangle.uv1, point) / triangle.uvArea;
                            float w = MathUtils.TriangleArea(triangle.uv1, triangle.uv2, point) / triangle.uvArea;
                            
                            Vector3 worldPosForUv = w*triangle.v0 + u*triangle.v1 + v*triangle.v2;
                            
                            float ambientValue = 0.0f;
                            
                            foreach (Vector3 sampleVector in samplePoints) {
                                
                                Vector3 globalDir = triangle.faceRotation * sampleVector;
                                Ray testRay = new Ray(worldPosForUv + globalDir*1e-3f, globalDir);

                                float sampleValueToUse;
                                if (scene.IntersectRay(testRay, out float hitLambda, out MeshRenderer hitMesh, out int nTests) && hitLambda < 1.0f) {
                                    sampleValueToUse = hitLambda;
                                }
                                else {
                                    sampleValueToUse = 1.0f;
                                }

                                ambientValue += sampleValueToUse;
                            }

                            ambientValue /= nSamples;
                            
                            int x = Mathf.FloorToInt(point.x * textureSize);
                            int y = Mathf.FloorToInt(point.y * textureSize);

                            texture.SetPixel(x, y, Color.HSVToRGB(1, 0, ambientValue));
                        }
                    }
                }
            }
            
            texture.Apply();
            texture.SaveImage($@"C:\Users\Nils\Desktop\Textures\", visual.name);
            
            return texture;
        }

        private bool PointInTriangle(Edge edge0, Edge edge1, Edge edge2, Vector2 point) {
            return CrossProduct(edge0.dir, point - edge0.start) < DELTA &&
                   CrossProduct(edge1.dir, point - edge1.start) < DELTA &&
                   CrossProduct(edge2.dir, point - edge2.start) < DELTA;
        }
        
        private float CrossProduct(Vector2 a, Vector2 b) {
            return a.x * b.y - a.y * b.x;
        }
        

        private float TriangleArea(Vector2 a, Vector2 b, Vector2 c) {
            // Calculate vectors AB, AC, and BC
            Vector2 ab = b - a;
            Vector2 ac = c - a;
            return Mathf.Abs(CrossProduct(ab, ac)) * 0.5f;
        }
        
        
        
    }
}