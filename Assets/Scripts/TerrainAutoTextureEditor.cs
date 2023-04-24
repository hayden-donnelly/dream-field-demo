using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainAutoTexture))]
public class TerrainAutoTextureEditor : Editor
{
    // The method to draw the inspector GUI
    public override void OnInspectorGUI()
    {
        // Draw the default GUI
        DrawDefaultInspector();

        // Get the target script
        TerrainAutoTexture script = (TerrainAutoTexture)target;

        // Draw a button to apply the textures
        if (GUILayout.Button("Apply Textures"))
        {
            script.ApplyTextures();
        }
    }
}