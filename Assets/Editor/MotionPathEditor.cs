
using UnityEditor;
using UnityEngine;

#pragma warning disable 0219

public static class MotionPathUtil {
	public static MotionPathController get_selected_motion_path() {
		MotionPathController path = null;
		if(Selection.activeTransform) {
			path = Selection.activeTransform.GetComponent<MotionPathController>();
			if(!path && Selection.activeTransform.parent) {
				path = Selection.activeTransform.parent.GetComponent<MotionPathController>();
			}
		}

		return path;
	}

	public static MotionPathNode add_node(MotionPathController path, Vector3 pos) {
		Transform transform = Util.new_transform(path.transform, "Node", pos);
		MotionPathNode node = transform.gameObject.AddComponent<MotionPathNode>();
		return node;
	}
}

[InitializeOnLoad]
public class MotionPathEditor {
	public static MotionPathEditor inst;

	Transform next_active_transform;

	static MotionPathEditor() {
		if(inst == null) {
			inst = new MotionPathEditor();

			SceneView.onSceneGUIDelegate += inst.OnSceneGUI;
			EditorApplication.hierarchyWindowItemOnGUI += inst.OnHierarchyWindowItem;

			Debug.Log("MotionPathEditor()");
		}
	}

	public void process_event(Event evt) {
		if(evt != null) {
			switch(evt.type) {
				case EventType.Layout: {
					if(next_active_transform) {
						Selection.activeTransform = next_active_transform;
						next_active_transform = null;
					}

					break;
				}

				case EventType.ValidateCommand: {

					break;
				}

				case EventType.ExecuteCommand: {
					if(evt.commandName == "SoftDelete") {
						if(!next_active_transform && Selection.activeTransform && Selection.activeTransform.parent) {
							Transform node = Selection.activeTransform;
							MotionPathController path = node.parent.GetComponent<MotionPathController>();
							if(path && node.parent.childCount > 1) {
								int prev_index = node.GetSiblingIndex() - 1;
								if(prev_index < 0) {
									prev_index = node.parent.childCount - 1;
								}

								next_active_transform = node.parent.GetChild(prev_index);
							}
						}
					}

					break;
				}

				default: {
					break;
				}
			}
		}
	}

	public void OnSceneGUI(SceneView scene_view) {
		Event evt = Event.current;
		process_event(evt);

		MotionPathController path = MotionPathUtil.get_selected_motion_path();
		if(path) {
			Transform hover_node = null;
			
			Ray mouse_ray = Camera.current.ScreenPointToRay(new Vector3(evt.mousePosition.x, -evt.mousePosition.y + Camera.current.pixelHeight));

			//TODO: This needs to stay in sync with the boz gizmo!!
			Vector3 min = new Vector3(-0.25f, 0.0f,-0.25f);
			Vector3 max = new Vector3( 0.25f, 0.5f, 0.25f);

			for(int i = 0; i < path.transform.childCount; i++) {
				Transform node = path.transform.GetChild(i);

				//TODO: Need to raycast the scene and all of the nodes to make sure this is the closest intersection!!
				if(MathExt.ray_box_intersect(node.position + min, node.position + max, mouse_ray)) {
					hover_node = node;
					break;
				}
			}

			if(MotionPathController.hover_node != hover_node) {
				MotionPathController.hover_node = hover_node;
				EditorUtility.SetDirty(path);
			}

			if(MotionPathController.hover_node) {
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

				if(evt.type == EventType.MouseUp) {
					Selection.activeTransform = MotionPathController.hover_node;
				}
			}
		}
	}

	public void OnHierarchyWindowItem(int inst_id, Rect selection_rect) {
		process_event(Event.current);
	}
}

[CustomEditor(typeof(MotionPathController))]
public class MotionPathControllerInspector : Editor {
	public SerializedObject serializer;

	public SerializedProperty global_speed;

	[MenuItem("Killbox/Motion Path")]
	public static void create_motion_path() {
		GameObject game_object = new GameObject("Motion Path");
		MotionPathController path = game_object.AddComponent<MotionPathController>();

		int intial_node_count = 6;
		for(int i = 0; i < intial_node_count; i++) {
			float t = (float)i / (float)intial_node_count;

			Vector3 pos = Vector3.zero;
			pos.x = Mathf.Cos(t * MathExt.TAU) * 10.0f;
			pos.z = Mathf.Sin(t * MathExt.TAU) * 10.0f;

			MotionPathUtil.add_node(path, pos);
		}

		Undo.RegisterCreatedObjectUndo(game_object, "Create " + game_object.name);
		Selection.activeObject = game_object;
	}

	public void OnEnable() {
		serializer = new SerializedObject(target);

		global_speed = serializer.FindProperty("global_speed");
	}

	public override void OnInspectorGUI() {
		serializer.Update();

		EditorGUILayout.LabelField("Motion Path Controller", EditorStyles.miniLabel);

		MotionPathController path = (MotionPathController)target;

		EditorGUILayout.PropertyField(global_speed, new GUIContent("Global Speed"));

		if(GUILayout.Button("Add Node")) {
			MotionPathNode node = MotionPathUtil.add_node(path, Vector3.zero);
			Selection.activeTransform = node.transform;
		}

		//TODO: Temp!!
		if(EditorApplication.isPlaying) {
			if(GUILayout.Button("Reverse")) {
				path.reverse_now = true;
			}
		}

		serializer.ApplyModifiedProperties();
	}
}

[CustomEditor(typeof(MotionPathNode))]
public class MotionPathNodeInspector : Editor {
	public SerializedObject serializer;

	public SerializedProperty override_speed;
	public SerializedProperty speed;

	public SerializedProperty flip_direction;

	public SerializedProperty has_event;
	public SerializedProperty evt_type;
	public SerializedProperty evt_trigger;

	public void OnEnable() {
		serializer = new SerializedObject(target);

		//TODO: Can we do this automatically??
		override_speed = serializer.FindProperty("override_speed");
		speed = serializer.FindProperty("speed");

		flip_direction = serializer.FindProperty("flip_direction");

		has_event = serializer.FindProperty("has_event");
		evt_type = serializer.FindProperty("evt.type");
		evt_trigger = serializer.FindProperty("evt.trigger");
	}

	public override void OnInspectorGUI() {
		serializer.Update();

		EditorGUILayout.LabelField("Motion Path Node", EditorStyles.miniLabel);

		EditorGUILayout.PropertyField(override_speed, new GUIContent("Override Speed"));
		if(override_speed.boolValue) {
			EditorGUILayout.PropertyField(speed, new GUIContent("  Speed"));
		}

		EditorGUILayout.PropertyField(flip_direction, new GUIContent("Flip Direction"));

		EditorGUILayout.PropertyField(has_event, new GUIContent("Event"));
		if(has_event.boolValue) {
			EditorGUILayout.PropertyField(evt_type, new GUIContent("  Type"));
			EditorGUILayout.PropertyField(evt_trigger, new GUIContent("  Trigger"));
		}

		serializer.ApplyModifiedProperties();
	}
}
