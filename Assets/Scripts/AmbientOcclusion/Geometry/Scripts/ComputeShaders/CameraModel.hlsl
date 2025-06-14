#include "Utils.hlsl"

struct CameraDefinition {
    float3 cameraCenter;
    float4x4 rotation;
    
    float3 pixel00Loc;
    float3 pixelDeltaU;
    float3 pixelDeltaV;

    uint imageWidth;
    uint imageHeight;
};

float3 RotateVector(float3 vec, float4x4 rotationMatrix){
    // Extend the vector to a float4 with w = 1.0 to allow multiplication with the 4x4 matrix
    float4 extendedVector = float4(vec, 1.0);

    // Perform the matrix multiplication
    float4 rotatedVector = mul(rotationMatrix, extendedVector);

    // Return the rotated vector as a float3 (ignore the w component)
    return rotatedVector.xyz;
}

float3 GetCenterToPixelDirection(CameraDefinition camera_definition, uint i, uint j) {
    float3 pixelCenter = camera_definition.pixel00Loc + i * camera_definition.pixelDeltaU + j * camera_definition.pixelDeltaV;
    float3 rayDirection = RotateVector(normalize(pixelCenter - camera_definition.cameraCenter), camera_definition.rotation);
    return rayDirection;
}

Ray GetPixelRay(CameraDefinition camera_definition, uint i, uint j) {
    Ray ray;
    ray.Origin = camera_definition.cameraCenter;
    ray.Direction = GetCenterToPixelDirection(camera_definition, i, j);
    return ray;
}

int MapIndexToHeight(CameraDefinition camera_definition, uint index) {
    return index / camera_definition.imageWidth;
}

int MapIndexToWidth(CameraDefinition camera_definition, uint index) {
    return index % camera_definition.imageWidth;
}

float GammaFunction(float x, float p) {
    float fx = pow(x, p);
    float fOneMinusX = pow(1.0 - x, p);
    return fx / (fx + fOneMinusX);
}
