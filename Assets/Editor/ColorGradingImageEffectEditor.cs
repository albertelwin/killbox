using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ColorGradingImageEffect))]
class ColorGradingImageEffectEditor : Editor {
	SerializedObject serialized_object;

	SerializedProperty red_channel;
	SerializedProperty green_channel;
	SerializedProperty blue_channel;

	SerializedProperty saturation;

	void OnEnable() {
		serialized_object = new SerializedObject(target);

		saturation = serialized_object.FindProperty("saturation");

		red_channel = serialized_object.FindProperty("red_channel");
		green_channel = serialized_object.FindProperty("green_channel");
		blue_channel = serialized_object.FindProperty("blue_channel");

		serialized_object.ApplyModifiedProperties();
	}

	public override void OnInspectorGUI() {
		serialized_object.Update();

		saturation.floatValue = EditorGUILayout.Slider( "Saturation", saturation.floatValue, 0.0f, 5.0f);

		bool apply_curve_changes = false;

		EditorGUILayout.PropertyField(red_channel, new GUIContent("Red")); apply_curve_changes = apply_curve_changes || GUI.changed;
		EditorGUILayout.PropertyField(green_channel, new GUIContent("Green")); apply_curve_changes = apply_curve_changes || GUI.changed;
		EditorGUILayout.PropertyField(blue_channel, new GUIContent("Blue")); apply_curve_changes = apply_curve_changes || GUI.changed;

		serialized_object.ApplyModifiedProperties();
		if(apply_curve_changes) {
			(serialized_object.targetObject as ColorGradingImageEffect).gameObject.SendMessage("UpdateTextures");
		}
	}
}
