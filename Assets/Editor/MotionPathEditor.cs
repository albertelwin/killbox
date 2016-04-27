
using UnityEditor;
using UnityEngine;

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
		Transform transform = Util.new_transform(path.transform, "Node");
		transform.position = pos;
		return transform.gameObject.AddComponent<MotionPathNode>();
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

				if(evt.type == EventType.MouseUp && evt.button == 0 && evt.modifiers == EventModifiers.None) {
					Selection.activeTransform = MotionPathController.hover_node;
					evt.Use();
				}
			}

			if(evt.type == EventType.MouseUp && evt.button == 1 && evt.modifiers == EventModifiers.None) {
				if(min_t == hit_info.distance) {
					Vector3 pos = mouse_ray.GetPoint(hit_info.distance);
					MotionPathNode node = MotionPathUtil.add_node(path, pos);

					if(selection.type == MotionPathSelectionType.NODE) {
						duplicated_node = selection.node.transform;
					}

					Undo.RegisterCreatedObjectUndo(node.gameObject, "Create " + node.name);
					Selection.activeTransform = node.transform;
					// evt.Use();
				}
			}

			if(selection.type == MotionPathSelectionType.NODE) {
				if(evt.type == EventType.KeyDown) {
					int node_index = -1;

					if(evt.keyCode == KeyCode.W) {
						node_index = selection.node.transform.GetSiblingIndex() + 1;
						if(node_index >= selection.path.transform.childCount) {
							node_index = 0;
						}
					}
					else if(evt.keyCode == KeyCode.S) {
						node_index = selection.node.transform.GetSiblingIndex() - 1;
						if(node_index < 0) {
							node_index = selection.path.transform.childCount - 1;
						}
					}

					if(node_index > -1) {
						Selection.activeTransform = selection.path.transform.GetChild(node_index);
						evt.Use();
					}
				}
			}
		}
	}

	public void OnHierarchyWindowItem(int inst_id, Rect selection_rect) {
		process_event(Event.current);
	}
}

public class MotionPathLinksViewer : PopupWindowContent {
	public Transform[] links;

	public override Vector2 GetWindowSize() {
		return new Vector2(200, 18 * links.Length + 2);
	}

	public override void OnGUI(Rect rect) {
		for(int i = 0; i < links.Length; i++) {
			EditorGUILayout.ObjectField(links[i], null, false);
		}
	}
}

[CustomEditor(typeof(MotionPathController))]
[CanEditMultipleObjects]
public class MotionPathControllerInspector : Editor {
	public SerializedProperty global_speed;
	public SerializedProperty start_node;

	public Rect links_rect;

	[MenuItem("Killbox/Motion Path")]
	public static void create_motion_path() {
		GameObject game_object = new GameObject("MotionPath");
		MotionPathController path = game_object.AddComponent<MotionPathController>();

		path.start_node = MotionPathUtil.add_node(path, Vector3.zero);
		path.start_node.transform.localPosition = Vector3.zero;

		Undo.RegisterCreatedObjectUndo(game_object, "Create " + game_object.name);
		Selection.activeObject = game_object;
	}

	public void OnEnable() {
		global_speed = serializedObject.FindProperty("global_speed");
		start_node = serializedObject.FindProperty("start_node");
	}

	public override void OnInspectorGUI() {
		serializedObject.Update();

		MotionPathController path = (MotionPathController)target;

		EditorGUILayout.LabelField("Motion Path Controller", EditorStyles.miniLabel);

		EditorGUILayout.PropertyField(global_speed, new GUIContent("Global Speed"));

		EditorGUILayout.Separator();

		if(!path.start_node) {
			GUI.enabled = false;
		}

		if(GUILayout.Button("View Start Node")) {
			if(path.start_node) {
				EditorGUIUtility.PingObject(path.start_node);
			}
		}

		GUI.enabled = true;

		int link_count = 0;
		NpcController[] npcs = (NpcController[])Object.FindObjectsOfType(typeof(NpcController));
		for(int i = 0; i < npcs.Length; i++) {
			NpcController npc = npcs[i];
			if(npc.motion_path == path) {
				link_count++;
			}
		}

		if(link_count == 0) {
			GUI.enabled = false;
		}

		if(GUILayout.Button("View Linked NPCs")) {
			if(link_count > 0) {
				Transform[] links = new Transform[link_count];
				for(int i = 0, link_index = 0; i < npcs.Length; i++) {
					NpcController npc = npcs[i];
					if(npc.motion_path == path) {
						Assert.is_true(link_index < link_count);
						links[link_index++] = npc.transform;
					}
				}

				if(link_count == 1) {
					EditorGUIUtility.PingObject(links[0]);
				}
				else {
					MotionPathLinksViewer links_viewer = new MotionPathLinksViewer();
					links_viewer.links = links;
					PopupWindow.Show(links_rect, links_viewer);
				}
			}
		}

		GUI.enabled = true;

		if(Event.current.type == EventType.Repaint) {
			links_rect = GUILayoutUtility.GetLastRect();
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
	public SerializedProperty stop_animation;

	public void OnEnable() {
		override_speed = serializedObject.FindProperty("override_speed");
		speed = serializedObject.FindProperty("speed");

		flip_direction = serializedObject.FindProperty("flip_direction");

		stop = serializedObject.FindProperty("stop");
		stop_forever = serializedObject.FindProperty("stop_forever");
		stop_time = serializedObject.FindProperty("stop_time");
		stop_animation = serializedObject.FindProperty("stop_animation");
	}

	public override void OnInspectorGUI() {
		serializedObject.Update();

		MotionPathNode node = (MotionPathNode)target;

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

			EditorGUILayout.PropertyField(stop_animation, new GUIContent("  Animation"));
		}

		MotionPathController path = node.transform.parent != null ? node.transform.parent.GetComponent<MotionPathController>() : null;
		Assert.is_true(path != null);

		if(path.start_node == node) {
			GUI.enabled = false;
		}

		if(GUILayout.Button("Set As Start Node")) {
			path.start_node = node;
		}

		GUI.enabled = true;

		serializedObject.ApplyModifiedProperties();
	}
}
