using System.Runtime.InteropServices;
using UnityEngine;

namespace Matoya.Minigolf.Scripts.OcclusionTool
{
    public partial class CameraModel
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct CameraDefinition {
            public const int N_VECTORS3 = 4;
            public const int N_INTS = 2;
            public const int N_MATRIX4_4 = 1;
            
            public Vector3 cameraCenter;
            public Matrix4x4 rotation;
    
            public Vector3 pixel00Loc;
            public Vector3 pixelDeltaU;
            public Vector3 pixelDeltaV;

            public int imageWidth;
            public int imageHeight;
        }
    }
}