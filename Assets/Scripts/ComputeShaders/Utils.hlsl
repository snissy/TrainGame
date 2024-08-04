#define FLT_MAX 1e+32
#define DELTA 1e-16

struct Ray {
    float3 Origin;
    float3 Direction;
};

struct Bounds{
    float3 Center;
    float3 Extents;
};

struct Triangle {
    
    float2 uv0; 
    float2 uv1;
    float2 uv2;

    float uvArea;

    float2 uvMin;
    float2 uvMax;

    float3 v0;
    float3 v1;
    float3 v2;

    float3 faceNormal;
    float3 faceTangent;
    float3 faceBinormal;

    float4 faceRotation;
        
    Bounds bounds;
};

struct BVH_Node {

    uint LeftNodeIndex;
    uint RightNodeIndex;
    
    Bounds Bounds;
    
    uint leafIndexStart;
    uint leafIndexEnd;
};

float TriangleRayIntersectionTest(Ray testRay, Triangle tri) {
    // https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
    
    float3 edge1 = tri.v1 - tri.v0;
    float3 edge2 = tri.v2 - tri.v0;

    // TODO NILS THIS COULD BE Cached
    
    float3 rayCrossEdge2 = cross(testRay.Direction, edge2);

    float det = dot(edge1, rayCrossEdge2);

    if (det > -DELTA && det < DELTA) {
        return -1.0f; // This ray is parallel to this triangle.
    }

    float invDet = 1.0f / det;
    float3 s = testRay.Origin - tri.v0;
    float u = invDet * dot(s, rayCrossEdge2);

    if (u < 0.0f || u > 1.0f) {
        return -1.0f;
    }

    float3 sCrossEdge1 = cross(s, edge1);
    float v = invDet * dot(testRay.Direction, sCrossEdge1);

    if (v < 0.0f || u + v > 1.0f) {
        return -1.0f;
    }

    // At this stage we can compute t to find out where the intersection point is on the line.
    float hitLambda = invDet * dot(edge2, sCrossEdge1);

    if (hitLambda > DELTA) {// ray intersection
        return hitLambda;
    }
    // This means that there is a line intersection but not a ray intersection.
    return -1.0f;
}

float RayIntersectsBounds(Ray ray, Bounds bounds) {
    // Precompute the inverse of the ray direction
    float3 invDir = 1.0 / ray.Direction;
    
    // Calculate bounds min and max
    float3 boundsMin = bounds.Center - bounds.Extents;
    float3 boundsMax = bounds.Center + bounds.Extents;

    // Calculate intersection distances for each axis
    float3 t1 = (boundsMin - ray.Origin) * invDir;
    float3 t2 = (boundsMax - ray.Origin) * invDir;

    // Calculate tmin and tmax for each axis
    float3 tmin = min(t1, t2);
    float3 tmax = max(t1, t2);

    // Find the largest tmin and the smallest tmax
    float largest_tmin = max(max(tmin.x, tmin.y), tmin.z);
    float smallest_tmax = min(min(tmax.x, tmax.y), tmax.z);

    // Check for intersection
    bool intersect = smallest_tmax >= largest_tmin;

    // Return -1 if no intersection, else return largest_tmin
    return intersect ? largest_tmin : -1.0;
}

