using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Planet Generation/Settings/Space body shading")]
public class SpaceBodyShadingSettings : ScriptableObject
{
    ComputeShader shader;

    Vector4[] cachedShading;
    ComputeBuffer shadingBuffer;


    public Vector4[] GenerateShadingData(ComputeBuffer vertexBuffer)
    {
        int numVertices = vertexBuffer.count;
        Vector4[] shadingData = new Vector4[numVertices];

        if(shader)
        {
            // Assign

            shader.SetInt("numVertices", numVertices);
            shader.SetBuffer(0, "vertices", vertexBuffer);
            ComputeHelper.CreateAndSetBuffer<Vector4>(ref shadingBuffer, numVertices, shader, "shadingData");

            // Run


            //Get result


        }

        return shadingData;
    }
}
