using System.Numerics;
using Common;
using DefaultNamespace;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Path = TrackGenerator.Path.Path;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace Train {
    
    [RequireComponent(typeof(BoxCollider))]
    public class TrainObject : MonoBehaviour {
        
        [SerializeField] private BoxCollider box;
        public Vector3 TrainDimensions => box.GetBoxDimensionSize();
        public void UpdatePosition(Path path, float t) {

            Vector3 trainDim = TrainDimensions;
            
            float deltaT = path.DistanceToTValue(trainDim.z)*0.5f;

            Vector3 backPos = path.GetPoint(t - deltaT);
            Vector3 forwardPos = path.GetPoint(t + deltaT);
            Vector3 midPoint = (backPos + forwardPos) * 0.5f;
            
            transform.position = midPoint + Vector3.up * (0.5f*trainDim.y);
            transform.rotation = Quaternion.LookRotation((forwardPos - backPos).normalized, Vector3.up);;
        }
        
        private void GetBackForwardPoints(out Vector3 back, out Vector3 forward) {
        
            float halfLenght = TrainDimensions.z * 0.5f;
            Vector3 mid = transform.position;
            Vector3 trainForward = transform.forward;
        
            back = mid - trainForward * halfLenght;
            forward = mid + trainForward * halfLenght;
        }
        
        private void GetLeftRightPoints(out Vector3 left, out Vector3 right) {
        
            float halfLenght = TrainDimensions.x * 0.5f;
            Vector3 mid = transform.position;
            Vector3 trainRight = transform.right;
        
            left = mid - trainRight * halfLenght;
            right = mid + trainRight * halfLenght;
        }
        
        private void OnDrawGizmosSelected() {
            
            GetBackForwardPoints(out Vector3 back, out Vector3 forward);
            GetLeftRightPoints(out Vector3 left, out Vector3 right);
            
            Color c = Color.HSVToRGB(0.90f, 1, 1);
            DebugDraw.DrawArrow(back, forward, c);
            DebugDraw.DrawArrow(left, right, c);
        }
    }
}
