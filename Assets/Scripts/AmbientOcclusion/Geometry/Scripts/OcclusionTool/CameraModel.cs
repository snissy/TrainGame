using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AmbientOcclusion.Geometry.Scripts.OcclusionTool
{
    public class CameraModel
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct CameraDefinition
        {
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

        public struct PixelData
        {
            public Ray ray;
            public int pixelHeight;
            public int pixelWidth;
            public float value;

            public void SetValue(float hitLambda)
            {
                value = hitLambda;
            }
        }

        public struct ViewDefinition
        {
            public Vector3 center;
            public Vector3 p00;
            public Vector3 p10;
            public Vector3 p11;
            public Vector3 p01;
        }

        private const float ASPECT_RATIO = 16.0f / 9.0f;

        private readonly Pose cameraPose;

        private readonly int nPixels;
        private int imageHeight;
        private Vector3 pixel00Loc;
        private Vector3 pixelDeltaU;
        private Vector3 pixelDeltaV;
        private Vector3 cameraCenter;

        public int NPixels => ImageWidth * ImageHeight;
        public int ImageWidth => nPixels;
        public int ImageHeight => imageHeight;

        public CameraModel(Pose cameraPose, int nPixels, float focalLength)
        {
            this.cameraPose = cameraPose;
            this.nPixels = nPixels;
            CalculateImgPlane(focalLength);
        }

        private void CalculateImgPlane(float focalLength)
        {
            // Calculate the image height, and ensure that it's at least 1.
            imageHeight = (int)(ImageWidth / ASPECT_RATIO);
            imageHeight = imageHeight < 1 ? 1 : imageHeight;

            // Camera
            float viewportHeight = 2.0f;
            float viewportWidth = viewportHeight * ((float)ImageWidth / imageHeight);
            cameraCenter = cameraPose.position;

            // Calculate the vectors across the horizontal and down the vertical viewport edges.
            Vector3 viewportU = new(viewportWidth, 0, 0);
            Vector3 viewportV = new(0, viewportHeight, 0);

            // Calculate the horizontal and vertical delta vectors from pixel to pixel.
            pixelDeltaU = viewportU / ImageWidth;
            pixelDeltaV = viewportV / ImageHeight;

            // Calculate the location of the upper left pixel.
            Vector3 viewportUpperLeft = cameraCenter + new Vector3(0, 0, focalLength) - viewportU / 2 - viewportV / 2;
            pixel00Loc = viewportUpperLeft + 0.5f * (pixelDeltaU + pixelDeltaV);
        }

        public IEnumerable<PixelData> GetRays()
        {
            for (int j = 0; j < ImageHeight; j++)
            {
                for (int i = 0; i < ImageWidth; i++)
                {
                    Vector3 rayDirection = GetCenterToPixelDirection(i, j);
                    yield return new PixelData
                    {
                        ray = new Ray(cameraCenter, rayDirection),
                        pixelHeight = j,
                        pixelWidth = i
                    };
                }
            }
        }

        private Vector3 GetCenterToPixelDirection(int i, int j)
        {
            Vector3 pixelCenter = pixel00Loc + i * pixelDeltaU + j * pixelDeltaV;
            Vector3 rayDirection = cameraPose.rotation * (pixelCenter - cameraCenter).normalized;
            return rayDirection;
        }

        public ViewDefinition GetViewDefinition()
        {
            return new ViewDefinition
            {
                center = cameraCenter,
                p00 = cameraCenter + GetCenterToPixelDirection(0, 0),
                p10 = cameraCenter + GetCenterToPixelDirection(ImageWidth - 1, 0),
                p11 = cameraCenter + GetCenterToPixelDirection(ImageWidth - 1, ImageHeight - 1),
                p01 = cameraCenter + GetCenterToPixelDirection(0, ImageHeight - 1)
            };
        }

        public CameraDefinition GetCameraDefinition()
        {
            return new CameraDefinition
            {
                cameraCenter = cameraPose.position,
                rotation = Matrix4x4.Rotate(cameraPose.rotation),
                pixel00Loc = pixel00Loc,
                pixelDeltaU = pixelDeltaU,
                pixelDeltaV = pixelDeltaV,
                imageWidth = ImageWidth,
                imageHeight = ImageHeight
            };
        }
    }
}