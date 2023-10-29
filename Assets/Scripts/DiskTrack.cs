using System.Collections.Generic;
using DefaultNamespace;
using MeshGeneration;
using TrackGenerator;
using TrackGenerator.Path;
using Train;
using UnityEditor;
using UnityEngine;
using MathUtils = Common.MathUtils;

public class DiskTrack : MonoBehaviour {
    
    [SerializeField] private bool drawTrack = false;
    [SerializeField] private bool drawDisk = false;
    [SerializeField] private bool drawLutTrack = false;
    
    [Space(10)]
    [SerializeField] private Color drawColor = Color.yellow;
    [SerializeField] private Color diskColor = Color.cyan;
    
    [Space(5)]
    [SerializeField] private bool angleCheck = false;
    [SerializeField] private bool randomTrack;
    [SerializeField] private float distanceLimit = 0.25f;
    [SerializeField] private float angleLimit = 40;
    
    [Space(10)]
    [SerializeField] private List<Transform> trackPoints = new List<Transform>();
    [Space(5)] 
    [SerializeField] private CustomMesh trackMeshGenerator;
    [Space(5)] 
    [SerializeField] private TrainObject trainObject;

    private List<Vector3> points = new List<Vector3>();
    private List<Vector3> randomPoints = new List<Vector3>();
    private Path path;

    [ContextMenu("Make Random Track")]
    private void MakeTrack() {
        
        randomPoints.Clear();
        randomPoints.AddRange(Generator.GetRandomTrack(distanceLimit, angleCheck,angleLimit));
        
        path = new Path(randomPoints.GetDiskTrack(), 8192, circularPath:true);
        trackMeshGenerator.GenerateMesh(path, trainObject);
    }

    private void Awake() {
        MakeTrack();
        trainObject.SetPath(path);
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

        if (drawLutTrack) {
            DrawLutTrack();
        }
    }

    private void DrawLutTrack() {

        int nSteps = 200;
        float[] linSpace = MathUtils.LinSpace(0, 1f, nSteps);
        
        for (int i = 0; i < nSteps; i++) {
            float t1Value = linSpace[i];
            float t2Value = linSpace[(i + 1) % nSteps];
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(path.GetPoint(t1Value), path.GetPoint(t2Value));
            Gizmos.DrawSphere(path.GetPoint(t1Value), 0.075f);
        }
        
        Gizmos.DrawSphere( path.GetPoint(0.0f), 0.25f);
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
        List<TriangleDisk> disks = points.GetDiskList();
        foreach (TriangleDisk disk in disks) {
            Gizmos.DrawSphere(disk.Center, 0.05f);
            DebugDraw.DrawDisk(disk.Center, disk.Radius, diskColor);
            DebugDraw.DrawDottedLine(disk.TriangleVertices[0], disk.TriangleVertices[1], diskColor);
            DebugDraw.DrawDottedLine(disk.TriangleVertices[1], disk.TriangleVertices[2], diskColor);
            DebugDraw.DrawDottedLine(disk.TriangleVertices[2], disk.TriangleVertices[0], diskColor);
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
}
