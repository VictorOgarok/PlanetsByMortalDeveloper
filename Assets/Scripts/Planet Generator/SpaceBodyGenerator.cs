using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO
//-Improve noises(masks and other stuff)
//-Make LODs
//-Implement gravity
//-Tesselation?
//-Faster sphere generation?


[ExecuteInEditMode]
public class SpaceBodyGenerator : MonoBehaviour
{
    public SpaceBodySettings settings;

    Mesh previewMesh;
    Mesh collisionMesh;

    [Range(16,1024)]
    public int resolution;

    Dictionary<int, SphereMesh> spheres;

    public bool shapeSettingsUpdate = false;
    private Vector2 heightMinMax;
    public Vector3 offset;
    public bool logTimers = true;

    ComputeBuffer vertexBuffer;

    void Start()
    {
        if (Application.isPlaying)
        {
            GameGeneration();
            //some actions in game mode
        }
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            PreviewGeneration();
        }
    }

    private void GameGeneration()
    {
        if (CanGenerateMesh())
        {
            // Manage LODs here

            GenerateCollisionMesh(resolution);
            MeshFilter filter;
            if (!TryGetComponent<MeshFilter>(out filter))
            {
                filter = gameObject.AddComponent<MeshFilter>();
            }

            MeshRenderer renderer;
            if (!TryGetComponent<MeshRenderer>(out renderer))
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }
            MeshCollider collider;
            if (!TryGetComponent<MeshCollider>(out collider))
            {
                collider = gameObject.AddComponent<MeshCollider>();
            }
            Physics.BakeMesh(collisionMesh.GetInstanceID(), false);
            collider.sharedMesh = collisionMesh;

            //Заменить на систему лодов
            heightMinMax = GenerateTerrainMesh(ref previewMesh, GetTerrainRes());
            filter.sharedMesh = previewMesh;

        }

        ReleaseBuffers();
    }

    private void PreviewGeneration()
    {
        ComputeHelper.shouldReleaseEditModeBuffers -= ReleaseBuffers;
        ComputeHelper.shouldReleaseEditModeBuffers += ReleaseBuffers;

        if (CanGenerateMesh())
        {
            if (shapeSettingsUpdate)
            {
                shapeSettingsUpdate = false;

                var terrainMeshTimer = System.Diagnostics.Stopwatch.StartNew();
                heightMinMax = GenerateTerrainMesh(ref previewMesh, GetTerrainRes());

                LogTimer(terrainMeshTimer, "Generate terrain mesh");

                DrawPreviewMesh();
            }
        }
        //Shading update 

        ReleaseBuffers();
    }

    private void DrawPreviewMesh()
    {
        if (!TryGetComponent(out MeshRenderer meshRenderer))
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        if (!TryGetComponent(out MeshFilter meshFilter))
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        meshFilter.mesh = previewMesh;
    }

    private int GetTerrainRes()
    {


        return resolution;
    }

    public Vector2 GenerateTerrainMesh(ref Mesh mesh, int res)
    {
        var (vertices, triangles) = CreateSphereMesh(res);
        ComputeHelper.CreateStructuredBuffer<Vector3>(ref vertexBuffer, vertices);

        float[] heights = settings.shape.CalculateHeights(vertexBuffer, offset);

        float minHeight = float.PositiveInfinity;
        float maxHeight = float.NegativeInfinity;
        for (int i = 0; i < heights.Length; i++)
        {
            float height = settings.shape.radius * (1 + heights[i]);
            vertices[i] *= height;
            minHeight = Mathf.Min(minHeight, height);
            maxHeight = Mathf.Max(maxHeight, height);
        }
        CreateMesh(ref mesh, vertices.Length);
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0, true);
        mesh.RecalculateNormals();




        return new Vector2(minHeight, maxHeight);
    }

    void GenerateCollisionMesh(int res)
    {
        var (vertices, triangles) = CreateSphereMesh(res);
        ComputeHelper.CreateStructuredBuffer<Vector3>(ref vertexBuffer, vertices);

        // Set heights
        float[] heights = settings.shape.CalculateHeights(vertexBuffer,offset);
        for (int i = 0; i < vertices.Length; i++)
        {
            float height = heights[i];
            vertices[i] *= height;
        }

        // Create mesh
        CreateMesh(ref collisionMesh, vertices.Length);
        collisionMesh.vertices = vertices;
        collisionMesh.triangles = triangles;
    }

    void CreateMesh(ref Mesh mesh, int length)
    {
        const int vertexLimit16Bit = 1 << 16 - 1; // 65535
        if (mesh == null)
        {
            mesh = new Mesh();
        }
        else
        {
            mesh.Clear();
        }
        mesh.indexFormat = (length < vertexLimit16Bit) ? UnityEngine.Rendering.IndexFormat.UInt16 : UnityEngine.Rendering.IndexFormat.UInt32;
    }

    private (Vector3[] vertices, int[] triangles) CreateSphereMesh(int res)
    {
        if (spheres == null)
        {
            spheres = new Dictionary<int, SphereMesh>();
        }

        if (!spheres.ContainsKey(res)) spheres.Add(res, new SphereMesh(res));

        var vertices = new Vector3[spheres[res].Vertices.Length];
        var triangles = new int[spheres[res].Triangles.Length];
        System.Array.Copy(spheres[res].Vertices, vertices, vertices.Length);
        System.Array.Copy(spheres[res].Triangles, triangles, triangles.Length);
        return (vertices, triangles);
    }

    private void LogTimer(System.Diagnostics.Stopwatch sw, string text)
    {
        if (logTimers)
        {
            Debug.Log(text + " " + sw.ElapsedMilliseconds + " ms.");
        }
    }

    bool CanGenerateMesh()
    {
        return ComputeHelper.CanRunEditModeCompute && settings.shape;
    }

    void ReleaseBuffers()
    {
        ComputeHelper.Release(vertexBuffer);
        if (settings.shape)
        {
            settings.shape.ReleaseBuffers();
        }
    }

    void OnValidate()
    {
        if (settings)
        {
            if (settings.shape)
            {
                settings.shape.OnSettingsChanged -= OnShapeSettingChanged;
                settings.shape.OnSettingsChanged += OnShapeSettingChanged;
            }
        }
    }

    void OnShapeSettingChanged()
    {
        shapeSettingsUpdate = true;
    }
}
