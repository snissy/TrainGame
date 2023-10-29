

using System.Collections.Generic;
using Common;
using TrackGenerator.Path;
using UnityEngine;

namespace MeshGeneration {
    public class SleepersMeshGeneration {
        
        private readonly Path path;
        private float width;
        private readonly float stepSize;
        private readonly Vector2[] shapePoints;
        private Quaternion perpRotationXY0 = Quaternion.Euler(0, 0, 90);

        public SleepersMeshGeneration(Path path, PolygonCollider2D polygonShape, float width, float railWidth, float stepSize = 0.25f) {
            
            this.path = path;
            this.width = width;
            this.stepSize = stepSize;

            shapePoints = polygonShape.points;
            
            Bounds shapeBounds = polygonShape.bounds;
            float boundWidth = shapeBounds.extents.x * 2.0f;
            float scaleFactor = railWidth / boundWidth;

            for (var i = 0; i < shapePoints.Length; i++) {
                shapePoints[i]*=scaleFactor;
            }
        }
        
        public Mesh GenerateMesh() {
            
            // TODO fix uv the shading is off
            // Will be out tangent space for now!
            
            float length = path.Length;
            int numberOfSamples = Mathf.CeilToInt(length / stepSize);
            LinSpace sampleSpace = new LinSpace(0, 1, numberOfSamples);

            Mesh rootMesh = new Mesh() {
                name = "Train Track Mesh"
            };

            Vector3 sideVector = Vector3.right * width*0.5f;
            int nSegments = shapePoints.Length;
            CombineInstance[] combine = new CombineInstance[nSegments];

            for (var i = 0; i < nSegments; i++) {
                
                LineSegment segment = new LineSegment(shapePoints[i].SwizzleXY0(), shapePoints[(i + 1) % nSegments].SwizzleXY0());

                combine[i].mesh = MakeLineMesh(sampleSpace, segment, sideVector);
                combine[i].transform = Matrix4x4.identity;
            }
            
            rootMesh.CombineMeshes(combine);
            rootMesh.Optimize();
            
            return rootMesh;
        }

        private Mesh MakeLineMesh(LinSpace sampleSpace, LineSegment lineSegment, Vector3 centerOffset) {
            
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();
            
            float timeStep = sampleSpace.array[0];

            Quaternion rotation = Quaternion.LookRotation(path.GetDirection(timeStep), Vector3.up);
            
            LineSegment rotatedSegment = rotation * lineSegment;
            Vector3 rotatedOffset = rotation * centerOffset;
            
            Vector3 step = path.GetPoint(timeStep) + rotatedOffset;
            Vector3 stepPlusOne = step - rotatedOffset;
            
            // LAST EDIT
            
            Vector3 v0 = step + rotatedSegment.End;
            Vector3 v1 = step + rotatedSegment.Start;

            int lastVertexIndex = 3;

            int v0Index = lastVertexIndex - 3;
            int v1Index = lastVertexIndex - 2;
            int v2Index = lastVertexIndex - 1;
            int v3Index = lastVertexIndex;
            
            vertices.AddRange(new[] {
                v0, v1
            });

            Vector3 normal = perpRotationXY0 * rotatedSegment.Dir;
            
            normals.AddRange( new[] {
                normal, normal, normal, normal
            });

            triangles.AddRange(new[] {
                v1Index, v2Index, v0Index,
                v1Index, v3Index, v2Index
            });

            Quaternion lastRotation = rotation;

            int circleIndex = sampleSpace.array.Length - 2;
            
            for (int i = 1; i < sampleSpace.array.Length - 1; i++) {
                
                timeStepPlusOne = sampleSpace.array[i + 1];
                rotation = Quaternion.LookRotation(path.GetDirection(timeStepPlusOne), Vector3.up);
                if (i!= circleIndex && lastRotation == rotation) {
                    continue;
                }
                lastRotation = rotation;
                
                rotatedSegment = rotation * lineSegment;
                rotatedOffset = rotation * centerOffset;
                
                stepPlusOne = path.GetPoint(timeStepPlusOne) + rotatedOffset;
                
                v2 = stepPlusOne + rotatedSegment.End;
                v3 = stepPlusOne + rotatedSegment.Start;
                
                normal = perpRotationXY0 * rotatedSegment.Dir;

                lastVertexIndex += 2;

                v0Index = lastVertexIndex - 3;
                v1Index = lastVertexIndex - 2;
                v2Index = lastVertexIndex - 1;
                v3Index = lastVertexIndex;
                
                vertices.AddRange(new[]{
                    v2, v3
                });
                
                normals.AddRange( new[] {
                    normal, normal
                });

                triangles.AddRange(new[] {
                    v1Index, v2Index, v0Index,
                    v1Index, v3Index, v2Index
                });
                
            }
            
            Mesh res = new Mesh() {
                name = "Sub Mesh",
                vertices = vertices.ToArray(),
                normals = normals.ToArray(),
                triangles = triangles.ToArray(),
            };
            res.Optimize();
            
            return res;
        }
    }
}