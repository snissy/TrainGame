using System;
using System.Collections.Generic;
using UnityEngine;

public class RMS : MonoBehaviour
{
    
    private Matrix4x4 cBezierMatrix = new Matrix4x4()
    {
        m00 = -1,
        m01 = 3,
        m02 = -3,
        m03 = 1,

        m10 = 3,
        m11 = -6,
        m12 = 3,
        m13 = 0,

        m20 = -3,
        m21 = 3,
        m22 = 0,
        m23 = 0,

        m30 = 1,
        m31 = 0,
        m32 = 0,
        m33 = 0
    };
    
    Matrix4x4 pointsMatrix = new Matrix4x4();
    private Matrix4x4 tMatrix4X4 = new Matrix4x4();
    
    [Range(0, 2f)] 
    public float speed = 1.0f;
    [Range(0.002f, 0.4f)]
    public float deltaDist = 0.25f;
    private float frameInt = 0;

    private List<(Vector3 pos, float t)> bezierPositions;

    private Transform[] CubesArray = new Transform[10];
    private float currentTime = 0.0f;
    private void Awake()
    {
        if (CubesArray[0] != null)
        {
            for (int i = 0; i < CubesArray.Length; i++)
            {
                DestroyImmediate(CubesArray[i].gameObject);
            }
        }
        
        int nCubes = 10;
        for (int i = 0; i < nCubes; i++)
        {
            var cubeI = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            cubeI.position = Vector3.zero;
            cubeI.rotation = Quaternion.identity;
            cubeI.localScale = Vector3.one*0.75f;
            CubesArray[i] = cubeI;
        }
    }

    private void OnDrawGizmos()
    {
        
        List<Vector3> points = new List<Vector3>();


        foreach (Transform childTransform in this.transform)
        {
            points.Add(childTransform.position);
        }

        int nPoints = points.Count;

        for (int i = 0; i < nPoints - 1; i++)
        {
            Gizmos.DrawLine(points[i], points[(i + 1)]);
        }
        Gizmos.DrawWireCube(points[^1], Vector3.one * 0.8f);
        
        for (int i = 0; i < nPoints; i++)
        {
            pointsMatrix.SetRow(i, points[i]);
        }
        Matrix4x4 interpolationMatrix = this.cBezierMatrix * pointsMatrix;
        var newBezierPositions = new List<(Vector3 p, float t)>();
        Matrix4x4 pointMatrix;
        Vector3 pos;
        
        float t = 0;
        while (t < 1.0f)
        {
            tMatrix4X4.SetRow(0, new Vector4(Mathf.Pow(t, 3), Mathf.Pow(t, 2), t, 1));
            pointMatrix = tMatrix4X4*interpolationMatrix;
            pos = new Vector3(pointMatrix.m00, pointMatrix.m01, pointMatrix.m02);
            newBezierPositions.Add((pos, t));
            
            tMatrix4X4.SetRow(0, new Vector4(3*Mathf.Pow(t, 2), 2*t, 1, 0));
            pointMatrix = tMatrix4X4*interpolationMatrix;
            Vector3 tangent = new Vector3(pointMatrix.m00, pointMatrix.m01, pointMatrix.m02);
            
            t = t + deltaDist/tangent.magnitude;

        }
        
        tMatrix4X4.SetRow(0, new Vector4(1, 1, 1, 1));
        pointMatrix = tMatrix4X4*interpolationMatrix;
        pos = new Vector3(pointMatrix.m00, pointMatrix.m01, pointMatrix.m02);
        newBezierPositions.Add((pos, 1));
        
        bezierPositions = newBezierPositions;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < newBezierPositions.Count-1; i++)
        {
            Vector3 p1 = newBezierPositions[i].p;
            Vector3 p2 = newBezierPositions[i+1].p;
            Gizmos.DrawLine(p1, p2);
            
        }
    }

    private void Update()
    {
        if (CubesArray[0] != null)
        {
            int nPoints = bezierPositions.Count;
            float deltaCube = nPoints / 10.0f;
            currentTime += 100f*(Mathf.Exp(speed) - 1.0f)*Time.deltaTime;
            int nCubes = 10;
            
            for (int i = 0; i < nCubes; i++)
            {
                var cubeI = CubesArray[i];

                float floatTime = currentTime + i * deltaCube;
                
                int start = Mathf.FloorToInt(floatTime);

                int end =  Mathf.CeilToInt(floatTime);
                
                float lerpT = floatTime - (float) start;
                
                cubeI.position =  Vector3.Lerp(bezierPositions[start % nPoints].pos, bezierPositions[end % nPoints].pos, lerpT);
                
                if (end % nPoints == 0)
                {
                    cubeI.position = bezierPositions[0].pos;
                }
            }
        }
    }
}
