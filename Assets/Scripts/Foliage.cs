
using UnityEngine;

public class Foliage {
	public Transform transform;

	public GameObject destroyed_mesh;

	public FoliageController[] entries;

	public static float CULL_DIST = 80.0f;
	public static float CULL_DIST_SQ = CULL_DIST * CULL_DIST;

	public float delay_time;

	public static Foliage new_inst(GameManager game_manager, Transform transform) {
		Foliage foliage = new Foliage();
		foliage.transform = transform;

		foliage.destroyed_mesh = transform.GetChild(0).gameObject;

		foliage.delay_time = 0.0f;

		foliage.entries = new FoliageController[transform.childCount - 1];
		for(int i = 0; i < foliage.entries.Length; i++) {
			FoliageController entry = transform.GetChild(i + 1).GetComponent<FoliageController>();
			entry.game_manager = game_manager;

			entry.anim = entry.GetComponentInChildren<Animation>();
			entry.anim_renderer = entry.anim.GetComponentInChildren<SkinnedMeshRenderer>();
			entry.static_renderer = entry.GetComponentInChildren<MeshRenderer>();

			Util.offset_first_anim(entry.anim);

			entry.transform.localRotation = Quaternion.Euler(0.0f, 360.0f * Random.value, 0.0f);
			entry.transform.localPosition += Vector3.up * Random.Range(-1.0f, 1.0f) * 0.2f;

			entry.initial_pos = entry.transform.position;

			foliage.entries[i] = entry;
		}

		return foliage;
	}

	public static void set_animated_mesh_state(FoliageController entry, bool state) {
		if(entry.anim.enabled != state) {
			entry.anim.enabled = state;
			entry.anim_renderer.enabled = state;
			entry.static_renderer.enabled = !state;
		}
	}

	public static void on_explosion(Foliage foliage, Vector3 hit_pos) {
		// foliage.delay_time = 0.9f;

		// foliage.destroyed_mesh.SetActive(true);

		for(int i = 0; i < foliage.entries.Length; i++) {
			FoliageController entry = foliage.entries[i];

			float dist = Vector3.Distance(hit_pos, entry.transform.position);
			if(dist < Environment.EXPLOSION_RADIUS) {
				entry.gameObject.SetActive(false);
			}
		}
	}

	public static void on_reset(Foliage foliage) {
		foliage.destroyed_mesh.SetActive(false);

		for(int i = 0; i < foliage.entries.Length; i++) {
			FoliageController entry = foliage.entries[i];
			entry.gameObject.SetActive(true);
			entry.transform.position = entry.initial_pos;
		}
	}

	public static void update(GameManager game_manager, Foliage foliage) {
		Camera camera_ = game_manager.player2_inst != null ? game_manager.player2_inst.camera_ : null;
		if(camera_ != null) {
			Vector3 camera_pos = camera_.transform.position;
			Plane[] frustum_planes = GeometryUtility.CalculateFrustumPlanes(camera_);

			for(int i = 0; i < foliage.entries.Length; i++) {
				FoliageController entry = foliage.entries[i];

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
			for(int i = 0; i < foliage.entries.Length; i++) {
				FoliageController entry = foliage.entries[i];
				set_animated_mesh_state(entry, false);
			}
		}

		// float new_time = foliage.delay_time - Time.deltaTime;
		// if(new_time <= 0.0f && foliage.delay_time > 0.0f) {
		// 	foliage.destroyed_mesh.SetActive(true);
		// 	for(int i = 0; i < foliage.entries.Length; i++) {
		// 		foliage.entries[i].gameObject.SetActive(false);
		// 	}

		// 	// Debug.Log(new_time);
		// }
		// foliage.delay_time = new_time;
	}
}