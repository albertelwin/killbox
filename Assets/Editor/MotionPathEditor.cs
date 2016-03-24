
using UnityEditor;
using UnityEngine;

#pragma warning disable 0219

public static class MotionPathUtil {
	public static MotionPathController get_selected_motion_path() {
		MotionPathController controller = null;
		if(Selection.activeTransform) {
			controller = Selection.activeTransform.GetComponent<MotionPathController>();
			if(!controller && Selection.activeTransform.parent) {
				controller = Selection.activeTransform.parent.GetComponent<MotionPathController>();
			}
		}

		return controller;
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
							MotionPathController controller = node.parent.GetComponent<MotionPathController>();
							if(controller && node.parent.childCount > 1) {
								// Debug.Log(string.Format("[{0}]: {1}", node.GetSiblingIndex(), node.name));

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

		MotionPathController controller = MotionPathUtil.get_selected_motion_path();
		if(controller) {
			Transform hover_node = null;
			
			Ray mouse_ray = Camera.current.ScreenPointToRay(new Vector3(evt.mousePosition.x, -evt.mousePosition.y + Camera.current.pixelHeight));

			//TODO: This needs to stay in sync with the boz gizmo!!
			Vector3 min = new Vector3(-0.25f, 0.0f,-0.25f);
			Vector3 max = new Vector3( 0.25f, 0.5f, 0.25f);

			for(int i = 0; i < controller.transform.childCount; i++) {
				Transform node = controller.transform.GetChild(i);

				//TODO: Need to raycast the scene and all of the nodes to make sure this is the closest intersection!!
				if(MathExt.ray_box_intersect(node.position + min, node.position + max, mouse_ray)) {
					hover_node = node;
					break;
				}
			}

			if(MotionPathController.hover_node != hover_node) {
				MotionPathController.hover_node = hover_node;
				EditorUtility.SetDirty(controller);
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
class MotionPathInspector : Editor {
	[MenuItem("Killbox/Motion Path")]
	static void create_motion_path() {
		GameObject game_object = new GameObject("Motion Path");
		game_object.AddComponent<MotionPathController>();

		int intial_node_count = 6;
		for(int i = 0; i < intial_node_count; i++) {
			float t = (float)i / (float)intial_node_count;

			Vector3 pos = Vector3.zero;
			pos.x = Mathf.Cos(t * MathExt.TAU) * 10.0f;
			pos.z = Mathf.Sin(t * MathExt.TAU) * 10.0f;

			Transform node = Util.new_transform(game_object.transform, "Node", pos);
		}

		Undo.RegisterCreatedObjectUndo(game_object, "Create " + game_object.name);
		Selection.activeObject = game_object;
	}

	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		MotionPathController controller = (MotionPathController)target;

		if(GUILayout.Button("Add Node")) {
			Transform node = (new GameObject("Node")).transform;
			node.parent = controller.transform;
		}
	}
}
