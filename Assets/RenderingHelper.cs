using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class RenderingHelper : MonoBehaviour
{
    Camera cam;
    public ComputeShader computeShader;

    public Cubemap cubemap;

    RenderTexture renderTexture;
    ComputeBuffer sphereBuffer;
    ComputeBuffer blackholeBuffer;
    ComputeBuffer boxBuffer;
    [SerializeField] private int maxDepth = 5;
    [SerializeField] private float focusDistance = 10.0f;
    [SerializeField] private float defocusStrength = 0.0f;

    void Start()
    {
        Init();
        initRenderTexture();

        setScene();
        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.Dispatch(0, Mathf.CeilToInt(renderTexture.width / 8.0f), Mathf.CeilToInt(renderTexture.height / 8.0f), 1);

        sphereBuffer.Dispose();
        blackholeBuffer.Dispose();
        boxBuffer.Dispose();
    }

    private void Init()
    {
        cam = Camera.main;
    }

    private void initRenderTexture()
    {
        if (renderTexture == null || renderTexture.width != Screen.width || renderTexture.height != Screen.height)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
            }
            renderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(renderTexture, dest);
    }

    void setScene()
    {
        computeShader.SetMatrix("inverseProjectionMatrix", cam.projectionMatrix.inverse);
        computeShader.SetMatrix("cameraToWorldMatrix", cam.cameraToWorldMatrix);
        computeShader.SetInt("maxDepth", maxDepth);
        computeShader.SetFloat("focusDistance", focusDistance);
        computeShader.SetFloat("defocusStrength", defocusStrength);
        computeShader.SetTexture(0, "_skybox", cubemap);

        createSpheres();
        createBlackHoles();
        createBoxes();
    }

    void createSpheres()
    {
        RaytracingSphere[] sphereObjects = FindObjectsOfType<RaytracingSphere>();
        Sphere[] spheres = new Sphere[sphereObjects.Length + 1];

        spheres[0] = new Sphere()
        {
            position = new Vector3(0.0f, 0.0f, 0.0f),
            radius = 0.0f,
            material = new RaytracingMaterial()
            {
                color = new Color(0.0f, 0.0f, 0.0f),
                emissionColor = new Color(0.0f, 0.0f, 0.0f),
                specularColor = Color.black,
                roughness = 0.0f,
                specularProbability = 0.0f,
                emissionStrength = 0.0f,
            }
        };

        for (int i = 1; i < spheres.Length; i++)
        {
            spheres[i] = new Sphere()
            {
                position = sphereObjects[i - 1].transform.position,
                radius = sphereObjects[i - 1].transform.localScale.x * 0.5f,
                material = sphereObjects[i - 1].material,
            };
        }

        sphereBuffer = new ComputeBuffer(spheres.Length, sizeof(float) * 19);
        sphereBuffer.SetData(spheres);
        computeShader.SetBuffer(0, "spheres", sphereBuffer);

        computeShader.SetInt("numSpheres", sphereObjects.Length);
    }

    void createBlackHoles()
    {
        RaytracingBlackHole[] blackholeObjects = FindObjectsOfType<RaytracingBlackHole>();
        BlackHole[] blackholes = new BlackHole[blackholeObjects.Length + 1];

        blackholes[0] = new BlackHole()
        {
            sRadius = 0.0f,
            position = new Vector3(0.0f, 0.0f, 0.0f)
        };

        for (int i = 1; i < blackholes.Length; i++)
        {
            blackholes[i] = new BlackHole()
            {
                sRadius = blackholeObjects[i - 1].mass * 2,
                position = blackholeObjects[i - 1].transform.position
            };
        }

        blackholeBuffer = new ComputeBuffer(blackholes.Length, sizeof(float) * 4);
        blackholeBuffer.SetData(blackholes);
        computeShader.SetBuffer(0, "blackholes", blackholeBuffer);

        if (blackholeObjects.Length > 0)
        {
            computeShader.SetBool("hasBlackHole", true);
        }
        else
        {
            computeShader.SetBool("hasBlackHole", false);
        }
    }

    void createBoxes()
    {
        RaytracingBox[] boxObjects = FindObjectsOfType<RaytracingBox>();
        Box[] boxes = new Box[boxObjects.Length + 1];

        boxes[0] = new Box()
        {
            position = new Vector3(0.0f, 0.0f, 0.0f),
            corner = new Vector3(0.0f, 0.0f, 0.0f),
            material = new RaytracingMaterial()
            {
                color = new Color(0.0f, 0.0f, 0.0f),
                emissionColor = new Color(0.0f, 0.0f, 0.0f),
                specularColor = Color.black,
                roughness = 0.0f,
                specularProbability = 0.0f,
                emissionStrength = 0.0f,
            },
            rotation = new Vector4(0.0f, 0.0f, 0.0f, 0.0f)
        };

        for (int i = 1; i < boxes.Length; i++)
        {
            boxes[i] = new Box()
            {
                position = boxObjects[i - 1].transform.position,
                corner = new Vector3(boxObjects[i - 1].transform.localScale.x * 0.5f, boxObjects[i - 1].transform.localScale.y * 0.5f, boxObjects[i - 1].transform.localScale.z * 0.5f),
                material = boxObjects[i - 1].material,
                rotation = new Vector4(boxObjects[i - 1].transform.rotation.x, boxObjects[i - 1].transform.rotation.y, boxObjects[i - 1].transform.rotation.z, boxObjects[i - 1].transform.rotation.w)
            };
        }

        boxBuffer = new ComputeBuffer(boxes.Length, sizeof(float) * 25);
        boxBuffer.SetData(boxes);
        computeShader.SetBuffer(0, "boxes", boxBuffer);

        computeShader.SetInt("numBoxes", boxObjects.Length);
    }

    void OnDestroy()
    {
        if (sphereBuffer != null)
        {
            sphereBuffer.Dispose();
        }

        if (blackholeBuffer != null)
        {
            blackholeBuffer.Dispose();
        }

        if (boxBuffer != null)
        {
            boxBuffer.Dispose();
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}
