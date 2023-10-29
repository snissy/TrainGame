using System.Collections.Generic;
using Common;
using UnityEngine;

namespace TrackGenerator {
    public static class Generator { 
        private static readonly System.Random Random = new ();
        public static List<Vector3> GetRandomTrack(float distanceLimit, bool angleCheck, float angleLim) {
            
            List<Vector3> okPoint = GetRandomPointList(distanceLimit);
            
            SortOnCenter(okPoint);
            SwitchPositionNoIntersection(okPoint);
            
            if (angleCheck) {
                AngleCheck(okPoint, angleLim);
            }
            
            return okPoint; 
        }

        private static void SortOnCenter(List<Vector3> points) {
            Vector3 centerPoint = points.GetCenterPoint();
            points.SortByAngularDifference(centerPoint);
        }

        private static void AngleCheck(List<Vector3> points, float angleLim) {
            Debug.Log($"Number of points before angle check {points.Count}");
            bool noAngleError = false;
            while (!noAngleError && points.Count >= 3) {
                
                for (var i = 0; i < points.Count; i++) {
                
                    int nOkPoints = points.Count;

                    Vector3 p1 = points[i];
                    Vector3 p2 = points[(i + 1) % nOkPoints];
                    Vector3 p3 = points[(i + 2) % nOkPoints];

                    float angleValue = Vector3.Angle(p1 - p2, p3 - p2);

                    if (angleValue < angleLim || angleValue > 180 - angleLim) {
                        points.RemoveAt((i + 1) % nOkPoints);
                    }
                }
                
                noAngleError = AngleErrorExist(points, angleLim);
            }

            Debug.Log($"Number of points after angle check {points.Count}");
        }

        private static bool AngleErrorExist(List<Vector3> points, float angleLim) {
            
            for (var i = 0; i < points.Count; i++) {
                
                int nOkPoints = points.Count;

                Vector3 p1 = points[i];
                Vector3 p2 = points[(i + 1) % nOkPoints];
                Vector3 p3 = points[(i + 2) % nOkPoints];

                float angleValue = Vector3.Angle(p1 - p2, p3 - p2);

                if (angleValue < angleLim || angleValue > 180 - angleLim) {

                    return false;
                }
            }

            return true;
        }

        private static void SwitchPositionNoIntersection(List<Vector3> okPoint) {
            
            for (int i = 0; i < 10000; i++) {
                int swap1 = GetRandomIndex(okPoint);
                int swap2 = GetRandomIndex(okPoint);

                Vector3 swap1Vec = okPoint[swap1];
                Vector3 swap2Vec = okPoint[swap2];

                okPoint[swap1] = swap2Vec;
                okPoint[swap2] = swap1Vec;

                if (LineIntersect(okPoint)) {
                    okPoint[swap1] = swap1Vec;
                    okPoint[swap2] = swap2Vec;
                }
            }
        }

        private static List<Vector3> GetRandomPointList(float distanceLimit) {
            List<Vector3> res = new List<Vector3>();

            int nPoints = 250;

            for (int n = 0; n < nPoints; n++) {
                Vector3 randomPoint = UnityEngine.Random.insideUnitSphere * 15.0f;
                randomPoint = Vector3.ProjectOnPlane(randomPoint, Vector3.up);
                res.Add(randomPoint);
            }

            HashSet<int> indexMerged = new HashSet<int>();
            List<Vector3> okPoint = new List<Vector3>();

            for (int i = 0; i < res.Count; i++) {
                if (indexMerged.Contains(i)) {
                    continue;
                }

                Vector3 p = res[i];
                indexMerged.Add(i);

                int nPointsMerged = 1;

                for (int j = 0; j < res.Count; j++) {
                    if (indexMerged.Contains(j) || i == j) {
                        continue;
                    }

                    Vector3 pCheck = res[j];

                    if (Vector3.Distance(p, pCheck) < distanceLimit) {
                        indexMerged.Add(j);
                        nPointsMerged++;
                        p += (pCheck - p) / nPointsMerged;
                        j = 0;
                    }
                }
                okPoint.Add(p);
            }
            return okPoint;
        }

        static int GetRandomIndex(List<Vector3> points) {
            return Random.Next(0, points.Count - 1);
        }
        static bool LineIntersect(List<Vector3> points) {
            int nPoint = points.Count;
            for (int i = 0; i < nPoint; i++) {

                Vector3 checkStart = points[i];
                Vector3 checkEnd = points[(i + 1) % nPoint];

                for (int j = 0; j < nPoint; j++) {
                    if (j == i) {
                        continue;
                    }
                        
                    Vector3 lineStart = points[j];
                    Vector3 lineEnd = points[(j + 1) % nPoint];

                    if (MathUtils.Compute2DIntersection(new LineSegment(checkStart , checkEnd), new LineSegment(lineStart, lineEnd)).exist) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}