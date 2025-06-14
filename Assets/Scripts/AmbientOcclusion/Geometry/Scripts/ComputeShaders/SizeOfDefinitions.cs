using AmbientOcclusion.Geometry.Scripts.OcclusionTool;

namespace AmbientOcclusion.Geometry.Scripts.ComputeShaders
{
    public static class SizeOfDefinitions
    {
        public const int FLOAT = sizeof(float);
        public const int INT = sizeof(int);
        public const int VECTOR2_SIZE = FLOAT * 2;
        public const int VECTOR3_SIZE = FLOAT * 3;
        public const int VECTOR4_SIZE = FLOAT * 4;
        public const int MATRIX4_SIZE = FLOAT * 4 * 4;
        public const int RAY_SIZE = VECTOR3_SIZE * 2;
        public const int BOUNDS_SIZE = VECTOR3_SIZE * 2;
        public const int QUATERNION = VECTOR4_SIZE;

        public const int TRIANGLE_SIZE = Triangle.NVector2 * VECTOR2_SIZE +
                                         Triangle.NVector3 * VECTOR3_SIZE +
                                         Triangle.NFLoats * FLOAT +
                                         Triangle.NBounds * BOUNDS_SIZE +
                                         Triangle.NQuaternion * QUATERNION;

        public const int BVH_ARRAY_NODE = BVHArrayNode.NInts * INT +
                                          BVHArrayNode.NBounds * BOUNDS_SIZE;

        public const int CAMERA_DEFINITION_SIZE = CameraModel.CameraDefinition.N_VECTORS3 * VECTOR3_SIZE +
                                                  CameraModel.CameraDefinition.N_INTS * INT +
                                                  CameraModel.CameraDefinition.N_MATRIX4_4 * MATRIX4_SIZE;
    }
}