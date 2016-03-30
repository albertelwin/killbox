
using UnityEditor;
using UnityEngine;

#pragma warning disable 0219

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

	public static MotionPathNode add_node(MotionPathController path, Vector3 pos) {
		Transform transform = Util.new_transform(path.transform, "Node", pos);
		MotionPathNode node = transform.gameObject.AddComponent<MotionPathNode>();
		return node;
	}
}

[InitializeOnLoad]
public class MotionPathEditor {
	public static MotionPathEditor inst;

	Transform selected_node;
	Transform duplicated_node;

	static MotionPathEditor() {
		if(inst == null) {
			inst = new MotionPathEditor();

			SceneView.onSceneGUIDelegate += inst.OnSceneGUI;
			EditorApplication.hierarchyWindowItemOnGUI += inst.OnHierarchyWindowItem;

			// Debug.Log("MotionPathEditor()");
		}
	}

	public void process_event(Event evt) {
		if(evt != null) {
			switch(evt.type) {
				case EventType.Layout: {
					if(selected_node) {
						Selection.activeTransform = selected_node;
						selected_node = null;
					}

					if(duplicated_node) {
						MotionPathSelection selection = MotionPathUtil.get_selection();
						//NOTE: We've just duplicated a node so it should be selected!!
						Assert.is_true(selection != null && selection.type == MotionPathSelectionType.NODE);

						Transform node = selection.node.transform;
						node.SetSiblingIndex(duplicated_node.GetSiblingIndex() + 1);

						duplicated_node = null;
					}

					break;
				}

				case EventType.ValidateCommand: {

					break;
				}

				case EventType.ExecuteCommand: {
					if(evt.commandName == "SoftDelete") {
						if(!selected_node) {
							MotionPathSelection selection = MotionPathUtil.get_selection();
							if(selection != null && selection.type == MotionPathSelectionType.NODE) {
								Transform path = selection.path.transform;
								Transform node = selection.node.transform;

								if(path.childCount > 1) {
									int prev_index = node.GetSiblingIndex() - 1;
									if(prev_index < 0) {
										prev_index = path.childCount - 1;
									}

									selected_node = path.GetChild(prev_index);
								}
							}
						}
					}
					else if(evt.commandName == "Duplicate") {
						if(!duplicated_node) {
							MotionPathSelection selection = MotionPathUtil.get_selection();
							if(selection != null && selection.type == MotionPathSelectionType.NODE) {
								duplicated_node = Selection.activeTransform;
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

		MotionPathSelection selection = MotionPathUtil.get_selection();
		if(selection != null) {
			MotionPathController path = selection.path;

			Transform hover_node = null;
			
			Ray mouse_ray = Camera.current.ScreenPointToRay(new Vector3(evt.mousePosition.x, -evt.mousePosition.y + Camera.current.pixelHeight));

			float min_t = Mathf.Infinity;

			RaycastHit hit_info;
			if(Physics.Raycast(mouse_ray, out hit_info)) {
				min_t = hit_info.distance;
			}

			//TODO: This needs to stay in sync with the boz gizmo!!
			Vector3 min = new Vector3(-0.25f, 0.0f,-0.25f);
			Vector3 max = new Vector3( 0.25f, 0.5f, 0.25f);

			for(int i = 0; i < path.transform.childCount; i++) {
				Transform node = path.transform.GetChild(i);

				float t;
				if(MathExt.ray_box_intersect(node.position + min, node.position + max, mouse_ray, out t)) {
					if(t < min_t) {
						hover_node = node;
						min_t = t;
					}
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
[CanEditMultipleObjects]
public class MotionPathControllerInspector : Editor {
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
		global_speed = serializedObject.FindProperty("global_speed");
	}

	public override void OnInspectorGUI() {
		serializedObject.Update();

		EditorGUILayout.LabelField("Motion Path Controller", EditorStyles.miniLabel);

		MotionPathController path = (MotionPathController)target;

		EditorGUILayout.PropertyField(global_speed, new GUIContent("Global Speed"));

		EditorGUILayout.Separator();

		if(GUILayout.Button("View Linked NPCs")) {
			NpcController[] npcs = (NpcController[])Object.FindObjectsOfType(typeof(NpcController));
			for(int i = 0; i < npcs.Length; i++) {
				NpcController npc = npcs[i];
				if(npc.motion_path == path) {
					EditorGUIUtility.PingObject(npc);
				}
			}
		}

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

		GameObject prefab = (GameObject)PrefabUtility.GetPrefabParent(path.gameObject);
		if(prefab) {
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Prefab Options");

			// if(EditorApplication.isPlaying) {
				if(GUILayout.Button("Save")) {
					prefab = PrefabUtility.ReplacePrefab(path.gameObject, prefab);
				}
			// }
		}

		serializedObject.ApplyModifiedProperties();
	}
}

[CustomEditor(typeof(MotionPathNode))]
[CanEditMultipleObjects]
public class MotionPathNodeInspector : Editor {
	public SerializedProperty override_speed;
	public SerializedProperty speed;

	public SerializedProperty flip_direction;

	public SerializedProperty stop;
	public SerializedProperty stop_forever;
	public SerializedProperty stop_time;

	public void OnEnable() {
		override_speed = serializedObject.FindProperty("override_speed");
		speed = serializedObject.FindProperty("speed");

		flip_direction = serializedObject.FindProperty("flip_direction");

		stop = serializedObject.FindProperty("stop");
		stop_forever = serializedObject.FindProperty("stop_forever");
		stop_time = serializedObject.FindProperty("stop_time");
	}

	public override void OnInspectorGUI() {
		serializedObject.Update();

		EditorGUILayout.LabelField("Motion Path Node", EditorStyles.miniLabel);

		EditorGUILayout.PropertyField(override_speed, new GUIContent("Override Speed"));
		if(override_speed.boolValue) {
			EditorGUILayout.PropertyField(speed, new GUIContent("  Speed"));
		}

		EditorGUILayout.PropertyField(flip_direction, new GUIContent("Flip Direction"));

		EditorGUILayout.PropertyField(stop, new GUIContent("Stop"));
		if(stop.boolValue) {
			EditorGUILayout.PropertyField(stop_forever, new GUIContent("  Forever"));
			if(!stop_forever.boolValue) {
				EditorGUILayout.PropertyField(stop_time, new GUIContent("  Time"));
			}
		}

		serializedObject.ApplyModifiedProperties();
	}
}
