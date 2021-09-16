using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Planet Generation/Settings/Space body")]
public class SpaceBodySettings : ScriptableObject
{
    public SpaceBodyShapeSettings shape;
    public SpaceBodyShadingSettings shading;
}
