// PCG (permuted congruential generator). Credits: https://www.youtube.com/watch?v=Qz0KTGYJtUk, www.pcg-random.org and www.shadertoy.com/view/XlGcRh
float NextRandom(inout uint state)
{
    state = state * 747796405 + 2891336453;
    uint result = ((state >> ((state >> 28) + 4)) ^ state) * 277803737;
    result = (result >> 22) ^ result;
    return result;
}

float RandomValue(inout uint state){
    return NextRandom(state) / 4294967295.0 * 2.0 - 1.0; // 2^32 - 1;
}

float2 RandomValueInCircle(inout uint state) 
{
    while(true){
        float2 pt = float2(RandomValue(state), RandomValue(state));
        if(length(pt) <= 1) {
            return pt;
        }
    }

    return float2(0.0, 0.0);
}

float random (in float3 pt, in float period, in float seed) {
    pt.y = pt.y - period * floor(pt.y / period);

    return frac(sin( (seed + dot(pt.xyz, float3(12.9898,78.233,23.5405))))*43758.5453123);
}

float3 unit_vec(in float3 xyz, in float period, in float seed) {
    float theta = 6.28318530718*random(xyz, period, seed);
    float phi = 6.28318530718*random(xyz, period, seed * 23.9847);
    return float3(cos(theta), sin(theta), cos(phi));
}

float smoothmix(float a0, float a1, float w) {
    return (a1 - a0) * (3.0 - w * 2.0) * w * w + a0;
}

float dotGradDiff(in float ix, in float iy, in float it, in float x, in float y, in float t, in float period, in float seed) {
    float3 grad = unit_vec(float3(ix, iy, it), period, seed);
    float3 d = float3(x - ix, y - iy, t - it);

    return dot(grad, d);
}

float perlin(in float x, in float y, in float t, in float period, in float seed) {
    float x0 = floor(x);
    float x1 = x0 + 1.0;
    float y0 = floor(y);
    float y1 = y0 + 1.0;
    float t0 = floor(t);
    float t1 = t0 + 1.0;
    float dx = x - x0;
    float dy = y - y0;
    float dt = t - t0;

    // x, y, t0 interpolation

    float a = dotGradDiff(x0, y0, t0, x, y, t, period, seed);
    float b = dotGradDiff(x1, y0, t0, x, y, t, period, seed);
    float c = dotGradDiff(x0, y1, t0, x, y, t, period, seed);
    float d = dotGradDiff(x1, y1, t0, x, y, t, period, seed);

    float ab = smoothmix(a, b, dx);
    float cd = smoothmix(c, d, dx);
    float abcd = smoothmix(ab, cd, dy);

    // x, y, t1 interpolation

    float p = dotGradDiff(x0, y0, t1, x, y, t, period, seed);
    float q = dotGradDiff(x1, y0, t1, x, y, t, period, seed);
    float r = dotGradDiff(x0, y1, t1, x, y, t, period, seed);
    float s = dotGradDiff(x1, y1, t1, x, y, t, period, seed);

    float pq = smoothmix(p, q, dx);
    float rs = smoothmix(r, s, dx);
    float pqrs = smoothmix(ab, cd, dy);

    return smoothmix(abcd, pqrs, dt) * 0.5 + 0.5;
}

// THANK YOU OUR LORD AND SAVIOR INIGO QUILEZ
// https://iquilezles.org/articles/fbm/
float FBM(in float3 vec, in float H, in float seed, in float startingFrequency, in int numOctaves, in float currLength){
    float G = exp2(-H);
    float f = startingFrequency;
    float a = 1.0;
    float sumA = 0.0;
    float t = 0.0;
    float x = vec.x;
    float y = vec.y;
    float z = vec.z;

    for(int i = 0; i < numOctaves; i++){
        t += a * perlin(f * x + avg_r, f * y + avg_r, f * z + avg_r, f, seed);
        f *= 2.0;
        sumA += a;
        a *= G;
    }

    return t / sumA;
}