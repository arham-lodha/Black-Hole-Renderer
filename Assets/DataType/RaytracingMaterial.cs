using UnityEngine;


[System.Serializable]
public struct RaytracingMaterial
{
    public Color color; //4
    public Color emissionColor; // 4
    public Color specularColor; // 4
    [Range(0, 1)] public float emissionStrength; // 1
    [Range(0, 1)] public float specularProbability; // 1
    [Range(0, 1)] public float roughness; // 1

    // Total = 15
    public void SetDefaultValues()
    {
        color = Color.white;
        emissionColor = Color.white;
        specularColor = Color.white;

        emissionStrength = 0.0f;
        specularProbability = 0.0f;
        roughness = 1.0f;

    }
}
