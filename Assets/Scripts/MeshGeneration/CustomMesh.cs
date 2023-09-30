using TrackGenerator.Path;
using Train;
using UnityEngine;

namespace MeshGeneration {
    
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class CustomMesh : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        
        public void GenerateMesh(Path path, TrainObject trainObject) {
            MeshGeneration generation = new MeshGeneration(path, trainObject.TrainDimensions.x);
            Mesh finalMesh = generation.GenerateMesh();
            meshFilter.mesh = finalMesh;
        }
    }
}