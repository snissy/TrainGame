using Matoya.Common.Geometry;
using Matoya.Minigolf.Scripts.OcclusionTool;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.Linq;
using System.Threading;
using AmbientOcclusion.OcclusionTool;
using Matoya.Common;
using UnityEditor;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace Matoya {
    public sealed class TestBVH : MonoBehaviour {

    [SerializeField] private Transform rayTransform;
    [SerializeField] private List<MeshRenderer> meshRenderers;
    [SerializeField, Range(2, 13)] private int nPowerRays = 2;
    [SerializeField, Range(0.001f, 2f)] private float focalLenght;

    [SerializeField] private bool drawBounds;
    [SerializeField] private bool runSceneTest;
    [SerializeField] private bool dontDraw;

    [FormerlySerializedAs("runMeshHardTest")]
    [Header("Extra Test")]
    [SerializeField] private bool runMestTest;
    [SerializeField] private MeshRenderer meshToTest;

    [SerializeField] private int imageWidth = 0;
    [SerializeField] private int nPixels = 0;
    [SerializeField] private int NTest = 0;

    private BVHScene bvhScene;
    private BVHMesh bvhMesh;

    [ContextMenu("Get All Mesh Renders")]

    public void GetAllRenders() {
        meshRenderers = FindObjectsOfType<MeshRenderer>().ToList();
    }

    [ContextMenu("Bounding volume hierarchy")]
    public void MakeBoundingVolume() {
        bvhScene = new BVHScene(meshRenderers);
        if(meshToTest) {
            bvhMesh = new BVHMesh(meshToTest);
        }
    }

    private void OnDrawGizmos() {

        imageWidth = (int) Mathf.Pow(2, nPowerRays);
        nPixels = Mathf.FloorToInt(imageWidth * (imageWidth*1.7777f)) ;

        if(!runSceneTest) {
            return;
        }

        if(bvhScene == null) {
            Debug.LogWarning("No Volume Created");
            return;
        }
        if(rayTransform == null) {
            Debug.LogWarning("Link Ray");
            return;
        }

        IEnumerable<CameraModel.PixelData> rayGenerator = new CameraModel(rayTransform.Pose(), imageWidth, focalLenght).GetRays();

        if(meshToTest && meshToTest) {
            MakeMeshTest(rayGenerator);
            return;
        }
        MakeSceneTest(rayGenerator);
    }

    private void MakeMeshTest(IEnumerable<CameraModel.PixelData> rayGenerator) {
        foreach(CameraModel.PixelData pixel in rayGenerator) {
            Ray testRay = pixel.ray;
            if(bvhMesh.IntersectRay(testRay, out float hitLambda)) {
                DrawPoint(testRay.GetPoint(hitLambda), Color.yellow);
            }
            else {
                DrawLine(testRay.origin, testRay.GetPoint(250f), Color.red);
            }
        }
        if(drawBounds) {
            DrawBvh(bvhMesh);
        }
    }

    private void MakeSceneTest(IEnumerable<CameraModel.PixelData> rayGenerator) {
        int nTestSum = 0;
        foreach(CameraModel.PixelData pixel in rayGenerator) {
            Ray testRay = pixel.ray;
            if(bvhScene.IntersectRay(testRay, out float hitLambda, out MeshRenderer renderer, out int nTestRay)) {
                DrawPoint(testRay.GetPoint(hitLambda), Color.yellow);
            }
            else {
                DrawLine(testRay.origin, testRay.GetPoint(250f), Color.red);
            }
            nTestSum += nTestRay;
        }
        NTest = nTestSum;
        if(drawBounds) {
            DrawBvh(bvhScene);
        }
    }

    private void DrawBvh(BVHScene bvh) {
        foreach(Bounds bound in bvh.GetBounds()) {
            DrawBound(bound, Color.white);
        }
    }
    
    private void DrawBvh(BVHMesh bvh) {
        foreach(Bounds bound in bvh.GetBounds()) {
            DrawBound(bound, Color.white);
        }
    }

    private void DrawPoint(Vector3 point, Color c, float size = 0.075f) {
        DrawLine(point, point + Vector3.up*size, c);
        DrawLine(point, point + Vector3.right*size, c);
        DrawLine(point, point + Vector3.forward*size, c);
    }

    private void DrawBound(Bounds bounds , Color c) {
        Vector3[] boundsVertices = BVHUtils.CalculateWorldBoxCorners(bounds);
        for(int i = 0; i < 4; i++) {

            Vector3 c1 = boundsVertices[i];
            Vector3 c2 = boundsVertices[(i + 1) % 4];
            DrawLine(c1, c2, c);

            Vector3 c3 = boundsVertices[4 + i];
            Vector3 c4 = boundsVertices[4 + (i + 1) % 4];

            DrawLine(c3, c4, c);
            DrawLine(c1, c3, c);
        }
    }

    private void DrawLine(Vector3 p0, Vector3 p1, Color c) {
        if(dontDraw) {
            return;
        }
        Gizmos.color = c;
        Gizmos.DrawLine(p0, p1);
    }

    [ContextMenu("Make Image Safe")]
    public void MakeImageSafe() {

        try {
            MakeImage();
        }
        catch (Exception)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogWarning("Exception MAN!");
        }
    }
    public void MakeImage() {

        if(bvhScene == null) {
            Debug.LogWarning("Make a bvh Scene dummy");
            return;
        }

        CameraModel cameraModel = new(rayTransform.Pose(), imageWidth, focalLenght);
        // Create a new texture
        Texture2D texture = new(cameraModel.ImageWidth, cameraModel.ImageHeight);

        // Set background color
        Color[] backgroundColorArray = new Color[cameraModel.ImageWidth * cameraModel.ImageHeight];
        for (int i = 0; i < backgroundColorArray.Length; i++) {
            backgroundColorArray[i] = Color.black;
        }
        texture.SetPixels(backgroundColorArray);

        // Set points with assigned values

        Stopwatch stopwatch = new();
        // Start the stopwatch
        stopwatch.Start();
        EditorUtility.DisplayProgressBar("Making IMG", "Sending Ray", 0.0f);
        // Get the elapsed time
        ConcurrentQueue<CameraModel.PixelData> resultsQue = new();
        // Create a CountdownEvent to wait for all tasks to complete
        CountdownEvent countdown = new(cameraModel.NPixels);
        foreach (CameraModel.PixelData item in cameraModel.GetRays()) {
            ThreadPool.QueueUserWorkItem(ProcessStruct, (item, bvhScene, resultsQue, countdown));
        }

        // Wait for all tasks to complete
        countdown.Wait();

        foreach(CameraModel.PixelData p in resultsQue) {
            float lightness = Mathf.Pow(1 - Mathf.Clamp01(p.value / 90f), 2);
            texture.SetPoint(new Vector2Int(p.pixelWidth, p.pixelHeight), Color.HSVToRGB(1-lightness, 1, lightness)); // Example point at (100, 100) with value 10
        }

        stopwatch.Stop();
        // Get the elapsed time
        TimeSpan elapsedTime = stopwatch.Elapsed;
        Debug.Log($"Time taken POOL THREAD: {elapsedTime.TotalMilliseconds} milliseconds");
        // Apply changes
        texture.Apply();

        // Save the image
        texture.SaveImage(@"C:\Users\Nils\Desktop\", "GeneratedImg");
        EditorUtility.ClearProgressBar();
    }

    static void ProcessStruct(object state) {
        // Retrieve the struct and data structure from the state object
        (CameraModel.PixelData pixel, BVHScene scene, ConcurrentQueue<CameraModel.PixelData> resultsQue, CountdownEvent countdownEvent) = ((CameraModel.PixelData, BVHScene,ConcurrentQueue<CameraModel.PixelData>, CountdownEvent)) state;
        // Access the data structure Y based on the ID from the struct
        scene.IntersectRay(pixel.ray, out float hitLambda, out MeshRenderer renderer, out int nTest);
        pixel.SetValue(hitLambda);
        // Simulate processing by printing the data
        resultsQue.Enqueue(pixel);
        countdownEvent.Signal();
    }
}
}
