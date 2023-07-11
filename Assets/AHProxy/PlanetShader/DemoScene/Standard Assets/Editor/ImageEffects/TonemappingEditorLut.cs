using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor (typeof(TonemappingLut))]
class TonemappingLutEditor : Editor
{
	SerializedObject serObj;

    // REINHARD parameter
    SerializedProperty adaptiveGreyOffset;
    SerializedProperty adaptionSpeed;

    SerializedProperty adaptiveMin;
    SerializedProperty adaptiveMax;

    // LUT
    SerializedProperty lutExposureBias;

    SerializedProperty lutWhiteBalance;

    SerializedProperty lutContrast;
    SerializedProperty lutSaturation;
    SerializedProperty lutGamma;

    SerializedProperty lutToe;
    SerializedProperty lutShoulder;

    SerializedProperty lutShadows;
    SerializedProperty lutMidtones;
    SerializedProperty lutHighlights;

    SerializedProperty tonemapperLut;
    SerializedProperty remapCurve;

    SerializedProperty userLutTex;

    SerializedProperty enableAdaptive;
    SerializedProperty enableFilmicCurve;
    SerializedProperty enableColorGrading;
    SerializedProperty enableUserLut;
    SerializedProperty enableColorCurve;
    SerializedProperty debugClamp;

    void OnEnable () {
		serObj = new SerializedObject (target);

        adaptiveGreyOffset = serObj.FindProperty("adaptiveGreyOffset");
		adaptionSpeed = serObj.FindProperty("adaptionSpeed");
		adaptiveMin = serObj.FindProperty("adaptiveMin");
        adaptiveMax = serObj.FindProperty("adaptiveMax");

        lutExposureBias = serObj.FindProperty("lutExposureBias");
		lutWhiteBalance = serObj.FindProperty("lutWhiteBalance");
		lutContrast = serObj.FindProperty("lutContrast");
		lutSaturation = serObj.FindProperty("lutSaturation");
		lutGamma = serObj.FindProperty("lutGamma");
		lutToe = serObj.FindProperty("lutToe");
		lutShoulder = serObj.FindProperty("lutShoulder");
		lutShadows = serObj.FindProperty("lutShadows");
		lutMidtones = serObj.FindProperty("lutMidtones");
        lutHighlights = serObj.FindProperty("lutHighlights");
        tonemapperLut = serObj.FindProperty("tonemapperLut");
        remapCurve = serObj.FindProperty("remapCurve");
        userLutTex = serObj.FindProperty("userLutTex");
        enableAdaptive = serObj.FindProperty("enableAdaptive");
        enableFilmicCurve = serObj.FindProperty("enableFilmicCurve");
        enableColorGrading = serObj.FindProperty("enableColorGrading");
        enableColorCurve = serObj.FindProperty("enableColorCurve");
        enableUserLut = serObj.FindProperty("enableUserLut");
        debugClamp = serObj.FindProperty("debugClamp");
    }

	public override void OnInspectorGUI () {
		serObj.Update ();
		
        EditorGUILayout.PropertyField(lutWhiteBalance, new GUIContent("White Balance", "Adjust the white color before tonemapping"));

        EditorGUILayout.PropertyField(enableFilmicCurve, new GUIContent("Use Filmic Curve", "Enable filmic curve with shoulder and toe."));
        if (enableFilmicCurve.boolValue)
        {
            EditorGUILayout.PropertyField(lutExposureBias, new GUIContent("  Exposure Bias", "Adjust the overall exposure of the scene"));
            EditorGUILayout.PropertyField(lutContrast, new GUIContent("  Contrast", "Contrast adjustment (log-space)."));
            EditorGUILayout.PropertyField(lutToe, new GUIContent("  Toe", "Toe of the filmic curve.  Affects the darker areas of the scene."));
            EditorGUILayout.PropertyField(lutShoulder, new GUIContent("  Shoulder", "Shoulder of the filmic curve.  Brings overexposed highlights back into range."));
        }

        EditorGUILayout.PropertyField(enableAdaptive, new GUIContent("Use Adaptive", "Enable Exposure Adaptation"));
        if (enableAdaptive.boolValue)
        {
            EditorGUILayout.PropertyField(adaptiveGreyOffset, new GUIContent("  Midpoint Adjustment", "Mid grey adjustment in F-Stops"));
            EditorGUILayout.PropertyField(adaptionSpeed, new GUIContent("  Adaption Speed", "Speed of linear adaptation"));
            EditorGUILayout.PropertyField(adaptiveMin, new GUIContent("  Adaptive Min", "The lowest possible exposure value.  Adjust this value to modify the brightest areas of your level."));
            EditorGUILayout.PropertyField(adaptiveMax, new GUIContent("  Adaptive Max", "The highest possible exposure value.  Adjust this value to modify the darkest areas of your level."));
        }
        
        EditorGUILayout.PropertyField(enableUserLut, new GUIContent("Use Custom LUT", "Enable predefined lookup table."));
        if (enableUserLut.boolValue)
        {
            EditorGUILayout.PropertyField(userLutTex, new GUIContent("  User Lut Tex", "Lookup table."));
        }

        EditorGUILayout.PropertyField(enableColorGrading, new GUIContent("Use Color Grading", "Enables colro grading operations"));
        if (enableColorGrading.boolValue)
        {
            EditorGUILayout.PropertyField(lutSaturation, new GUIContent("  Saturation", "Saturation"));
            EditorGUILayout.PropertyField(lutGamma, new GUIContent("  Gamma", "Gamma"));
            EditorGUILayout.PropertyField(lutShadows, new GUIContent("  Shadows", "Shadows Color"));
            EditorGUILayout.PropertyField(lutMidtones, new GUIContent("  Midtones", "Midtones Color"));
            EditorGUILayout.PropertyField(lutHighlights, new GUIContent("  Highlights", "Highlights Color"));
        }

        EditorGUILayout.PropertyField(enableColorCurve, new GUIContent("Use Curve", "Enable color curve adjustment"));
        if (enableColorCurve.boolValue)
        {
            EditorGUILayout.PropertyField(remapCurve, new GUIContent("  Curve", "Curve"));
        }

        EditorGUILayout.PropertyField(debugClamp, new GUIContent("Debug Clamp", "A debug mode that turns all overexposed pixels pink.  If no pixels are overexposed, then HDR tonemapping will not help you."));

        EditorGUILayout.PropertyField(tonemapperLut, new GUIContent("Tonemap Shader", "Should be Hidden/TonemapperLut.shader"));


        GUILayout.Label("All following effects will use LDR color buffers", EditorStyles.miniBoldLabel);
		
		serObj.ApplyModifiedProperties();
	}
}
