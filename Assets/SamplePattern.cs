using System.Collections.Generic;
using Common;
using UnityEngine;

public class SamplePattern : MonoBehaviour {

    private const float OneOverPhi = 1.0f/1.61803398875f;

    [Range(0.0001f, 0.2f)][SerializeField] private float pointRadius;
    [Space(3)]
    [SerializeField] private Color drawColor2d;
    [SerializeField] private Color drawColorSphere;
    [SerializeField] private Color drawColorSamplingPatternSphere;
    [Space(5)]
    [Range(0.001f,0.05f)] [SerializeField] private float stepSize = 0.01f;
    private LinSpace StepRange => new LinSpace(0, 1.0f, Mathf.RoundToInt(1 / stepSize));
    private readonly List<Vector2> samplingPoints = new List<Vector2>();
    private float GetSamplePoint(float time, int nPoints) {
        float t = nPoints * time * OneOverPhi;
        return t - Mathf.Floor(t);
    }

    private List<Vector2> GetSamplePoints() {
        samplingPoints.Clear();
        int nPoint = StepRange.array.Length;
        for (var i = 0; i < nPoint; i++) {
            float step = StepRange.array[i];
            Vector2 samplePoint = new Vector2(step, GetSamplePoint(step, nPoint));
            samplingPoints.Add(samplePoint);
        }
        return samplingPoints;
    }

    private Vector3 SphericalToCartesian(float phi, float tau, float r) {
        float x = r * Mathf.Sin(tau) * Mathf.Cos(phi);
        float y = r * Mathf.Sin(tau) * Mathf.Sin(phi);
        float z = r * Mathf.Cos(tau);
        return new Vector3(x, z, y);
    }
    
    private void OnDrawGizmos() {
        Gizmos.color = drawColor2d;
        List<Vector2> drawPoints = GetSamplePoints();
        foreach (Vector2 drawPoint in drawPoints) {
            Gizmos.DrawWireSphere(drawPoint.SwizzleX0Y(), pointRadius);
        }
        
        Gizmos.color = drawColorSphere;

        float[] tauSpace = (new LinSpace(0, Mathf.PI * 0.5f, 25).array);
        float[] phiSpace = (new LinSpace(0, Mathf.PI * 2f, 25).array);

        foreach (float tauStep in tauSpace) {
            foreach (float phiStep in phiSpace) {
                Gizmos.DrawWireSphere(SphericalToCartesian(phiStep, tauStep, 1.0f), pointRadius);
            }
        }

        Gizmos.color = drawColorSamplingPatternSphere;
        foreach (Vector2 drawPoint in drawPoints) {
            float phiStep = MathFunctions.ReMap(drawPoint.x, 0, 1, 0, Mathf.PI * 2.0f); 
            float tauStep = MathFunctions.ReMap(drawPoint.y, 0, 1, 0, Mathf.PI * 0.5f); 
            Gizmos.DrawWireSphere(SphericalToCartesian(phiStep, tauStep, 1.0f), pointRadius);
            Gizmos.DrawRay(Vector3.zero,SphericalToCartesian(phiStep, tauStep, 1.0f) * 2.0f);
        }
    }
}


