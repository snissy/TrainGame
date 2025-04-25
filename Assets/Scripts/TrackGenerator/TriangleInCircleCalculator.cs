using UnityEngine;
using System.Collections.Generic;
using Common;
using DefaultNamespace;
using MathNet.Numerics.LinearAlgebra;

namespace TrackGenerator {
    
    public struct TriangleDisk
    {
        public Vector3 Center;
        public float Radius;
        public Vector3[] TriangleVertices;
        public Vector3[] TouchPoints;
    }
    public static class TriangleInCircleCalculator {
    
        private static Vector3 TangentPoint(Vector3 pA, float ra, Vector3 pB, float rb) {
            return (rb * pA + ra * pB)/(ra+rb);
        }
    
        public static TriangleDisk GetDisk(Vector3 A, Vector3 B, Vector3 C) {
           // calculate incircle
           
          float  a = Vector3.Distance(B, C);
          float b = Vector3.Distance(C, A);
          float c = Vector3.Distance(A, B);
           
          float norm = a + b + c;

          float s = norm * 0.5f;

          float radius = Mathf.Sqrt((s - a) * (s - b) * (s - c) * (1.0f / s));

          Vector3 incirclePoint = (a * A + b * B + c * C)*(1.0f /norm);
           
          float rA = 0.5f * (b + c - a);
          float rB = 0.5f * (a + c - b);
          float rC = 0.5f * (a + b - c);
           
          Vector3 start = TangentPoint(A, rA, B, rB);
          Vector3 end = TangentPoint(B, rB, C, rC);
          Vector3 extra = TangentPoint(C, rC, A, rA);
           
          return new TriangleDisk(){
           Center = incirclePoint,
           Radius = radius,
           TriangleVertices = new Vector3[] {A, B, C},
           TouchPoints = new Vector3[] {start, end, extra},
          };
   
        }

        public static List<TriangleDisk> GetDiskList(this List<Vector3> points) {
        
            List<TriangleDisk> res = new List<TriangleDisk>();

            int nPoints = points.Count;
            
            for (var i = 0; i < points.Count; i++) {
                Vector3 p1 = points[i];
                Vector3 p2 = points[(i + 1) % nPoints];
                Vector3 p3 = points[(i + 2) % nPoints];

                Vector3 p12 = (p1 + p2) * 0.5f;
                Vector3 p23 = (p2 + p3) * 0.5f;

                res.Add(GetDisk(p12, p2, p23));                
            }
    
            return res;
        }
        
        public static List<Vector3> GetDiskTrack(this List<Vector3> points){
        
            List<TriangleDisk> disks = points.GetDiskList();
    
            List<Vector3> finalTrack = new List<Vector3>();
            
            foreach (TriangleDisk disk in disks) {
    
                Vector3 firstTouch = disk.TouchPoints[0];
                Vector3 secondTouch = disk.TouchPoints[1];
    
                Vector3 diskToFirstTouch = firstTouch - disk.Center;
                Vector3 diskToSecondTouch = secondTouch - disk.Center;
                
                float[] tSteps = MathFunctions.LinSpace(0, 1.0f, 100);

                foreach (float step in tSteps) {
                    finalTrack.Add(disk.Center + Vector3.Slerp(diskToFirstTouch, diskToSecondTouch, step));
                }
            }
        
            return finalTrack;
        }
    }
}