
using UnityEngine;
using System.Collections.Generic;

public enum MotionPathEventType {
	STOP,
	CHANGE_PATH,
}

public enum MotionPathEventTrigger {
	NEVER,
	ALWAYS,

	ON_FIRST_HIT,
}

[System.Serializable]
public class MotionPathEvent {
	public MotionPathEventType type;
	public MotionPathEventTrigger trigger;
}

public class MotionPathNode : MonoBehaviour {
	public bool override_speed = false;
	public float speed = 1.0f;

	public bool flip_direction = false;

	public bool has_event = false;
	public MotionPathEvent evt;
}