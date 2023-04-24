using UnityEngine;

public class TerrainAutoTexture : MonoBehaviour
{
    public Terrain terrain;
    [SerializeField] private TerrainAutoTextureScriptableObject settings;

    public void ApplyTextures()
    {
        // Get the terrain data
        TerrainData terrainData = terrain.terrainData;

        // Get the heightmap and its resolution
        float[,] heightmap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        int heightmapResolution = terrainData.heightmapResolution;

        // Get the size of the terrain
        float width = terrainData.size.x;
        float length = terrainData.size.z;

        // Create a new splatmap array
        float[,,] splatmap = new float[settings.splatmapResolution, settings.splatmapResolution, settings.numTextures];

        // Loop through each pixel of the splatmap
        for (int i = 0; i < settings.splatmapResolution; i++)
        {
            for (int j = 0; j < settings.splatmapResolution; j++)
            {
                // Get the normalized coordinates of the pixel
                float x = (float)i / (float)settings.splatmapResolution;
                float y = (float)j / (float)settings.splatmapResolution;

                // Get the interpolated height at this point
                float height = heightmap[(int)(x * heightmapResolution), (int)(y * heightmapResolution)];

                // Get the normalized steepness at this point
                //float steepness = terrainData.GetSteepness(x, y) / 90f;
                float steepness = terrainData.GetSteepness(x, y);

                // Loop through each texture
                for (int k = 0; k < settings.numTextures; k++)
                {
                    float steepnessRange = settings.steepnessRanges[k + 1] - settings.steepnessRanges[k];
                    float steepnessValue = steepness - settings.steepnessRanges[k];
                    float steepnessPercent = steepnessValue / steepnessRange;
                    float splatmapValue = settings.blendingCurve.Evaluate(steepnessPercent);
                    splatmap[i, j, k] = splatmapValue;
                }
            }
        }

        // Set the splatmap to the terrain data
        terrainData.SetAlphamaps(0, 0, splatmap);
    }
}