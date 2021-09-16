using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCS : MonoBehaviour
{
	Mesh mesh;

    public ComputeShader cs;
    public ComputeShader cst;

    public ComputeBuffer cb;

    public int resolution;
	public float perturbStrength;

	// Start is called before the first frame update
	void Start()
    {
        
    }

    private void OnValidate()
    {
		SphereMesh sphereMesh = new SphereMesh(resolution);
		var vertices = sphereMesh.Vertices;
		var triangles = sphereMesh.Triangles;

		ComputeHelper.CreateStructuredBuffer<Vector3>(ref cb, vertices);

		float edgeLength = (vertices[triangles[0]] - vertices[triangles[1]]).magnitude;

		// Set heights
		//float[] heights = body.shape.CalculateHeights(cb);

		// Perturb vertices to give terrain a less perfectly smooth appearance
		if (cs)
		{
			ComputeShader perturbShader = cs;
			float maxperturbStrength = perturbStrength * edgeLength / 2;

			perturbShader.SetBuffer(0, "vertices", cb);
			perturbShader.SetInt("repeats", vertices.Length);
			perturbShader.SetFloat("maxStrength", maxperturbStrength);

			ComputeHelper.Run(perturbShader, vertices.Length);
			Vector3[] pertData = new Vector3[vertices.Length];
			cb.GetData(vertices);
		}

		// Calculate terrain min/max height and set heights of vertices
		//float minHeight = float.PositiveInfinity;
		//float maxHeight = float.NegativeInfinity;
		//for (int i = 0; i < heights.Length; i++)
		//{
		//	float height = heights[i];
		//	vertices[i] *= height;
		//	minHeight = Mathf.Min(minHeight, height);
		//	maxHeight = Mathf.Max(maxHeight, height);
		//}

		// Create mesh
		

		CreateMesh(ref mesh, vertices.Length);
		mesh.SetVertices(vertices);
		mesh.SetTriangles(triangles, 0, true);
		mesh.RecalculateNormals(); //
		GetComponent<MeshFilter>().sharedMesh = mesh;

		RenderTexture text = new RenderTexture(512,512,16);
		text.enableRandomWrite = true;
		text.Create();

		cst.SetTexture(0, "Result", text);

		GetComponent<MeshRenderer>().sharedMaterial.mainTexture = text;
	}

	void CreateMesh(ref Mesh mesh, int numVertices)
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
		mesh.indexFormat = (numVertices < vertexLimit16Bit) ? UnityEngine.Rendering.IndexFormat.UInt16 : UnityEngine.Rendering.IndexFormat.UInt32;
	}
}
