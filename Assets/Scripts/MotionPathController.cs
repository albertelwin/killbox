
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

public enum MotionPathAnimationType {
	IDLE,
	MOVING,
	ACTION,
}

public class MotionPathAgent {
	public Transform transform;
	public NavMeshAgent nav;

	public MotionPathController path;
	public MotionPathNode node;
	public MotionPathNode prev_node;
	public int saved_node_index;
	public float node_time;

	public Vector3 target_pos;
	public bool reversed;
	public float stop_time;
	public float run_time;

	public bool stopped;
	public bool started;
}

public enum MotionPathSelectionType {
	PATH,
	NODE,
}

public class MotionPathSelection {
	public MotionPathSelectionType type;

	public MotionPathController path;
	public MotionPathNode node;
}

public static class MotionPathUtil {
#if UNITY_EDITOR
	public static MotionPathSelection get_selection() {
		MotionPathSelection selection = null;

		if(Selection.activeTransform) {
			MotionPathController path = Selection.activeTransform.GetComponent<MotionPathController>();
			if(path) {
				selection = new MotionPathSelection();
				selection.type = MotionPathSelectionType.PATH;
				selection.path = path;
			}
			else if(Selection.activeTransform.parent) {
				path = Selection.activeTransform.parent.GetComponent<MotionPathController>();
				if(path) {
					selection = new MotionPathSelection();
					selection.type = MotionPathSelectionType.NODE;
					selection.path = path;
					selection.node = Selection.activeTransform.GetComponent<MotionPathNode>();
				}
			}
		}

		return selection;
	}
#endif

	public static MotionPathNode add_node(MotionPathController path, Vector3 pos) {
		Transform transform = Util.new_transform(path.transform, "Node");
		transform.position = pos;
		return transform.gameObject.AddComponent<MotionPathNode>();
	}

	public static float get_node_gizmo_size(Transform node, bool path_selected) {
		float size = 0.5f;
		if(Camera.current != null && path_selected) {
			float dist = Vector3.Distance(node.position, Camera.current.transform.position);
			float adjusted_size = dist * 0.025f;
			if(adjusted_size > size) {
				size = adjusted_size;
			}
		}

		return size;
	}
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

	public static Vector3 get_start_pos(MotionPathController path) {
		Vector3 pos = Vector3.zero;
		if(path.start_node != null) {
			Assert.is_true(path.start_node.transform.parent == path.transform);
			pos = path.start_node.transform.position;
		}
		else {

		}

		return pos;
	}

	public static MotionPathAgent new_agent(Transform transform, NavMeshAgent nav, MotionPathController path) {
		MotionPathAgent agent = new MotionPathAgent();
		agent.transform = transform;
		agent.nav = nav;

		agent.path = path;

		MotionPathNode start_node = null;
		if(path.start_node) {
			Assert.is_true(path.start_node.transform.parent == path.transform);
			agent.saved_node_index = get_node_index(path, path.start_node, false);
			start_node = path.start_node;
		}
		else {
			start_node = get_node(path, 0, false);
		}

		agent.nav.Warp(start_node.transform.position);
		agent.nav.SetDestination(start_node.transform.position);

		return agent;
	}

	public static void move_agent(MotionPathAgent agent, float dt, bool run) {
		Assert.is_true(agent.nav.enabled);

		agent.stopped = false;
		agent.started = false;

		float new_node_time = agent.node_time + dt;
		if(agent.node_time < agent.stop_time && new_node_time >= agent.stop_time) {
			agent.started = true;
		}

		agent.node_time = new_node_time;

		agent.run_time -= dt;
		if(agent.run_time < 0.0f) {
			agent.run_time = 0.0f;
		}
		if(run) {
			agent.run_time = 3.0f;
			agent.stop_time = 0.0f;
			agent.started = true;
		}

		if(!agent.node) {
			agent.saved_node_index = MotionPath.wrap_node_index(agent.path, agent.saved_node_index);
			agent.node = MotionPath.get_node(agent.path, agent.saved_node_index, agent.reversed);
			agent.node_time = 0.0f;
			agent.stop_time = 0.0f;
			agent.started = true;
		}
		else {
			//TODO: Calculate this from velocity/distance/etc.!!
			float min_dist = Mathf.Max(agent.nav.speed * 0.5f, 1.0f);
			if(agent.nav.remainingDistance <= min_dist && agent.node_time >= agent.stop_time) {
				agent.stop_time = 0.0f;
				if(agent.node.stop && agent.node.stop_time > 0.0f && agent.run_time == 0.0f) {
					agent.stop_time = agent.node.stop_time;
					agent.stopped = true;
				}

				bool flip_direction = agent.node.flip_direction;

				int node_index = MotionPath.get_node_index(agent.path, agent.node, agent.reversed);
				if(flip_direction) {
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
				agent.node_time = 0.0f;
				if(flip_direction) {
					agent.reversed = !agent.reversed;
				}

				agent.saved_node_index = MotionPath.wrap_node_index(agent.path, node_index + 1);
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
		}
		else {
			agent.stop_time = 0.0f;
		}

		if(agent.node_time < agent.stop_time) {
			speed_modifier = 0.0f;
		}

		if(agent.target_pos != agent.node.transform.position) {
			agent.target_pos = agent.node.transform.position;
			agent.nav.SetDestination(agent.target_pos);
		}

		float walk_speed = 2.0f;
		float walk_accel = 8.0f;
		float run_speed = 12.0f;
		float run_accel = 48.0f;

		if(agent.run_time > 0.0f) {
			agent.nav.speed = run_speed * speed_modifier;
			agent.nav.acceleration = run_accel;
		}
		else {
			agent.nav.speed = walk_speed * speed_modifier;
			agent.nav.acceleration = walk_accel;
		}
	}
}

public class MotionPathController : MonoBehaviour {
	public float global_speed = 1.0f;
	public MotionPathNode start_node = null;

#if UNITY_EDITOR
	//TODO: There must be a better way to do this!!
	public static Transform hover_node;
#endif

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

	void OnDrawGizmos() {
		bool selected = false;
		if(Selection.activeTransform != null) {
			if(Selection.activeTransform == transform || Selection.activeTransform.parent == transform) {
				selected = true;
			}
		}

		float alpha = selected ? 0.5f : 0.05f;
		Gizmos.color = Util.new_color(Util.white, alpha);

		if(transform.childCount == 1) {
			Transform node = transform.GetChild(0);
			float size = MotionPathUtil.get_node_gizmo_size(node, selected);
			Vector3 pos = node.position + Vector3.up * size * 0.5f;
			Gizmos.DrawWireCube(pos, Vector3.one * size);
		}
		else {
			Transform prev_node = transform.GetChild(transform.childCount - 1);

			for(int i = 0; i < transform.childCount; i++) {
				Transform node = transform.GetChild(i);
				float size = MotionPathUtil.get_node_gizmo_size(node, selected);
				Vector3 pos = node.position + Vector3.up * size * 0.5f;

				float prev_size = MotionPathUtil.get_node_gizmo_size(prev_node, selected);
				Vector3 prev_pos = prev_node.position + Vector3.up * prev_size * 0.5f;

				if(node == hover_node) {
					Color saved_color = Gizmos.color;
					Gizmos.color = Util.red;
					Gizmos.DrawWireCube(pos, Vector3.one * size);
					Gizmos.color = saved_color;
				}
				else {
					Gizmos.DrawWireCube(pos, Vector3.one * size);
				}

				draw_arrow_gizmo(prev_pos, pos, size);

				prev_node = node;
			}
		}
	}
#endif
}