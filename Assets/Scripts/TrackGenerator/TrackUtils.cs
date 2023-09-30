using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.TrackGenerator
{
    public static class TrackUtils {

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