using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.TrackGenerator
{
    public class TrackGenerator : MonoBehaviour
    {
        // public int nPoints = 16;

        [Range(0, 4f)] public float diskRadiusLimit = 0.001f;

        [Range(0, 16)]
        public int nIterations = 2;

        [Range(0, 0.4999f)] 
        public float sFactor = 0.25f;

        [Range(0, 0.33f)]
        public float epsilon = 0.5f;
        
        public Color DrawColor = Color.yellow;
        
        public bool onFirstCall = true;
        public bool DrawCircles = false;

        public List<Transform> controlPoints = new List<Transform>();

        private List<Vector3> trackPoints = new List<Vector3>();

        private List<Vector3> circlePoints;
        

        private void OnDrawGizmos()
        {
            if (onFirstCall)
            {
                GeneratePoints();
                onFirstCall = false;
            }

            int nPoints = trackPoints.Count;

            if (nPoints == 0)
            {
                return;
            }

            Gizmos.color = DrawColor;
            
            for (int i = 0; i < nPoints; i++)
            {
                Gizmos.DrawSphere(trackPoints[i], 0.055f);
                Gizmos.DrawLine(trackPoints[i], trackPoints[(i+1) % nPoints]);
            }
            
            Gizmos.color = Color.green;
            int nControlPoints = controlPoints.Count;
            
            for (var index = 0; index < nControlPoints; index++)
            {
                Gizmos.DrawSphere( controlPoints[index].position, 0.075f);
                Gizmos.DrawLine( controlPoints[index].position, controlPoints[(index+1) % nControlPoints].position);
            }
            
            circlePoints = TrackUtils.CircleCorner(trackPoints);
            circlePoints = TrackUtils.DouglasPeucker(circlePoints, epsilon);
            
            nPoints = circlePoints.Count;
            
            float totalLength = 0;

            for (int i = 0; i < nPoints; i++)
            {
                totalLength += Vector3.Distance(circlePoints[i], circlePoints[(i + 1) % nPoints]);
            }

            int nDesiredPoints = 100;
            float deltaLenght = totalLength / nDesiredPoints;
            float t = 1e-6f;
            
            List<Vector3> finalPoints = new List<Vector3>();
            
            while (t < nPoints)
            {
                int start = Mathf.FloorToInt(t);
                int end =  Mathf.CeilToInt(t);

                float lerpT = t - start;

                Vector3 startP = circlePoints[start % nPoints];
                Vector3 endP = circlePoints[end % nPoints];
                
                finalPoints.Add(Vector3.Lerp(startP, endP, lerpT));

                float distance = Vector3.Distance(startP, endP);
                float timeStep = deltaLenght / distance;
                
                t += timeStep;
            }

            circlePoints = finalPoints;

            Gizmos.color = Color.yellow;
            
            if (circlePoints.Count > 0)
            {
                int nCpoints = circlePoints.Count;
                for (var index = 0; index < nCpoints; index++)
                {
                    Gizmos.DrawSphere(circlePoints[index], 0.075f);
                    Gizmos.DrawLine( circlePoints[index], circlePoints[(index+1) % nCpoints]);
                }
            }

            if (TrackUtils.DiskPoints.Count > 0 && DrawCircles)
            {
                
                int nCpoints = TrackUtils.DiskPoints.Count;
                for (var index = 0; index < nCpoints; index++)
                {
                    TrackUtils.Disk d = TrackUtils.DiskPoints[index];
                    
                 
                    Gizmos.color = Color.Lerp(Color.white, Color.blue, (index / (nCpoints - 1.0f)));
                    Gizmos.DrawLine(d.v1, d.v2);
                    Gizmos.DrawLine(d.v2, d.v3);
                    Gizmos.DrawLine(d.v3, d.v1);
                }
            }
        }

        public List<Vector3> GeneratePoints() {
            
            trackPoints.Clear();
            
            int nControlPoints = controlPoints.Count;
            
            for (var index = 0; index < nControlPoints; index++)
            {
                trackPoints.Add(controlPoints[index].position);
            }
            
            circlePoints = TrackUtils.CircleCorner(trackPoints);
            circlePoints = TrackUtils.DouglasPeucker(circlePoints, epsilon);

            
            // mature code above ^^ 
            
            int nPoints = circlePoints.Count;
            
            float totalLength = 0;

            for (int i = 0; i < nPoints; i++)
            {
                totalLength += Vector3.Distance(circlePoints[i], circlePoints[(i + 1) % nPoints]);
            }

            int nDesiredPoints = 100;
            float deltaLenght = totalLength / nDesiredPoints;
            float t = 1e-6f;
            
            List<Vector3> finalPoints = new List<Vector3>();
            
            while (t < nPoints)
            {
                int start = Mathf.FloorToInt(t);
                int end =  Mathf.CeilToInt(t);

                float lerpT = t - start;

                Vector3 startP = circlePoints[start % nPoints];
                Vector3 endP = circlePoints[end % nPoints];
                
                finalPoints.Add(Vector3.Lerp(startP, endP, lerpT));

                float distance = Vector3.Distance(startP, endP);
                float timeStep = distance / deltaLenght;
                
                t += timeStep;
            }

            circlePoints = finalPoints;
            
            return finalPoints;
        }

        

    }
}