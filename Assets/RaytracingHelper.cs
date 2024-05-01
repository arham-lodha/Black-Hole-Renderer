using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks.Sources;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class RaytracingHelper : MonoBehaviour
{

    [Header("Shader")]
    public ComputeShader cameraRayShader;
    public ComputeShader traceRayShader;

    [Header("Textures")]
    public Cubemap cubemap;

    [Header("Raytracer Settings")]
    public int numSamples = 10;
    public float maxTimeIncrement = 0.5f;
    public int maxDepth = 100;
    private int stepCount = 0;

    private bool hardStop = false;

    private Camera _camera;

    //Textures
    private RenderTexture _position;
    private RenderTexture _direction;
    private RenderTexture _dt;
    private RenderTexture _throughput;
    private RenderTexture _result;
    private RenderTexture _isComplete;

    private bool startRender = true;
    private bool completed = false;
    private int width = Screen.width;
    private int height = Screen.height;
    private int numThreads = 8;
    public float mass = 1f;

    private float checkTime = 0f;
    private float checkTimer = 15f;

    private Vector2Int lastCheck;

    //buffers
    ComputeBuffer sphereBuffer;
    ComputeBuffer blackholeBuffer;
    ComputeBuffer boxBuffer;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(_result, dest);
    }

    private void InitRenderTextures()
    {
        setupTexture(ref _position, RenderTextureFormat.ARGBFloat, numSamples * width, height * numSamples);
        setupTexture(ref _direction, RenderTextureFormat.ARGBFloat, numSamples * width, height * numSamples);
        setupTexture(ref _throughput, RenderTextureFormat.ARGBFloat, numSamples * width, height * numSamples);
        setupTexture(ref _result, RenderTextureFormat.ARGBFloat, numSamples * width, height * numSamples);
        setupTexture(ref _isComplete, RenderTextureFormat.RFloat, numSamples * width, height * numSamples);
        // setupTexture(ref _dt, RenderTextureFormat.RFloat, numSamples * width, height * numSamples);
    }

    Vector3 toCartesianCoordinates(Vector3 spherical)
    {
        float r = spherical.x;
        float theta = spherical.y;
        float phi = spherical.z;

        return new Vector3(
            r * Mathf.Sin(theta) * Mathf.Cos(phi),
            r * Mathf.Cos(phi),
            r * Mathf.Sin(theta) * Mathf.Sin(phi)
        );
    }

    private void setShaderParams()
    {

        // Pre-convert camera position to spherical
        Vector3 cart = _camera.transform.position;
        float r = cart.magnitude;
        float theta = Mathf.Atan2(new Vector2(cart.x, cart.z).magnitude, cart.y);
        float phi = Mathf.Atan2(cart.z, cart.x);



        // Camera Compute Shader
        cameraRayShader.SetMatrix("inverseProjectionMatrix", _camera.projectionMatrix.inverse);
        cameraRayShader.SetMatrix("cameraToWorldMatrix", _camera.cameraToWorldMatrix);
        cameraRayShader.SetInt("numSamples", numSamples);
        cameraRayShader.SetInt("width", width * numSamples);
        cameraRayShader.SetInt("height", height * numSamples);
        cameraRayShader.SetFloat("mass", mass);
        cameraRayShader.SetFloat("maxTimeIncrement", maxTimeIncrement);
        cameraRayShader.SetVector("_camSpherical", new Vector3(r, theta, phi));

        //traceRayShader
        traceRayShader.SetTexture(0, "_skybox", cubemap);
        traceRayShader.SetInt("width", width * numSamples);
        traceRayShader.SetFloat("mass", mass);
        traceRayShader.SetBool("hardStop", hardStop);

        createSpheres();
        createBoxes();
    }

    private void createSpheres()
    {
        if (sphereBuffer != null)
        {
            sphereBuffer.Dispose();
        }
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
                specularProbability = 0.0f,
                emissionStrength = 0.0f,
                roughness = 0.0f,
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
        traceRayShader.SetBuffer(0, "spheres", sphereBuffer);

        traceRayShader.SetInt("numSpheres", sphereObjects.Length);
    }

    private void createBlackHoles()
    {
        if (blackholeBuffer != null)
        {
            blackholeBuffer.Dispose();
        }
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
        traceRayShader.SetBuffer(0, "blackholes", blackholeBuffer);

        if (blackholeObjects.Length > 0)
        {
            traceRayShader.SetBool("hasBlackHole", true);
        }
        else
        {
            traceRayShader.SetBool("hasBlackHole", false);
        }
    }

    private void createBoxes()
    {
        if (boxBuffer != null)
        {
            boxBuffer.Dispose();
        }
        RaytracingBox[] boxObjects = FindObjectsOfType<RaytracingBox>();
        Box[] boxes = new Box[boxObjects.Length + 1];

        boxes[0] = new Box()
        {
            position = new Vector3(0.0f, 0.0f, 0.0f),
            corner = new Vector3(0.0f, 0.0f, 0.0f),
            rotation = new Vector4(0.0f, 0.0f, 0.0f, 0.0f),
            material = new RaytracingMaterial()
            {
                color = new Color(0.0f, 0.0f, 0.0f),
                emissionColor = new Color(0.0f, 0.0f, 0.0f),
                specularColor = Color.black,
                specularProbability = 0.0f,
                emissionStrength = 0.0f,
                roughness = 0.0f,
            },
        };

        for (int i = 1; i < boxes.Length; i++)
        {
            boxes[i] = new Box()
            {
                position = boxObjects[i - 1].transform.position,
                corner = new Vector3(boxObjects[i - 1].transform.localScale.x * 0.5f,
                boxObjects[i - 1].transform.localScale.y * 0.5f,
                boxObjects[i - 1].transform.localScale.z * 0.5f),
                rotation = new Vector4(
                    boxObjects[i - 1].transform.rotation.x,
                    boxObjects[i - 1].transform.rotation.y,
                    boxObjects[i - 1].transform.rotation.z,
                    boxObjects[i - 1].transform.rotation.w),
                material = boxObjects[i - 1].material,
            };
        }

        boxBuffer = new ComputeBuffer(boxes.Length, sizeof(float) * 25);
        boxBuffer.SetData(boxes);
        traceRayShader.SetBuffer(0, "boxes", boxBuffer);

        traceRayShader.SetInt("numBoxes", boxObjects.Length);
    }



    // Update is called once per frame
    void Update()
    {
        if (!completed)
        {
            if (startRender)
            {
                Debug.Log("Starting Render");

                startRender = false;
                stepCount = 0;
                lastCheck = Vector2Int.zero;

                setShaderParams();
                generateCameraRays();
            }
            else
            {
                traceRay();
                stepCount++;

                if (hardStop)
                {
                    OnComplete();
                }

                if (stepCount >= maxDepth)
                {
                    hardStop = true;
                    traceRayShader.SetBool("hardStop", hardStop);
                }

                if (Time.time - checkTime > checkTimer)
                {
                    checkTime = Time.time;
                    checkCompletion();
                }
            }
        }
    }

    // Helper Methods

    private void setupTexture(ref RenderTexture renderTexture, RenderTextureFormat format, int width, int height)
    {
        if (renderTexture == null || renderTexture.width != Screen.width || renderTexture.height != Screen.height)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
            }
            renderTexture = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
        renderTexture.wrapMode = TextureWrapMode.Clamp;
    }

    private void generateCameraRays()
    {
        InitRenderTextures();

        //set textures
        cameraRayShader.SetTexture(0, "Position", _position);
        cameraRayShader.SetTexture(0, "Direction", _direction);
        cameraRayShader.SetTexture(0, "Throughput", _throughput);
        cameraRayShader.SetTexture(0, "Result", _result);
        cameraRayShader.SetTexture(0, "IsComplete", _isComplete);
        // cameraRayShader.SetTexture(0, "DT", _dt);

        int numThreadsX = Mathf.CeilToInt(numSamples * width / numThreads);
        int numThreadsY = Mathf.CeilToInt(numSamples * height / numThreads);

        cameraRayShader.Dispatch(0, numThreadsX, numThreadsY, 1);
    }

    private void traceRay()
    {
        traceRayShader.SetTexture(0, "Position", _position);
        traceRayShader.SetTexture(0, "Direction", _direction);
        traceRayShader.SetTexture(0, "Throughput", _throughput);
        traceRayShader.SetTexture(0, "Result", _result);
        traceRayShader.SetTexture(0, "IsComplete", _isComplete);
        // traceRayShader.SetTexture(0, "DT", _dt);

        int numThreadsX = Mathf.CeilToInt(numSamples * width / numThreads);
        int numThreadsY = Mathf.CeilToInt(numSamples * height / numThreads);

        traceRayShader.Dispatch(0, numThreadsX, numThreadsY, 1);
    }

    private void checkCompletion()
    {
        Texture2D completedTex = renderToTexture(_isComplete, TextureFormat.RFloat);

        // Loop over pixels searching for incomplete
        for (int i = lastCheck.x; i < _isComplete.width; i++)
        {
            for (int j = lastCheck.y; j < _isComplete.width; j++)
            {
                if (completedTex.GetPixel(i, j).r == 0)
                {
                    // Debug.Log("Incomplete on pixel: (" + i.ToString() + ", " + j.ToString() + ")");
                    Destroy(completedTex);
                    lastCheck = new Vector2Int(i, j);
                    return;
                }
            }
        }

        // Run method if not broken
        Destroy(completedTex);
        Debug.Log("All pixels rendered successfully.");
        OnComplete();
    }

    private void OnComplete()
    {
        completed = true;

        Debug.Log("Finished Up!");
    }

    private Texture2D renderToTexture(RenderTexture tex, TextureFormat format)
    {
        RenderTexture.active = tex;
        Texture2D outTex = new Texture2D(tex.width, tex.height, format, false);
        outTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        outTex.Apply(false);
        RenderTexture.active = null;

        return outTex;
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

        if (_position != null)
        {
            _position.Release();
        }

        if (_direction != null)
        {
            _direction.Release();
        }

        if (_isComplete != null)
        {
            _position.Release();
        }

        if (_throughput != null)
        {
            _throughput.Release();
        }

        if (_result != null)
        {
            _result.Release();
        }

        if (_dt != null)
        {
            _result.Release();
        }
    }
}
