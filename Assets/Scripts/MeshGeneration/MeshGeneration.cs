using System.Collections.Generic;
using Common;
using TrackGenerator.Path;
using UnityEngine;

namespace MeshGeneration {
    
    public class MeshGeneration {
        
        private readonly Path path;
        private float width;

        public MeshGeneration(Path path, float width) {
            this.path = path;
            this.width = width*0.5f;
        }
        
        public Mesh GenerateMesh() {

            Mesh res = new Mesh();

            float length = path.Length;
            int numberOfSamples = Mathf.CeilToInt(length / 0.15f);
            
            LinSpace sampleSpace = new LinSpace(0, 1, numberOfSamples);
            
            float timeStep = sampleSpace.array[0];
            float timeStepPlusOne = sampleSpace.array[1];

            Vector3 step = path.GetPoint(timeStep);
            Vector3 stepPlusOne = path.GetPoint(timeStepPlusOne);
            
            Vector3 dir = path.GetDirection(timeStep) * width;
            Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized * width;
            
            Vector3 v0 = step - perp;
            Vector3 v1 = step + perp;
            
            Vector3 v2 = stepPlusOne - perp;
            Vector3 v3 = stepPlusOne + perp;
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            
            vertices.AddRange(new [] {
                v0, v1, v2, v3
            });
            
            int lastVertexIndex = 3;
            
            int v0Index = lastVertexIndex - 3;
            int v1Index = lastVertexIndex - 2;
            int v2Index = lastVertexIndex - 1;
            int v3Index = lastVertexIndex;
            
            triangles.AddRange(new [] {
                v1Index,v2Index, v0Index,
                v1Index, v3Index, v2Index
            });
            
            for (int i = 1; i < sampleSpace.array.Length - 1; i++) {

                timeStepPlusOne = sampleSpace.array[i + 1];
                
                stepPlusOne = path.GetPoint(timeStepPlusOne);
            
                dir = path.GetDirection(timeStepPlusOne) * width;
                perp = Vector3.Cross(dir, Vector3.up).normalized * width;
                
                v2 = stepPlusOne - perp;
                v3 = stepPlusOne + perp;
                
                vertices.AddRange(new [] {
                    v2, v3
                });

                lastVertexIndex += 2;

                v0Index = lastVertexIndex - 3;
                v1Index = lastVertexIndex - 2;
                v2Index = lastVertexIndex - 1;
                v3Index = lastVertexIndex;
            
                triangles.AddRange(new [] {
                    v1Index,v2Index, v0Index,
                    v1Index, v3Index, v2Index
                });
                
            }

            res.vertices = vertices.ToArray();
            res.triangles = triangles.ToArray();

            return res;
        }
        
    }
}