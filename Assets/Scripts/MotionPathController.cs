using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

public class MotionPathController : MonoBehaviour {
	[System.NonSerialized] public int control_count;
	[System.NonSerialized] public int control_index;

	public static Vector3 get_next_control_point(MotionPathController motion_path) {
		Transform child = motion_path.transform.GetChild(motion_path.control_index++);
		if(motion_path.control_index >= motion_path.control_count) {
			motion_path.control_index = 0;
		}

		return child.position;
	}

	public static Vector3 get_prev_control_point(MotionPathController motion_path) {
		Transform child = motion_path.transform.GetChild(motion_path.control_index--);
		if(motion_path.control_index <= 0) {
			motion_path.control_index = motion_path.control_count - 1;
		}

		return child.position;
	}

	void Start() {		
		control_count = transform.childCount;
		control_index = 0;
	}

#if UNITY_EDITOR
	void draw_arrow_gizmo(Vector3 from, Vector3 to, float size) {
		Quaternion rotation = Quaternion.LookRotation(to - from);

		Vector3 p0 = to + rotation * new Vector3( 0.5f, 0.0f,-1.5f) * size;
		Vector3 p1 = to + rotation * new Vector3(-0.5f, 0.0f,-1.5f) * size;
		Vector3 p2 = to + rotation * new Vector3( 0.0f, 0.5f,-1.5f) * size;
		Vector3 p3 = to + rotation * new Vector3( 0.0f,-0.5f,-1.5f) * size;
		Vector3 p4 = to + rotation * new Vector3( 0.0f, 0.0f,-0.5f) * size;

		Gizmos.DrawLine(from, to);

		Gizmos.DrawLine(p0, p1);
		Gizmos.DrawLine(p2, p3);

		Gizmos.DrawLine(p0, p4);
		Gizmos.DrawLine(p1, p4);
		Gizmos.DrawLine(p2, p4);
		Gizmos.DrawLine(p3, p4);
	}

	void OnDrawGizmos() {
		if(Selection.activeTransform != null) {
			if(Selection.activeTransform.parent == transform) {
				OnDrawGizmosSelected();
			}
		}
	}

	void OnDrawGizmosSelected() {
		float gizmo_scale = 0.5f;

		Gizmos.color = Util.new_color(Util.white, 0.5f);

		if(transform.childCount == 1) {
			Vector3 pos = transform.GetChild(0).position + Vector3.up * gizmo_scale * 0.5f;
			Gizmos.DrawWireCube(pos, Vector3.one * gizmo_scale);
		}
		// else if(transform.childCount == 2) {
		// 	Vector3 pos0 = transform.GetChild(0).position + Vector3.up * gizmo_scale * 0.5f;
		// 	Vector3 pos1 = transform.GetChild(1).position + Vector3.up * gizmo_scale * 0.5f;

		// 	Gizmos.DrawWireCube(pos0, Vector3.one * gizmo_scale);
		// 	Gizmos.DrawWireCube(pos1, Vector3.one * gizmo_scale);
		// 	draw_arrow_gizmo(pos0, pos1, gizmo_scale);
		// }
		else {
			for(int i = 0; i < transform.childCount; i++) {
				int next_index = i + 1;
				if(next_index >= transform.childCount) {
					next_index = 0;
				}

				Vector3 pos = transform.GetChild(i).position + Vector3.up * gizmo_scale * 0.5f;
				Vector3 next_pos = transform.GetChild(next_index).position + Vector3.up * gizmo_scale * 0.5f;

				Gizmos.DrawWireCube(pos, Vector3.one * gizmo_scale);
				draw_arrow_gizmo(pos, next_pos, gizmo_scale);
			}			
		}
	}
#endif
}