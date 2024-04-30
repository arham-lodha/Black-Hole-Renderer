struct Material {
    float4 color;
    float4 emissionColor;
    float4 specularColor;
    float emissionStrength;
    float specularProbability;
    float roughness;
};

struct Isect {
    int didHit;
    int hitBlackhole;
    float time;
    float3 intersectionPoint;
    float3 intersectionDirection;
    float3 normal;
    Material material;
};

struct Sphere {
    float3 position;
    float radius;
    Material material;
};

struct Box {
    float3 position;
    float3 corner;
    float4 rotation;
    Material material;
};

float3 qtrans(float4 q, float3 v) {
    return v + 2.0 * cross(cross(v, q.xyz) - q.w*v, q.xyz);
}

// https://iquilezles.org/articles/distfunctions/
float sphereSDF(float3 position, float3 center, float radius) {
    return length(position - center) - radius;
}

float boxSDF(float3 position, float3 center, float3 corner, float4 rotation) {
    float3 p = qtrans(rotation, position - center);
    float3 q = abs(p) - corner;

    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float3 getBoxNormal(float3 position, float3 center, float3 corner, float4 rotation){
    float eps = 0.001f;
    float3 dx = float3(eps, 0.0f, 0.0f);
    float3 dy = float3(0.0f, eps, 0.0f);
    float3 dz = float3(0.0f, 0.0f, eps);

    return normalize(
        float3(
            boxSDF(position + dx, center, corner, rotation) - boxSDF(position - dx, center, corner, rotation),
            boxSDF(position + dy, center, corner, rotation) - boxSDF(position - dy, center, corner, rotation),
            boxSDF(position + dz, center, corner, rotation) - boxSDF(position - dz, center, corner, rotation)
        )
    );
}

float3 getSphereNormal(float3 position, float3 center, float radius) {
    float eps = 0.001f;
    float3 dx = float3(eps, 0.0f, 0.0f);
    float3 dy = float3(0.0f, eps, 0.0f);
    float3 dz = float3(0.0f, 0.0f, eps);

    return normalize(
        float3(
            sphereSDF(position + dx, center, radius) - sphereSDF(position - dx, center, radius),
            sphereSDF(position + dy, center, radius) - sphereSDF(position - dy, center, radius),
            sphereSDF(position + dz, center, radius) - sphereSDF(position - dz, center, radius)
        )
    );
}