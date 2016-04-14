
using UnityEngine;

public class FoliageManager : MonoBehaviour {
	[System.NonSerialized] public GameManager game_manager;

	//TODO: Bucket these further if necessary!!
	[System.NonSerialized] public FoliageController[] entries;

	public static float CULL_DIST = 80.0f;
	public static float CULL_DIST_SQ = CULL_DIST * CULL_DIST;

	public static void set_animated_mesh_state(FoliageController entry, bool state) {
		if(entry.anim.enabled != state) {
			entry.anim.enabled = state;
			entry.anim_renderer.enabled = state;
			entry.static_renderer.enabled = !state;
		}
	}

	public void Awake() {
		game_manager = GameManager.get_inst();

		entries = new FoliageController[transform.childCount];
		for(int i = 0; i < entries.Length; i++) {
			FoliageController entry = transform.GetChild(i).GetComponent<FoliageController>();
			Transform entry_transform = entry.transform;

			entry.game_manager = game_manager;

			entry.anim = entry.GetComponentInChildren<Animation>();
			entry.anim_renderer = entry.anim.GetComponentInChildren<SkinnedMeshRenderer>();
			entry.static_renderer = entry.GetComponentInChildren<MeshRenderer>();

			AnimationState idle_anim = entry.anim["idle"];
			idle_anim.time = Random.Range(0.0f, idle_anim.length);

			entry_transform.localRotation = Quaternion.Euler(0.0f, 360.0f * Random.value, 0.0f);
			entry_transform.localPosition += Vector3.up * Random.Range(-1.0f, 1.0f) * 0.2f;

			entries[i] = entry;
		}
	}

	public void Update() {
		Camera camera_ = game_manager.player2_inst != null ? game_manager.player2_inst.camera_ : null;
		if(camera_ != null) {
			Vector3 camera_pos = camera_.transform.position;
			Plane[] frustum_planes = GeometryUtility.CalculateFrustumPlanes(camera_);

			for(int i = 0; i < entries.Length; i++) {
				FoliageController entry = entries[i];

				bool is_visible = false;

				float dist_sq = (camera_pos - entry.transform.position).sqrMagnitude;
				if(dist_sq < CULL_DIST_SQ) {
					//TODO: Use a cheaper test here!!
					if(GeometryUtility.TestPlanesAABB(frustum_planes, entry.anim_renderer.bounds)) {
						is_visible = true;
					}
				}

				set_animated_mesh_state(entry, is_visible);
			}
		}
		else {
			for(int i = 0; i < entries.Length; i++) {
				FoliageController entry = entries[i];
				set_animated_mesh_state(entry, false);
			}
		}
	}
}