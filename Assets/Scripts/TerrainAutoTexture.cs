using UnityEngine;

public class TerrainAutoTexture : MonoBehaviour
{
    // The terrain to texture
    public Terrain terrain;

    // The textures to apply to the terrain based on height and steepness ranges
    public Texture2D[] textures;

    // The height ranges for each texture (0 to 1)
    public float[] heightRanges;

    // The steepness ranges for each texture (0 to 90)
    public float[] steepnessRanges;

    // The splatmap resolution of the terrain
    public int splatmapResolution = 512;

    // The method to apply the textures to the terrain
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
        float[,,] splatmap = new float[splatmapResolution, splatmapResolution, textures.Length];

        // Loop through each pixel of the splatmap
        for (int i = 0; i < splatmapResolution; i++)
        {
            for (int j = 0; j < splatmapResolution; j++)
            {
                // Get the normalized coordinates of the pixel
                float x = (float)i / (float)splatmapResolution;
                float y = (float)j / (float)splatmapResolution;

                // Get the interpolated height at this point
                float height = heightmap[(int)(x * heightmapResolution), (int)(y * heightmapResolution)];

                // Get the normalized steepness at this point
                //float steepness = terrainData.GetSteepness(x, y) / 90f;
                float steepness = terrainData.GetSteepness(x, y);

                // Loop through each texture
                for (int k = 0; k < textures.Length; k++)
                {
                    // Check if the height and steepness are within the ranges for this texture
                    //if (height >= heightRanges[k] && height < heightRanges[k + 1] && steepness >= steepnessRanges[k] && steepness < steepnessRanges[k + 1])
                    //if (height >= heightRanges[k] && height < heightRanges[k + 1])
                    if(steepness >= steepnessRanges[k] && steepness < steepnessRanges[k + 1])
                    {
                        // Set the splatmap value to 1 for this texture
                        splatmap[i, j, k] = 1f;
                    }
                    else
                    {
                        // Set the splatmap value to 0 for this texture
                        splatmap[i, j, k] = 0f;
                    }
                }
            }
        }

        // Set the splatmap to the terrain data
        terrainData.SetAlphamaps(0, 0, splatmap);
    }
}