using System.Collections.Generic;
using TrackGenerator;
using UnityEngine;

public class DiskTrack : MonoBehaviour
{
    [SerializeField] private bool drawTrack = false;
    [SerializeField] private bool drawDisk = false;
    [SerializeField] private bool drawDiskTrack = false;
    [SerializeField] private Color drawColor = Color.yellow;
    [SerializeField] private Color diskColor = Color.cyan;
    [Space(5)]
    [SerializeField] [Range(0, 1)] private float pFactor = 0.5f; 
    
    [Space(10)]
    [SerializeField] private List<Transform> trackPoints = new List<Transform>();

    private List<Vector3> points = new List<Vector3>();
    
    private void OnDrawGizmos()
    {
        if (trackPoints.Count == 0) {
            return;
        }

        bool cacheDiff = CacheDiffWithTransform();

        if (cacheDiff) {
            points = TrackTransformToPoints();
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
    }

    private bool CacheDiffWithTransform()
    {
        if (points.Count != trackPoints.Count) {
            return true;
        }

        for (var i = 0; i < trackPoints.Count; i++) {
            
            Transform transformPoint = trackPoints[i];
            Vector3 cachePoint = points[i];

            if (Vector3.Distance(transformPoint.position, cachePoint) > 1e-4) {
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
