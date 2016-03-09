using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

public class TargetPointController : MonoBehaviour {
	public Transform high_value_target;

	[System.NonSerialized] public ScenarioType type;

	[System.NonSerialized] public Transform safe_points;
	[System.NonSerialized] public Transform spawn_points;

	Transform hit_point;
	//TODO: Rename this to hit_pos;
	[System.NonSerialized] public Vector3 pos;

	public static float hit_gizmo_size = 2.0f;

	void Start() {
		hit_point = transform.Find("HitPoint");
		pos = hit_point.position;

		safe_points = transform.Find("SafePoints");
		spawn_points = transform.Find("SpawnPoints");
		for(int i = 0; i < spawn_points.childCount; i++) {
			Transform dummy_player = spawn_points.GetChild(i);
			dummy_player.gameObject.SetActive(false);
		}
	}

#if UNITY_EDITOR
	static Vector3 get_gizmo_pos(Vector3 position, float size) {
		return position + Vector3.up * size * 0.5f;
	}

	static void draw_hit_gizmo(Vector3 position, float alpha) {
		Vector3 pos = get_gizmo_pos(position, hit_gizmo_size);

		Gizmos.color = Util.new_color(Util.red, alpha);
		Gizmos.DrawWireCube(pos, Vector3.one * hit_gizmo_size);
		Gizmos.DrawLine(pos, pos + Vector3.up * 100000.0f);
	}

	static void draw_child_gizmo(Vector3 position, Color color, Transform hit) {
		float gizmo_size = 1.0f;
		Vector3 pos = get_gizmo_pos(position, gizmo_size);

		Gizmos.color = color;
		Gizmos.DrawWireCube(pos, Vector3.one * gizmo_size);
		Gizmos.DrawLine(pos, get_gizmo_pos(hit.position, hit_gizmo_size));
	}

	void OnDrawGizmos() {
		Transform hit_point = transform.Find("HitPoint");
		Transform safe_points = transform.Find("SafePoints");
		Transform spawn_points = transform.Find("SpawnPoints");

		float low_opacity = 0.25f;

		float hit_point_alpha = low_opacity;
		float target_alpha = low_opacity;

		if(Selection.activeTransform != null) {
			Transform active_transform = Selection.activeTransform;

			if(active_transform == transform.parent) {
				hit_point_alpha = 1.0f;
			}
			else {
				bool parent_or_child_picked = false;
				float spawn_alpha = low_opacity;
				float safe_alpha = low_opacity;

				if(active_transform == transform || active_transform == hit_point || active_transform.parent == safe_points || active_transform.parent == spawn_points) {
					parent_or_child_picked = true;
				}
				else if(active_transform == safe_points) {
					parent_or_child_picked = true;
					safe_alpha = 1.0f;
				}
				else if(active_transform == spawn_points) {
					parent_or_child_picked = true;
					spawn_alpha = 1.0f;
				}
				else if(active_transform == high_value_target) {
					parent_or_child_picked = true;
					target_alpha = 1.0f;
				}

				if(parent_or_child_picked) {
					hit_point_alpha = 1.0f;

					for(int i = 0; i < safe_points.childCount; i++) {
						Transform child = safe_points.GetChild(i);
						float alpha = child == active_transform ? 1.0f : safe_alpha;
						draw_child_gizmo(child.position, Util.new_color(Util.green, alpha), hit_point);
					}

					for(int i = 0; i < spawn_points.childCount; i++) {
						Transform child = spawn_points.GetChild(i);
						float alpha = child == active_transform ? 1.0f : spawn_alpha;
						draw_child_gizmo(child.position, Util.new_color(Util.white, alpha), hit_point);
					}
				}
			}
		}

		if(high_value_target != null) {
			draw_child_gizmo(high_value_target.position - Vector3.up * high_value_target.localScale.y * 0.5f, Util.new_color(Util.red, target_alpha), hit_point);
		}

		draw_hit_gizmo(hit_point.position, hit_point_alpha);
	}
#endif
}