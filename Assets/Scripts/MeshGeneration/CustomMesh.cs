using System;
using TrackGenerator.Path;
using Train;
using UnityEngine;

namespace MeshGeneration {
    
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public class CustomMesh : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private PolygonCollider2D trackShape;
        
        public void GenerateMesh(Path path, TrainObject trainObject) {
            
            TrackMeshGeneration generation = new TrackMeshGeneration(path, trackShape, trainObject.TrainDimensions.x, trainObject.WheelWidth);
            Mesh finalMesh = generation.GenerateMesh();
            meshFilter.sharedMesh = finalMesh;
            
            // TODO WHAT A HACK FIX!
            
            Bounds shapeBounds = trackShape.bounds;
            float boundWidth = shapeBounds.extents.x * 2.0f;
            float scaleFactor = trainObject.WheelWidth / boundWidth;
            
            transform.position = -Vector3.up*shapeBounds.max.y*scaleFactor;
        }
        
    }
}