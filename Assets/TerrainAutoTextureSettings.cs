using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Settings", menuName = "ScriptableObjects/TerrainAutoTextureSettings", order = 1)]
public class TerrainAutoTextureScriptableObject : ScriptableObject
{
    public int numTextures;
    public float[] steepnessRanges; // 0-90
    public int splatmapResolution = 512;
    public AnimationCurve blendingCurve;
}
