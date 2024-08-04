using UnityEngine;

namespace Matoya.Minigolf.Scripts.OcclusionTool
{
    public partial class CameraModel
    {
        public struct ViewDefinition {
            public Vector3 center;
            public Vector3 p00;
            public Vector3 p10;
            public Vector3 p11;
            public Vector3 p01;
        }
    }
}