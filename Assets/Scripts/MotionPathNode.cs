
using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class MotionPathNode : MonoBehaviour {
	public bool override_speed = false;
	public float speed = 1.0f;

	public bool flip_direction = false;

	public bool stop = false;
	public bool stop_forever = false;
	public float stop_time = 1.0f;
	public MotionPathAnimationType stop_animation = MotionPathAnimationType.IDLE;

#if UNITY_EDITOR
	public void OnEnable() {
		name = "Node";
	}
#endif
}