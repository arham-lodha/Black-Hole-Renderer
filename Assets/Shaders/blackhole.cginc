struct BlackHole{
    float sRadius;
    float3 position;
};

struct Ray {
    float3 position;
    float3 direction;
};

float3 dxdt(float3 p, float3 x, float mass){
    float dist = length(x.xyz);
    float rs = 2.0 * mass;
    float rsR = rs / (4.0 * dist);
    float multiplier = pow(abs(1 - rsR) , 2)/pow(1 + rsR, 6);

    return p * multiplier;
}

float3 dpdt(float3 p, float3 x, float mass) {
    float r = length(x.xyz);
    
    float rs = 2.0 * mass;
    float rsR = rs / (4.0 * r);
    float u = abs(1 - rsR);
    float v = abs(1 + rsR);

    return -1/(2 * pow(r, 3)) * (
        pow(length(p), 2) * (u * u) / pow(v, 7) + rs * x.xyz / (1 - pow(rs / (4 * r), 2))
    );
}

// float4 dxdt(float3 p, float4 x, float m){
//     float r = x.x;
//     float rs = 2.0 * m;
//     float A = (1.0 - rs/r);

//     float dt = sqrt(((A * p.x * p.x) + (p.y * p.y / (r * r)) + (p.z * p.z/ (r * r * sin(x.y) * sin(x.y)))) / A);

//     float drdt = A * r / dt;

//     float dthetadt = x.y / (dt * r * r);
    
//     float dphidt = p.z / (dt * r * r * sin(x.y) * sin(x.y));

//     return float4(drdt, dt, dthetadt, dphidt);
// }

// float3 dpdt(float3 p, float4 x, float m){
//     float r = x.x;
//     float rs = 2.0 * m;
//     float A = (1.0 - rs / r);
//     float sth = sin(x.y);
//     float a = sqrt(1.0 - rs / r);
//     float dt = sqrt(((A * p.x * p.x) + (p.y * p.y / (r * r)) + (p.z * p.z/ (r * r * sth * sth))) / A);

//     float dpr = (-dt * 2.0 * rs / (r * r)) + (-p.x * p.x * rs / (2.0 * dt * r * r)) + (p.y * p.y / r * r* r * dt) + (p.z * p.z / (r * r * sth * sth * dt));
//     float dptheta = (p.z * p.z / (r * r * sth * sth * tan(x.y) * dt));
//     float dpphi = 0.0;

//     return float3(dpr, dptheta, dpphi);
// }

void rungeKuttaIntegration(float mass, float dt, int num_steps, inout float4 position, inout float3 direction){
    float h = dt / float(num_steps);

    for(int i = 0; i < num_steps; i++) {
        float3 p1 = dpdt(direction, position.xyz, mass);
        float3 p2 = dpdt(direction + h * 1.0 / 3.0 * p1, position.xyz, mass);
        float3 p3 = dpdt(direction + h * (-1.0 / 3.0 * p1 + 1.0 * p2), position.xyz, mass);
        float3 p4 = dpdt(direction + h * (1.0 * p1 - 1.0 * p2 + 1.0 * p3), position.xyz, mass);

        direction = direction + h * (1.0 / 6.0 * p1 + 3.0/8.0 * p2 + 3.0/8.0 * p3 + 1.0/8.0 * p4);
        // direction = normalize(direction);
    
        float3 x1 = dxdt(direction, position.xyz, mass);
        float3 x2 = dxdt(direction, position.xyz + h * 1.0 / 3.0 * x1, mass);
        float3 x3 = dxdt(direction, position.xyz + h * (-1.0 / 3.0 * x1 + 1.0 * x2), mass);
        float3 x4 = dxdt(direction, position.xyz + h * (1.0 * x1 - 1.0 * x2 + 1.0 * x3), mass);

        float3 temp = position + h * (1.0 / 6.0 * x1 + 3.0/8.0 * x2 + 3.0/8.0 * x3 + 1.0/8.0 * x4);

        position.x = temp.x;
        position.y = temp.y;
        position.z = temp.z;
    }
}
