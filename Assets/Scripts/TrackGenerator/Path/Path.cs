using System;
using System.Collections.Generic;
using Common;
using UnityEngine;

namespace TrackGenerator.Path{
    
    [Serializable]
    public class Path {
        
        private const float DELTA = 1e-6f;
        
        private readonly List<Vector3> points;
        private float pathLenght;

        private int lutLength;
        private int lutEndIndex;
        public Vector3[] lut;

        public float Length => pathLenght;

        public Path(List<Vector3> points, int lutLength, bool circularPath){
            
            if (circularPath){
                points.Add(points[0]);
            }
            
            this.points = points;
            this.lutLength = lutLength;
            lutEndIndex = lutLength - 1;
            
            CalculatePathLength();
            ConstructLut();
        }


        private void CalculatePathLength() {
            
            Vector3 p1 = points[0];
            for (int i = 1; i < points.Count; i++) {
                Vector3 p2 = points[i];
                pathLenght += Vector3.Distance(p1, p2);
                p1 = p2;
            }
        }
        private void ConstructLut() {

            lut = new Vector3[lutLength];
            Utils.LinSpace tSpace = new Utils.LinSpace(0, pathLenght, lutLength);
            Vector3 lineStart = points[0];
            Vector3 lineEnd = points[1];
            float segmentLenght = Vector3.Distance(lineStart, lineEnd);
            float coveredSegmentDistance = 0.0f;
            
            int lineIndex = 0;

            for (var i = 0; i < tSpace.array.Length; i++) {
                
                float coveredL = tSpace.array[i];
                
                float coveredPlusCurrent = segmentLenght + coveredSegmentDistance;
                float overNextSegment = (coveredL - coveredPlusCurrent);
                
                if (overNextSegment > DELTA) {
                    coveredSegmentDistance += segmentLenght;
                    lineIndex++;
                    lineStart = points[lineIndex];
                    lineEnd = points[lineIndex + 1];
                    segmentLenght = Vector3.Distance(lineStart, lineEnd);
                }

                float onSegmentLenght = coveredL - coveredSegmentDistance;
                float tLerpValue = onSegmentLenght / segmentLenght;

                lut[i] = Vector3.Lerp(lineStart, lineEnd, tLerpValue);
            }
            
            DisplayLutError();
        }

        private void DisplayLutError() {
            
            float lutPathLength = 0.0f;
            Vector3 p1 = lut[0];
            for (int i = 1; i < lut.Length; i++) {
                Vector3 p2 = lut[i];
                lutPathLength += Vector3.Distance(p1, p2);
                p1 = p2;
            }

            float error = Mathf.Abs(lutPathLength - pathLenght) / pathLenght;
            Debug.Log($"The lut table error is {error}");
        }

        public Vector3 GetPoint(float t) {
            
            
            // we should probably use log numbers for better performance
            
            // TODO fix hermit interpolation https://www.cubic.org/docs/hermite.htm
            float lutTValue = t * (lutEndIndex);
            int x1 = Math.Clamp(Mathf.FloorToInt(lutTValue), 0, lutEndIndex);
            int x2 = Math.Clamp(x1 + 1, 0, lutEndIndex);
            
            Vector3 y1 = lut[x1];
            Vector3 y2 = lut[x2];
            
            float lerpValue = lutTValue - x1;
            
            return Vector3.Lerp(y1,  y2, lerpValue);
        }

        public Vector3 GetDirection(float t) {
            float h = DELTA * 2;
            
            Vector3 deltaMinus = GetPoint(t - h);
            Vector3 deltaPlus = GetPoint(t + h);
            
            return ((deltaPlus - deltaMinus) / (2.0f*h)).normalized;
        }

        public float DistanceToTValue(float distance) {
            return distance / pathLenght;
        }
    }
}