using UnityEngine;
using UnityEditor;
using System.Collections;

[InitializeOnLoad]
public class EditorInit {
	static EditorInit() {
		GameManager.set_world_brightness_(null, 1.0f);
		GameManager.set_infrared_mode(false);
	}
}
