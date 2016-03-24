using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

public class MotionPathController : MonoBehaviour {
	[System.NonSerialized] public int node_count;
	[System.NonSerialized] public int node_index;

#if UNITY_EDITOR
	public static Transform hover_node;
#endif

	//TODO: Bad API!!
	public static Vector3 get_next_node(MotionPathController motion_path) {
		Transform child = motion_path.transform.GetChild(motion_path.node_index++);
		if(motion_path.node_index >= motion_path.node_count) {
			motion_path.node_index = 0;
		}

		return child.position;
	}

	//TODO: Bad API!!
	public static Vector3 get_prev_node(MotionPathController motion_path) {
		Transform child = motion_path.transform.GetChild(motion_path.node_index--);
		if(motion_path.node_index <= 0) {
			motion_path.node_index = motion_path.node_count - 1;
		}

		return child.position;
	}

	void Start() {		
		node_count = transform.childCount;
		node_index = 0;
	}

#if UNITY_EDITOR
	void draw_arrow_gizmo(Vector3 from, Vector3 to, float size) {
		Vector3 look = to - from;
		if(look != Vector3.zero) {
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
	}

	public void draw_gizmos() {
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
				Transform node = transform.GetChild(i);

				int next_index = i + 1;
				if(next_index >= transform.childCount) {
					next_index = 0;
				}
				Transform next_node = transform.GetChild(next_index);

				Vector3 pos = node.position + Vector3.up * gizmo_scale * 0.5f;
				Vector3 next_pos = next_node.position + Vector3.up * gizmo_scale * 0.5f;

				if(node == hover_node) {
					Color saved_color = Gizmos.color;
					Gizmos.color = Util.white;
					Gizmos.DrawWireCube(pos, Vector3.one * gizmo_scale);
					Gizmos.color = saved_color;
				}
				else {
					Gizmos.DrawWireCube(pos, Vector3.one * gizmo_scale);
				}

				draw_arrow_gizmo(pos, next_pos, gizmo_scale);
			}			
		}
	}

	void OnDrawGizmos() {
		if(Selection.activeTransform != null) {
			if(Selection.activeTransform.parent == transform) {
				draw_gizmos();
			}
		}
	}

	void OnDrawGizmosSelected() {
		draw_gizmos();
	}
#endif
}