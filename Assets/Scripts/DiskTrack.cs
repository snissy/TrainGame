using System;
using System.Collections.Generic;
using Common;
using MeshGeneration;
using TrackGenerator;
using TrackGenerator.Path;
using Train;
using UnityEditor;
using UnityEngine;
using MathUtils = Common.MathUtils;
using Random = UnityEngine.Random;

public class DiskTrack : MonoBehaviour
{
    [SerializeField] private bool drawTrack = false;
    [SerializeField] private bool drawDisk = false;
    [SerializeField] private bool drawDiskTrack = false;
    [SerializeField] private bool drawLutTrack = false;
    
    [Space(10)]
    [SerializeField] private Color drawColor = Color.yellow;
    [SerializeField] private Color diskColor = Color.cyan;
    
    [Space(5)]
    [SerializeField] private bool angleCheck = false;
    [SerializeField] private bool randomTrack;
    [SerializeField] private float distanceLimit = 0.25f;
    
    [Space(10)]
    [SerializeField] private List<Transform> trackPoints = new List<Transform>();
    [Space(5)] 
    [SerializeField] private CustomMesh meshGenerator;
    [Space(5)] 
    [SerializeField] private TrainObject trainObject;
    [Space(5)]
    [SerializeField] private float speed;
    
    private List<Vector3> points = new List<Vector3>();
    private List<Vector3> randomPoints = new List<Vector3>();
    private Path path;

    [ContextMenu("Make Random Track")]
    private void MakeTrack() {
        randomPoints.Clear();
        randomPoints.AddRange(GetRandomTrack());
        path = new Path(randomPoints.GetDiskTrack(), 4096, circularPath:true);
        meshGenerator.GenerateMesh(path, trainObject);
    }

    private void Awake() {
        MakeTrack();
    }

    private void Update() {
        
        float t = Time.realtimeSinceStartup * (speed * 1e-3f);
        float animationT = t - Mathf.Floor(t);
        float objectTime = animationT;
        float finalTime = objectTime - Mathf.Floor(objectTime);
        
        trainObject.UpdatePosition(path, finalTime);
    }

    private void OnDrawGizmos() {
        
        if (trackPoints.Count == 0) {
            return;
        }

        if (randomTrack) {
            if (randomPoints.Count == 0) {
                MakeTrack();
            }
            points = randomPoints;
        }
        
        else {
            bool cacheDiff = CacheDiffWithTransform();
            if (cacheDiff) {
                points = TrackTransformToPoints();
            }
        }

        if (drawTrack){
            DrawBaseTrack(); 
        }

        if (drawDisk) {
            DrawDiscs();
        }

        if (drawDiskTrack) {
            DrawDiskTrack();
        }

        if (drawLutTrack) {
            DrawLutTrack();
        }
    }

    private void DrawLutTrack() {

        int nSteps = 200;
        Utils.LinSpace linSpace = new Utils.LinSpace(0, 1f, nSteps);
        
        for (int i = 0; i < nSteps; i++) {
            float t1Value = linSpace.array[i];
            float t2Value = linSpace.array[(i + 1) % nSteps];
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(path.GetPoint(t1Value), path.GetPoint(t2Value));
            Gizmos.DrawSphere(path.GetPoint(t1Value), 0.075f);
        }
    }

    private bool CacheDiffWithTransform() {
        
        if (points.Count != trackPoints.Count) {
            return true;
        }

        for (var i = 0; i < trackPoints.Count; i++) {
            
            Transform transformPoint = trackPoints[i];
            Vector3 cachePoint = points[i];

            if (Vector3.Distance(transformPoint.position, cachePoint) > 1e-5) {
                return true;
            }
        }

        return false;
    }

    private void DrawDiscs() {
        
        Gizmos.color = diskColor;
        Handles.color = diskColor;
        float screenSpaceSize = 7.50f;
        
        List<TriangleDisk> disks = points.GetDiskList();

        foreach (TriangleDisk disk in disks) {
            
            Handles.DrawDottedLine(disk.TriangleVertices[0], disk.TriangleVertices[1],screenSpaceSize);
            Handles.DrawDottedLine(disk.TriangleVertices[1], disk.TriangleVertices[2],screenSpaceSize);
            Handles.DrawDottedLine(disk.TriangleVertices[2], disk.TriangleVertices[0],screenSpaceSize);
            Handles.DrawWireDisc(disk.Center, Vector3.up, disk.Radius);
        }
    }
    
    private void DrawDiskTrack() {
        
        List<Vector3> finalTrack = points.GetDiskTrack();
        Gizmos.color = Color.Lerp(diskColor, Color.white, 0.66f);
        
        Vector3 upOffset = Vector3.up * 1.33f;
        int nPoints = finalTrack.Count;

        for (var i = 0; i < finalTrack.Count; i++) {
            
            var p1 = finalTrack[i] + upOffset;
            var p2 = finalTrack[(i + 1) % nPoints] + upOffset;
            
            Gizmos.DrawLine(p1, p2);
            //Gizmos.DrawSphere(p1, 0.25f);
        }
    }
    
    private void DrawBaseTrack() {
        
        Gizmos.color = drawColor;
        
        int nPoints = points.Count;

        for (var i = 0; i < points.Count; i++) {
            var p1 = points[i];
            var p2 = points[(i + 1) % nPoints];
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawSphere((p1 + p2)*0.5f, 0.15f);
        }
    }
    
    private List<Vector3> TrackTransformToPoints() {

        List<Vector3> res = new List<Vector3>();

        for (var i = 0; i < trackPoints.Count; i++) {
            res.Add(trackPoints[i].position);
        }
        return res;
    }
    
    private List<Vector3> GetRandomTrack() {

        List<Vector3> res = new List<Vector3>();
        
        int nPoints = 500;
        
        for (int n = 0; n < nPoints; n++) {

            Vector3 randomPoint = Random.insideUnitSphere * 15.0f;
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
            
            List<int> mergeIndex = new List<int>();
            int nPointsMerged = 1;

            for (int j = 0; j < res.Count; j++) {

                if (indexMerged.Contains(j) || i == j) {
                    continue;
                }
                
                var pCheck = res[j];
                
                if (Vector3.Distance(p, pCheck) < distanceLimit) {
                    mergeIndex.Add(j);
                    
                    nPointsMerged++;
                    p += (pCheck - p) / nPointsMerged;
                }
            }
            
            okPoint.Add(p);

            foreach (int index in mergeIndex) {
                indexMerged.Add(index);
            }

            indexMerged.Add(i);
        }
        
        Vector3 centerPoint = okPoint.GetCenterPoint();
        okPoint.SortByAngularDifference(centerPoint);
        
        System.Random random = new System.Random();

        int GetRandomIndex() {
            return random.Next(0, okPoint.Count - 1);
        }

        bool LineIntersect() {
            int nPoint = okPoint.Count;
            for (int i = 0; i < nPoint; i++) {

                Vector3 checkStart = okPoint[i];
                Vector3 checkEnd = okPoint[(i + 1) % nPoint];

                for (int j = 0; j < nPoint; j++) {
                    if (j == i) {
                        continue;
                    }
                    
                    Vector3 lineStart = okPoint[j];
                    Vector3 lineEnd = okPoint[(j + 1) % nPoint];

                    if (MathUtils.Compute2DIntersection(checkStart, checkEnd, lineStart, lineEnd).exist) {
                        return true;
                    };
                }
            }

            return false;
        }
        
        for (int i = 0; i < 10000; i++) {

            int swap1 = GetRandomIndex();
            int swap2 = GetRandomIndex();
            
            Vector3 swap1Vec = okPoint[swap1];
            Vector3 swap2Vec = okPoint[swap2];

            okPoint[swap1] = swap2Vec;
            okPoint[swap2] = swap1Vec;

            if (LineIntersect()) {
                okPoint[swap1] = swap1Vec;
                okPoint[swap2] = swap2Vec;
            }
        }
        
        Debug.Log($"Number of points before angle check {okPoint.Count}");

        if (angleCheck) {
            bool noAngleError = false;
        
            while (!noAngleError && okPoint.Count >= 3) {
            
                noAngleError = true;
            
                for (var i = 0; i < okPoint.Count; i++) {
                
                    int nOkPoints = okPoint.Count;
                
                    var p1 = okPoint[i];
                    var p2 = okPoint[(i + 1) % nOkPoints];
                    var p3 = okPoint[(i + 2) % nOkPoints];
            
                    float angleValue = Vector3.Angle(p1 - p2, p3 - p2);

                    float aLim = 40;
                    if (angleValue < aLim || angleValue > 180-aLim) {
                        noAngleError = false;
                        okPoint.RemoveAt((i + 1) % nOkPoints);
                    }
                }
            }
        
            Debug.Log($"Number of points after angle check {okPoint.Count}");
        }
        
        return okPoint;
    }
    
}
