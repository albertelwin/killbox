
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

public class MotionPathAgent {
	public Transform transform;
	public NavMeshAgent nav;

	public MotionPathController path;
	public MotionPathNode node;
	public MotionPathNode prev_node;
	public int saved_node_index;

	public Vector3 target_pos;
	public bool reversed;
	public float stop_time;

	public bool entered_player_radius;
	public bool in_player_radius;

	public float walk_speed;
	public float walk_accel;
	public float run_speed;
	public float run_accel;
}

public class MotionPath {
	public static int get_node_count(MotionPathController path) {
		return path.transform.childCount;
	}

	public static int reverse_index(MotionPathController path, int index) {
		int node_count = get_node_count(path);
		Assert.is_true(index < node_count);
		return index == 0 ? 0 : node_count - index;
	}

	public static int get_node_index(MotionPathController path, MotionPathNode node, bool reverse) {
		Assert.is_true(node != null);

		int index = node.transform.GetSiblingIndex();
		if(reverse) {
			index = reverse_index(path, index);
		}

		return index;
	}

	public static int wrap_node_index(MotionPathController path, int node_index) {
		if(node_index >= get_node_count(path)) {
			node_index = 0;
		}

		return node_index;
	}

	public static MotionPathNode get_node(MotionPathController path, int index, bool reverse) {
		Assert.is_true(index < MotionPath.get_node_count(path));

		int child_index = index;
		if(reverse) {
			child_index = reverse_index(path, index);
		}

		MotionPathNode node = path.transform.GetChild(child_index).GetComponent<MotionPathNode>();
		Assert.is_true(node != null);
		return node;
	}

	public static MotionPathAgent new_agent(Transform transform, NavMeshAgent nav, MotionPathController path) {
		MotionPathAgent agent = new MotionPathAgent();
		agent.transform = transform;
		agent.nav = nav;

		agent.path = path;

		agent.walk_speed = 2.0f;
		agent.walk_accel = 8.0f;
		agent.run_speed = 12.0f;
		agent.run_accel = 48.0f;

		return agent;
	}

	public static void set_agent_next_node(MotionPathAgent agent, bool reverse) {
		int node_index = MotionPath.get_node_index(agent.path, agent.node, agent.reversed);

		if(reverse) {
			if(node_index == 0) {
				node_index = MotionPath.get_node_count(agent.path);
			}
			node_index--;
		}
		else {
			node_index = MotionPath.wrap_node_index(agent.path, node_index + 1);
		}

		agent.prev_node = agent.node;
		agent.node = MotionPath.get_node(agent.path, node_index, agent.reversed);

		if(reverse) {
			agent.reversed = !agent.reversed;
		}

		agent.saved_node_index = MotionPath.wrap_node_index(agent.path, node_index + 1);
	}

	public static void move_agent(MotionPathAgent agent, float dt, Transform player2, bool first_hit) {
		agent.entered_player_radius = false;
		agent.stop_time -= dt;
		if(agent.stop_time < 0.0f) {
			agent.stop_time = 0.0f;
		}

		if(agent.nav.enabled && !first_hit) {
			if(!agent.node) {
				agent.saved_node_index = MotionPath.wrap_node_index(agent.path, agent.saved_node_index);
				agent.node = MotionPath.get_node(agent.path, agent.saved_node_index, agent.reversed);
			}
			else {
				bool should_reverse = agent.path.reverse_now;
				agent.path.reverse_now = false;

				if(player2 != null && Vector3.Distance(agent.transform.position, player2.position) <= 2.5f) {
					if(!agent.in_player_radius) {
						agent.entered_player_radius = true;
						should_reverse = true;
					}

					agent.in_player_radius = true;
					//TODO: Change NPC speed when tigged!!
					// agent.nav.speed = run_speed;
					// agent.nav.acceleration = run_accel;
				}
				else {
					agent.in_player_radius = false;
				}

				//TODO: Calculate this from velocity/distance/etc.!!
				float min_dist = Mathf.Max(agent.nav.speed * 0.5f, 1.0f);
				if(agent.nav.remainingDistance <= min_dist) {
					if(agent.node.stop) {
						agent.stop_time = agent.node.stop_time;
					}

					MotionPath.set_agent_next_node(agent, agent.node.flip_direction);
				}
				else if(should_reverse) {
					MotionPath.set_agent_next_node(agent, true);
				}
			}

			Assert.is_true(agent.node != null);

			float speed_modifier = agent.path.global_speed;
			if(agent.prev_node) {
				if(agent.prev_node.override_speed) {
					speed_modifier = agent.prev_node.speed;
				}

				if(!agent.prev_node.stop) {
					agent.stop_time = 0.0f;
				}
				else {
					if(agent.prev_node.stop_forever || agent.stop_time > 0.0f) {
						speed_modifier = 0.0f;
					}					
				}
			}

			if(agent.target_pos != agent.node.transform.position) {
				agent.target_pos = agent.node.transform.position;
				agent.nav.SetDestination(agent.target_pos);
			}

			if(agent.in_player_radius) {
				agent.nav.speed = agent.run_speed * speed_modifier;
				agent.nav.acceleration = agent.run_accel;
			}
			else {
				agent.nav.speed = agent.walk_speed * speed_modifier;
				agent.nav.acceleration = agent.walk_accel;
			}
		}
	}
}

public class MotionPathController : MonoBehaviour {
	public float global_speed = 1.0f;

#if UNITY_EDITOR
	//TODO: There must be a better way to do this!!
	public static Transform hover_node;
#endif

	[System.NonSerialized] public bool reverse_now;

	void Start() {
		int node_count = MotionPath.get_node_count(this);
		Assert.is_true(node_count > 0);
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