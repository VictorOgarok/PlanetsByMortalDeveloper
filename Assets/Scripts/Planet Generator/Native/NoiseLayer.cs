using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseLayer
{
    public bool enabled = true;
    public int maskLayer=-1;
    public NoiseSettings noiseSettings;

    [System.Serializable]
    public class NoiseSettings
    {
        public enum NoiseType { Simple, Ridgid, Continent };
        public NoiseType noiseType;

        [ConditionalHide("noiseType", 0)]
        public SimpleNoiseSettings simpleNoiseSettings;
        [ConditionalHide("noiseType", 1)]
        public RidgidNoiseSettings ridgidNoiseSettings;
        [ConditionalHide("noiseType", 2)]
        public ContinentNoiseSettings continentNoiseSettings;

        [System.Serializable]
        public class SimpleNoiseSettings
        {
            public float baseRoughness;
            public int numLayers;
            public float roughness;
            public float persistence;
            public float strength;
        }

        [System.Serializable]
        public class RidgidNoiseSettings : SimpleNoiseSettings
        {
            public float weightMultiplier = .8f;
        }

        [System.Serializable]
        public class ContinentNoiseSettings : SimpleNoiseSettings
        {
            public float oceanFloorDepth;
            public float oceanFloorSmoothing;
            public float oceanDepthMultiplier;
        }
    }
}
