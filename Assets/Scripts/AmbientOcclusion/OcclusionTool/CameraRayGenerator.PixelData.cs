using UnityEngine;

namespace Matoya.Minigolf.Scripts.OcclusionTool
{
    public partial class CameraModel
    {
        public struct PixelData {
            public Ray ray;
            public int pixelHeight;
            public int pixelWidth;
            public float value;

            public void SetValue(float hitLambda) {
                value = hitLambda;
            }
        }
    }
}