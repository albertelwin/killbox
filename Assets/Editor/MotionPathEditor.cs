
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
		Transform transform = Util.new_transform(path.transform, "Node", pos);
		MotionPathNode node = transform.gameObject.AddComponent<MotionPathNode>();
		return node;
	}

	public static void show_prefab_options(MotionPathController path) {
		GameObject prefab = (GameObject)PrefabUtility.GetPrefabParent(path.gameObject);

		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Prefab Options");

		GUILayout.BeginHorizontal();

		if(prefab == null) {
			GUI.enabled = false;
		}

		if(GUILayout.Button("View")) {
			EditorGUIUtility.PingObject(prefab);
		}

		GUI.enabled = true;

		bool can_save = false;
		if(prefab) {
			PropertyModification[] changes = PrefabUtility.GetPropertyModifications(path.gameObject);
			int ignored_change_count = 8;

			if(changes != null && changes.Length > ignored_change_count) {
				can_save = true;

				// for(int change_index = ignored_change_count; change_index < changes.Length; change_index++) {
				// 	PropertyModification change = changes[change_index];
				// 	Debug.Log(change.propertyPath);
				// }
			}

			if(!can_save) {
				for(int child_index = 0; child_index < path.transform.childCount; child_index++) {
					Transform child = path.transform.GetChild(child_index);

					PropertyModification[] child_changes = PrefabUtility.GetPropertyModifications(child.gameObject);
					if(child_changes == null) {
						can_save = true;
						break;
					}
				}
			}
		}
		else {
			can_save = true;
		}

		if(!can_save) {
			GUI.enabled = false;
		}

		if(GUILayout.Button("Save")) {
			if(prefab) {
				prefab = PrefabUtility.ReplacePrefab(path.gameObject, prefab, ReplacePrefabOptions.ConnectToPrefab);
			}
			else {
				string folder = "Assets/Prefabs/Motion Paths";

				//TODO: This could potientially get very slow!!
				string prefab_name = path.name;
				int suffix = 0;
				while(true) {
					string guid = AssetDatabase.AssetPathToGUID(string.Format("{0}/{1}.prefab", folder, prefab_name));
					if(guid == "") {
						break;
					}
					else {
						suffix++;
						prefab_name = path.name + suffix;
					}
				}

				path.name = prefab_name;
				prefab = PrefabUtility.CreatePrefab(string.Format("{0}/{1}.prefab", folder, prefab_name), path.gameObject, ReplacePrefabOptions.ConnectToPrefab);
			}
		}

		GUI.enabled = true;

		GUILayout.EndHorizontal();
	}
}

[InitializeOnLoad]
public class MotionPathEditor {
	public static MotionPathEditor inst;

	Transform selected_node;
	Transform duplicated_node;
	Transform modified_path;

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

					if(modified_path) {
						Assert.is_true(!EditorApplication.isPlaying);

						//TODO: Do we always want to overwrite the prefab on delete??
						GameObject prefab = (GameObject)PrefabUtility.GetPrefabParent(modified_path.gameObject);
						Assert.is_true(prefab != null);
						prefab = PrefabUtility.ReplacePrefab(modified_path.gameObject, prefab, ReplacePrefabOptions.ConnectToPrefab);

						modified_path = null;
					}

					break;
				}

				case EventType.ValidateCommand: {
					if(evt.commandName == "SoftDelete") {
						MotionPathSelection selection = MotionPathUtil.get_selection();
						if(selection != null && selection.type == MotionPathSelectionType.NODE) {
							GameObject prefab = (GameObject)PrefabUtility.GetPrefabParent(selection.path.gameObject);
							if(prefab) {
								PrefabUtility.DisconnectPrefabInstance(selection.path.gameObject);
							}
						}
					}
					else if(evt.commandName == "UndoRedoPerformed") {
						//TODO: Is there any way to know what was undone/redone??
					}

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

								if(!EditorApplication.isPlaying) {
									//TODO: Nodes being deleted from multiple paths
									GameObject prefab = (GameObject)PrefabUtility.GetPrefabParent(selection.path.gameObject);
									if(prefab) {
										modified_path = selection.path.transform;
									}								
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

	public Rect links_rect;

	[MenuItem("Killbox/Motion Path")]
	public static void create_motion_path() {
		GameObject game_object = new GameObject("MotionPath");
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

		MotionPathController path = (MotionPathController)target;

		EditorGUILayout.LabelField("Motion Path Controller", EditorStyles.miniLabel);

		EditorGUILayout.PropertyField(global_speed, new GUIContent("Global Speed"));

		EditorGUILayout.Separator();

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

		if(GUILayout.Button("Add Node")) {
			MotionPathNode node = MotionPathUtil.add_node(path, Vector3.zero);
			Selection.activeTransform = node.transform;
		}

		MotionPathUtil.show_prefab_options(path);

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
		}

		MotionPathController path = node.transform.parent != null ? node.transform.parent.GetComponent<MotionPathController>() : null;
		Assert.is_true(path != null);
		MotionPathUtil.show_prefab_options(path);

		serializedObject.ApplyModifiedProperties();
	}
}
