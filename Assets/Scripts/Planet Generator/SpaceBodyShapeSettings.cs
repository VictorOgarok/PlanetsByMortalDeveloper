using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static NoiseLayer.NoiseSettings.NoiseType;

[CreateAssetMenu(menuName = "Planet Generation/Settings/Space body shape")]
public class SpaceBodyShapeSettings : ScriptableObject
{
    public float radius = 1;
    [SerializeField]
    NoiseLayer[] noiseLayers;
    public int test;
    public ComputeShader[] heightShaders;
    public ComputeShader mergeShader;

    ComputeBuffer heightsBuffer;
    ComputeBuffer finalHeightsBuffer;
    ComputeBuffer heightLayersBuffer;

    public event Action OnSettingsChanged;

    public float[] CalculateHeights(ComputeBuffer vertices, Vector3 offset)
    {
        List<float[]> heightLayers = new List<float[]>();
        for (int i = 0; i < noiseLayers.Length; i++)
        {
            if(!noiseLayers[i].enabled)
            {
                continue;
            }
            //Assign
            var set = noiseLayers[i].noiseSettings;
            var heightShader = heightShaders[(int)set.noiseType];
            NoiseLayer.NoiseSettings.SimpleNoiseSettings sets;
            
            switch (set.noiseType)
            {
                case Simple:
                    sets = set.simpleNoiseSettings;
                    break; 
                case Ridgid:
                    sets = set.ridgidNoiseSettings;
                    heightShader.SetFloat("weightMultiplier", set.ridgidNoiseSettings.weightMultiplier);
                    break;
                case Continent:
                    sets = set.continentNoiseSettings;
                    heightShader.SetFloat("oceanFloorDepth", set.continentNoiseSettings.oceanFloorDepth);
                    heightShader.SetFloat("oceanFloorSmoothing", set.continentNoiseSettings.oceanFloorSmoothing);
                    heightShader.SetFloat("oceanDepthMultiplier", set.continentNoiseSettings.oceanDepthMultiplier);
                    break;
                default:
                    throw new NotImplementedException("Not implemented this noise");
            }
            heightShader.SetFloat("baseRoughness", sets.baseRoughness);
            heightShader.SetInt("numLayers", sets.numLayers);
            heightShader.SetFloats("offset", offset.x, offset.y, offset.z);
            heightShader.SetFloat("roughness", sets.roughness);
            heightShader.SetFloat("persistence", sets.persistence);
            heightShader.SetFloat("strength", sets.strength);
            heightShader.SetInt("numVertices", vertices.count);
            heightShader.SetBuffer(0, "vertices", vertices);
            ComputeHelper.CreateAndSetBuffer<float>(ref heightsBuffer, vertices.count, heightShader, "heights");

            //Run
            ComputeHelper.Run(heightShader, vertices.count);

            //Get result
            var heights = new float[vertices.count];
            heightsBuffer.GetData(heights);
            heightLayers.Add(heights);
        }

        return MergeHeights(heightLayers, vertices.count);
    }

    public float[] MergeHeights(List<float[]> heights,int vertsCount)
    {
        var heightLayers = new float[heights.Count * vertsCount];
        for (int i = 0; i < heights.Count; i++)
        {
            heights[i].CopyTo(heightLayers, vertsCount * i);
        }

        //Assign
        ComputeHelper.CreateStructuredBuffer(ref heightLayersBuffer, heightLayers);
        ComputeHelper.CreateAndSetBuffer<float>(ref finalHeightsBuffer, vertsCount, mergeShader, "result");

        mergeShader.SetInt("numVertices", vertsCount);
        mergeShader.SetInt("numLayers", heights.Count);
        mergeShader.SetBuffer(0, "heights", heightLayersBuffer);

        //Run
        ComputeHelper.Run(mergeShader, vertsCount);

        //Get result
        var result = new float[vertsCount];
        finalHeightsBuffer.GetData(result);

        return result;
    }

    public void ReleaseBuffers()
    {
        ComputeHelper.Release(heightsBuffer,finalHeightsBuffer,heightLayersBuffer);
    }

    void OnValidate()
    {
        if (OnSettingsChanged != null) OnSettingsChanged();
    }
}
