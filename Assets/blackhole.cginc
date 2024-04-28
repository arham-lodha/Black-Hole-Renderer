struct BlackHole{
    float sRadius;
    float3 position;
};

struct Ray {
    float3 position;
    float3 direction;
};

float3 dxdt(float3 p, float3 x, BlackHole b){
    float dist = length(x - b.position);
    float rsR = b.sRadius / (4.0 * dist);
    float multiplier = pow(abs(1 - rsR) , 2)/pow(1 + rsR, 6);

    return p * multiplier;
}

float3 dpdt(float3 p, float3 x, BlackHole b) {
    float r = length(x - b.position);
    
    float rsR = b.sRadius / (4.0 * r);
    float u = abs(1 - rsR);
    float v = abs(1 + rsR);

    return -1/(2 * pow(r, 3)) * (
        pow(length(p), 2) * (u * u) / pow(v, 7) + b.sRadius * x / (1 - pow(b.sRadius/(4 * r), 2))
    );
}

void rungeKuttaIntegration(inout Ray r, BlackHole b, float dt, int num_steps){
    float h = dt / float(num_steps);

    for(int i = 0; i < num_steps; i++) {
        float3 p1 = dpdt(r.direction, r.position, b);
        float3 p2 = dpdt(r.direction + h * 1.0 / 3.0 * p1, r.position, b);
        float3 p3 = dpdt(r.direction + h * (-1.0 / 3.0 * p1 + 1.0 * p2), r.position, b);
        float3 p4 = dpdt(r.direction + h * (1.0 * p1 - 1.0 * p2 + 1.0 * p3), r.position, b);

        r.direction = r.direction + h * (1.0 / 6.0 * p1 + 3.0/8.0 * p2 + 3.0/8.0 * p3 + 1.0/8.0 * p4);
    
        float3 x1 = dxdt(r.direction, r.position, b);
        float3 x2 = dxdt(r.direction, r.position + h * 1.0 / 3.0 * x1, b);
        float3 x3 = dxdt(r.direction, r.position + h * (-1.0 / 3.0 * x1 + 1.0 * x2), b);
        float3 x4 = dxdt(r.direction, r.position + h * (1.0 * x1 - 1.0 * x2 + 1.0 * x3), b);

        r.position = r.position + h * (1.0 / 6.0 * x1 + 3.0/8.0 * x2 + 3.0/8.0 * x3 + 1.0/8.0 * x4);
    }
}
