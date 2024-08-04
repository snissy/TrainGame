using UnityEngine;

namespace Matoya.Common {
    public static class PoseExtensions {
        public static Pose Pose(this Transform transform) {
            return new Pose(transform.position, transform.rotation);
        }

        public static Pose LocalPose(this Transform transform) {
            return new Pose(transform.localPosition, transform.localRotation);
        }

        public static Pose Inverse(this Pose pose) {
            Quaternion invRot = Quaternion.Inverse(pose.rotation);
            return new Pose(invRot * -pose.position, invRot);
        }

        public static Pose GetTransformedBy(this Pose pose,  Matrix4x4 matrix) {
            return new Pose(matrix.MultiplyPoint(pose.position), matrix.rotation * pose.rotation);
        }

        public static Pose LerpSlerp(Pose a, Pose b, float t) {
            return new Pose(
                Vector3.Lerp(a.position, b.position, t),
                Quaternion.Slerp(a.rotation, b.rotation, t)
            );
        }
    }
}
