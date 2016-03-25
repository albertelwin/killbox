using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

public class MotionPathController : MonoBehaviour {
	public float global_speed = 1.0f;

#if UNITY_EDITOR
	//TODO: There must be a better way to do this!!
	public static Transform hover_node;
#endif

	public static int get_node_count(MotionPathController path) {
		return path.transform.childCount;
	}

	public static int get_node_index(MotionPathController path, MotionPathNode node) {
		Assert.is_true(node != null);
		return node.transform.GetSiblingIndex();
	}

	public static MotionPathNode get_node(MotionPathController path, int index) {
		Assert.is_true(index < get_node_count(path));
		MotionPathNode node = path.transform.GetChild(index).GetComponent<MotionPathNode>();
		Assert.is_true(node != null);
		return node;
	}

	void Start() {
		Assert.is_true(get_node_count(this) > 0);
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