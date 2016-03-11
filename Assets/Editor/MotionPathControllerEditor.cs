
using System;
using UnityEditor;
using UnityEngine;

#pragma warning disable 0219

[CustomEditor(typeof(MotionPathController))]
class MotionPathControllerEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		MotionPathController controller = (MotionPathController)target;

		if(GUILayout.Button("Add Control Point")) {
			Transform control_point = (new GameObject("Control Point")).transform;
			control_point.parent = controller.transform;
		}
	}
}
