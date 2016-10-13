
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

[ExecuteInEditMode]
public class MovingWater : MonoBehaviour {
	float scroll_speed = 0.5f;
	float offset = 0.0f;

	void Update() {

	}

#if UNITY_EDITOR
	void OnRenderObject() {
		Update();
		HandleUtility.Repaint();
	}
#endif
}
