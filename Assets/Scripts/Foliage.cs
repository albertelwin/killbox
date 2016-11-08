
using UnityEngine;

public class Foliage {
	public Transform transform;

	public FoliageController[] entries;

	public static float CULL_DIST = 80.0f;
	public static float CULL_DIST_SQ = CULL_DIST * CULL_DIST;

	public bool hit;
	public float hit_time;

	public static Foliage new_inst(GameManager game_manager, Transform transform) {
		Foliage foliage = new Foliage();
		foliage.transform = transform;

		foliage.hit = false;
		foliage.hit_time = 0.0f;

		foliage.entries = new FoliageController[transform.childCount];
		for(int i = 0; i < foliage.entries.Length; i++) {
			FoliageController entry = transform.GetChild(i).GetComponent<FoliageController>();
			entry.game_manager = game_manager;

			entry.anim = entry.GetComponentInChildren<Animation>();
			entry.anim_renderer = entry.anim.GetComponentInChildren<SkinnedMeshRenderer>();
			entry.static_renderer = entry.GetComponentInChildren<MeshRenderer>();

			Util.offset_first_anim(entry.anim);

			entry.transform.localRotation = Quaternion.Euler(0.0f, 360.0f * Random.value, 0.0f);
			entry.transform.localPosition += Vector3.up * Random.Range(-1.0f, 1.0f) * 0.2f;

			entry.initial_pos = entry.transform.position;
			entry.initial_rot = entry.transform.rotation;

			entry.rnd = Random.value;

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
		if(!foliage.hit) {
			foliage.hit = true;
			foliage.hit_time = 0.0f;
		}

		for(int i = 0; i < foliage.entries.Length; i++) {
			FoliageController entry = foliage.entries[i];

			float dist = Vector3.Distance(hit_pos, entry.transform.position);
			if(dist < Environment.EXPLOSION_RADIUS) {
				entry.gameObject.SetActive(false);
			}
		}
	}

	public static void on_reset(Foliage foliage) {
		foliage.hit = false;
		foliage.hit_time = 0.0f;

		for(int i = 0; i < foliage.entries.Length; i++) {
			FoliageController entry = foliage.entries[i];
			entry.gameObject.SetActive(true);
			entry.transform.position = entry.initial_pos;
			entry.transform.rotation = entry.initial_rot;
		}
	}

	public static void update(GameManager game_manager, Foliage foliage) {
		Camera camera_ = game_manager.player2_inst != null ? game_manager.player2_inst.camera_ : null;
		if(camera_ != null && !foliage.hit) {
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

		if(foliage.hit) {
			foliage.hit_time += Time.deltaTime;

			for(int i = 0; i < foliage.entries.Length; i++) {
				FoliageController entry = foliage.entries[i];
				Transform transform = entry.transform;

				Vector3 p = transform.position;
				p.y = 0.0f;
				Vector3 q = new Vector3(414.04f, 0.0f, 146.27f);

				Vector3 d = p - q;
				float dist_sqr = d.x * d.x + d.y * d.y + d.z * d.z;
				float dist = Mathf.Sqrt(dist_sqr);
				d *= (1.0f / dist);

				Vector3 axis = Vector3.Cross(Vector3.up, d).normalized;

				float t = Mathf.Clamp01(foliage.hit_time * 8.5f - (Mathf.Max(0.0f, dist * 0.1f) + 1.7f));
				float angle = (50.0f + entry.rnd * 10.0f) * Mathf.Min(1.0f, t * (650.0f / dist_sqr));

				transform.rotation = Quaternion.AngleAxis(angle, axis) * entry.initial_rot;
			}
		}
	}
}