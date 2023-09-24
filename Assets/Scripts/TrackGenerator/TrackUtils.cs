using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace DefaultNamespace.TrackGenerator
{
    
    public static class TrackUtils
    {
        public struct Disk {
            public Vector3 center;
            public float radius;

            public Vector3 v1;
            public Vector3 v2;
            public Vector3 v3;
        }
        
        public static List<Disk> DiskPoints = new List<Disk>(); 
        
        public static List<Vector3> CornerCutting(List<Vector3> trackPoints, int nIterations, float sFactor) {
            
            float s1 = sFactor;
            float s2 = (1 - sFactor);
            List<Vector3> newList = new List<Vector3>();

            for (int k = 0; k < nIterations; k++)
            {
                int numberOfPoints = trackPoints.Count;

                newList.Capacity = 2 * numberOfPoints;

                for (int i = 0; i < numberOfPoints; i++)
                {
                    Vector3 p1 = trackPoints[i];
                    Vector3 p2 = trackPoints[(i + 1) % numberOfPoints];

                    newList.Add(p1 * s2 + p2 * s1);
                    newList.Add(p1 * s1 + p2 * s2);
                }

                trackPoints.Clear();
                trackPoints.AddRange(newList);
                newList.Clear();
            }
            
            return trackPoints;
        }

        public static List<Vector3> CircleCorner(List<Vector3> trackPoints){
            
            // cut track segments;
            DiskPoints.Clear();
            
            int nPoints = trackPoints.Count;

            List<Vector3> resultList = new List<Vector3>();

            if (nPoints < 3) {
                throw new Exception("You need to pass a control polygon that has more than 2 vertices");
            }
            
            Vector3 TangentPoint(Vector3 pA, float ra, Vector3 pB, float rb) {
                return (rb * pA + ra * pB)/(ra+rb);
            }


            List<Vector3> trackPointsCopy = new List<Vector3>(trackPoints);
            Vector3 A;
            Vector3 B;
            Vector3 C;
            float a;
            float b;
            float c;
            
            float norm;

            float s ;

            float radius;

            Vector3 incirclePoint;
                
            float rA;
            float rB;
            float rC;
                
            Vector3 start;
            Vector3 end;
            
            int nCurvePoints;

            float nCurvePointsF;

            for (int i = 0; i < nPoints - 1 ; i++) {
                
                A = trackPointsCopy[i];
                B = trackPointsCopy[(i + 1) % nPoints];
                C = trackPointsCopy[(i + 2) % nPoints];

                // calculate incircle
                
                a = Vector3.Distance(B, C);
                b = Vector3.Distance(C, A);
                c = Vector3.Distance(A, B);
                
                norm = a + b + c;

                s = norm * 0.5f;

                radius = Mathf.Sqrt((s - a) * (s - b) * (s - c) * (1.0f / s));

                incirclePoint = (a * A + b * B + c * C)*(1.0f /norm);
                
                rA = 0.5f * (b + c - a);
                rB = 0.5f * (a + c - b);
                rC = 0.5f * (a + b - c);
                
                start = TangentPoint(A, rA, B, rB) - incirclePoint;
                end = TangentPoint(B, rB, C, rC) - incirclePoint;
                
                nCurvePoints = 50;
                
                nCurvePointsF = nCurvePoints; 
                
                for (int j = 0; j <= nCurvePoints; j++)
                {
                    float t = j / (nCurvePointsF);
                    resultList.Add(incirclePoint+Vector3.Slerp(start, end, t));
                }
                
                trackPointsCopy[(i + 1) % nPoints] = resultList[^1];
                
                DiskPoints.Add(new Disk() {
                    center = incirclePoint,
                    radius = radius,
                    v1 = A,
                    v2 = B,
                    v3 = C
                });
            }
            
            A = resultList[^1];
            B = trackPointsCopy[0];
            C = resultList[0];

            // calculate incircle
                
            a = Vector3.Distance(B, C);
            b = Vector3.Distance(C, A);
            c = Vector3.Distance(A, B);
                
            norm = a + b + c;

            s = norm * 0.5f;

            radius = Mathf.Sqrt((s - a) * (s - b) * (s - c) * (1.0f / s));

            incirclePoint = (a * A + b * B + c * C)*(1.0f /norm);
                
            rA = 0.5f * (b + c - a);
            rB = 0.5f * (a + c - b);
            rC = 0.5f * (a + b - c);
                
            start = TangentPoint(A, rA, B, rB) - incirclePoint;
            end = TangentPoint(B, rB, C, rC) - incirclePoint;
            
            nCurvePoints = 50;
                
            nCurvePointsF = nCurvePoints; 
                
            for (int j = 0; j <= nCurvePoints; j++)
            {
                float t = j / (nCurvePointsF);
                resultList.Add(incirclePoint+Vector3.Slerp(start, end, t));
            }
            
            DiskPoints.Add(new Disk()
            {
                center = incirclePoint,
                radius = radius,
                v1 = A,
                v2 = B,
                v3 = C
            });
            
            return resultList;
        }

        public static List<Vector3> DouglasPeucker(List<Vector3> pointList, float epsilon) {

            float dMax =  float.MinValue;
            int index = 0;
            int endIndex = pointList.Count - 1;

            Vector3 start = pointList[0];
            Vector3 end = pointList[endIndex];

            for (int i = 1; i < endIndex; i++)
            {
                float d = PerpendicularDistanceToLine(pointList[i], start, end);
                
                if (d > dMax)
                {
                    index = i;
                    dMax = d;
                }
            }
            
            List<Vector3> resultList = new List<Vector3>();

            if (dMax > epsilon)
            {
                List<Vector3> range1 = new List<Vector3>();
                List<Vector3> range2 = new List<Vector3>();

                for (int i = 0; i <= index; i++)
                {
                    range1.Add(pointList[i]);
                }

                for (int i = index; i < pointList.Count; i++)
                {
                    range2.Add(pointList[i]);
                }
                
                List<Vector3> recursiveResult1 = DouglasPeucker(range1, epsilon);
                List<Vector3> recursiveResult2 = DouglasPeucker(range2, epsilon);
                
                for (int i = 0; i < recursiveResult1.Count-1; i++)
                {
                    resultList.Add(recursiveResult1[i]);
                }
                
                for (int i = 0; i < recursiveResult2.Count; i++)
                {
                    resultList.Add(recursiveResult2[i]);
                }
                
            }
            else
            {
                resultList.Add(pointList[0]);
                resultList.Add(pointList[endIndex]);
            }
            
            return resultList;
        }

        public static float PerpendicularDistanceToLine(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 startToPoint = point-start;
            Vector3 pointProjectedOnLine = Vector3.Project(startToPoint, (end - start).normalized);
            return (startToPoint - pointProjectedOnLine).magnitude;
        }
        
        public static float Sigmoid(float distance) {
            return 1.0f / (1.0f + Mathf.Exp(-distance));
        }
        
        public static float DistanceToAllPoint(Vector3 p, List<Vector3> allPoints)
        {
            float totDist = 0.0f;

            foreach (Vector3 pointInCollection in allPoints)
            {
                totDist += Vector3.Distance(p, pointInCollection);
            }
            
            return totDist;
        }
        
    }
}