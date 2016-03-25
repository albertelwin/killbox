
using UnityEngine;
using System.Collections.Generic;

public enum MotionPathEventType {
	STOP,
	FLIP_DIRECTION,
	CHANGE_PATH,

	COUNT,
}

public enum MotionPathTriggerType {
	NEVER,
	ALWAYS,

	FIRST_HIT,

	COUNT,
}

[System.Serializable]
public class MotionPathEvent {
	public MotionPathEventType event_type;
	public MotionPathTriggerType trigger_type;
}

public class MotionPathNode : MonoBehaviour {
	public bool override_speed;
	public float speed;

	public List<MotionPathEvent> event_list;
}