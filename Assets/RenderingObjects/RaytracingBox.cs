using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaytracingBox : MonoBehaviour
{
    public RaytracingMaterial material;

    [SerializeField, HideInInspector] bool materialInitFlag;
    [SerializeField, HideInInspector] int materialObjectID;

    void OnValidate()
    {
        if (!materialInitFlag)
        {
            materialInitFlag = true;
            material.SetDefaultValues();
        }

        MeshRenderer renderer = GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            if (materialObjectID != gameObject.GetInstanceID())
            {
                renderer.sharedMaterial = new Material(renderer.sharedMaterial);
                materialObjectID = gameObject.GetInstanceID();

            }

            renderer.sharedMaterial.color = material.color;
        }
    }
}
