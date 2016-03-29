using UnityEngine;
using System.Collections;

public enum ScenarioType {
	FARM,
	MARKET,
	CAR,

	COUNT,
	NONE,
}

public class Environment {
	public enum Look {
		PLAYER1_POV,
		PLAYER2_POV,
	}

	public class FractureCell {
		public Transform transform;
		public Rigidbody rigidbody;
		public Vector3 initial_local_pos;
	}

	public class Fracture {
		public Transform transform;
		public FractureCell[] cells;

		public static Fracture new_inst(Transform transform) {
			Fracture fracture = new Fracture();
			fracture.transform = transform;

			fracture.cells = new FractureCell[transform.childCount];
			for(int i = 0; i < fracture.transform.childCount; i++) {
				FractureCell cell = new FractureCell();
				cell.transform = fracture.transform.GetChild(i);
				cell.rigidbody = cell.transform.GetComponent<Rigidbody>();
				cell.initial_local_pos = cell.transform.localPosition;

				fracture.cells[i] = cell;
			}

			return fracture;
		}
	}

	public class Building {
		public Transform transform;
		public Renderer renderer;
		public Collider collider;
		public Fracture fracture;
	}

	public class Explosion {
		public Transform transform;
		public AudioSource audio_source;
		public Renderer smoke;
		public Renderer shock_wave;
	}

	public Transform transform;

	public Building[] buildings;
	public Collectable[] collectables;
	public NpcController[] npcs;

	static public float EXPLOSION_RADIUS = 10.0f;
	public Explosion explosion;

	public KillboxController killbox;

	public Renderer controls_hint;
	public bool controls_hidden;

	public Look look;

	public static Environment new_inst(GameManager game_manager, Transform transform) {
		Environment env = new Environment();
		env.transform = transform;

		Transform buildings_parent = transform.Find("Buildings");
		env.buildings = new Building[buildings_parent.childCount];
		for(int i = 0; i < buildings_parent.childCount; i++) {
			Building building = new Building();
			building.transform = buildings_parent.GetChild(i);
			building.renderer = building.transform.GetComponent<Renderer>();
		building.collider = building.transform.GetComponent<Collider>();

			Transform fractured_mesh = building.transform.Find("FracturedMesh");
			if(fractured_mesh != null) {
				building.fracture = Fracture.new_inst(fractured_mesh);
			}

			env.buildings[i] = building;
		}

		Transform collectables_parent = transform.Find("Collectables");
		env.collectables = new Collectable[collectables_parent.childCount];
		for(int i = 0; i < env.collectables.Length; i++) {
			env.collectables[i] = Collectable.new_inst(collectables_parent.GetChild(i));
		}

		Transform npcs_parent = transform.Find("Npcs");
		env.npcs = new NpcController[npcs_parent.childCount];
		for(int i = 0; i < npcs_parent.childCount; i++) {
			env.npcs[i] = npcs_parent.GetChild(i).GetComponent<NpcController>();
		}

		env.explosion = new Explosion();
		env.explosion.transform = transform.Find("Explosion");
		env.explosion.audio_source = env.explosion.transform.GetComponent<AudioSource>();
		env.explosion.smoke = ((Transform)Object.Instantiate(game_manager.explosion_prefab, Vector3.zero, Quaternion.identity)).GetComponent<Renderer>();
		env.explosion.smoke.enabled = false;
		env.explosion.smoke.transform.parent = env.explosion.transform;
		env.explosion.smoke.name = "Smoke";
		env.explosion.shock_wave = ((Transform)Object.Instantiate(game_manager.explosion_prefab, Vector3.zero, Quaternion.identity)).GetComponent<Renderer>();
		env.explosion.shock_wave.enabled = false;
		env.explosion.shock_wave.transform.parent = env.explosion.transform;
		env.explosion.shock_wave.name = "ShockWave";

		env.killbox = transform.Find("Killbox").GetComponent<KillboxController>();

		env.controls_hint = transform.Find("Controls").GetComponent<Renderer>();
		env.controls_hidden = false;

		return env;
	}

	public static void reset(Environment env) {
		for(int i = 0; i < env.buildings.Length; i++) {
			Building building = env.buildings[i];

			building.renderer.enabled = true;
			building.collider.enabled = true;
			if(building.fracture != null) {
				remove_fracture(building.fracture);
			}
		}

		for(int i = 0; i < env.npcs.Length; i++) {
			env.npcs[i].Start();
		}

		for(int i = 0; i < env.collectables.Length; i++) {
			Collectable.reset(env.collectables[i]);
		}

		env.explosion.smoke.enabled = false;
		env.explosion.smoke.material.color = Util.black;
		env.explosion.shock_wave.enabled = false;
		env.explosion.shock_wave.material.color = Util.black;

		Killbox.hide(env.killbox);

		env.controls_hint.gameObject.SetActive(true);
		env.controls_hint.material.color = Util.white;
		env.controls_hidden = false;
	}

	public static void update(GameManager game_manager, Environment env) {
		Killbox.update(env.killbox);

		for(int i = 0; i < env.collectables.Length; i++) {
			Collectable.update(game_manager, env.collectables[i]);
		}
	}

	public static string get_material_look_id(Look look) {
		string id = "default";
		switch(look) {
			case Look.PLAYER2_POV: {
				id = "alive";
				break;
			}
		}

		return id;
	}

	public static void apply_look(GameManager game_manager, Environment env, Look new_look) {
		if(env.look != new_look) {
			env.look = new_look;

			int last_seed = Random.seed;
			//TODO: Pick a better seed??
			Random.seed = 1234;

			string material_id = get_material_look_id(env.look);
			Material npc_material = Resources.Load("npc_" + material_id + "_mat") as Material;

			for(int i = 0; i < env.npcs.Length; i++) {
				NpcController npc = env.npcs[i];
				npc.renderer_.material = npc_material;
				if(npc.anim_renderer != null) {
					npc.anim_renderer.material = npc_material;
				}

				if(env.look == Look.PLAYER2_POV) {
					npc.color_index = Util.random_index(game_manager.npc_color_pool.Length);
					npc.renderer_.material.color = game_manager.npc_color_pool[npc.color_index];
					if(npc.anim_renderer != null) {
						npc.anim_renderer.material.color = npc.renderer_.material.color;
					}
				}
			}

			Material player2_material = Resources.Load("other_" + material_id + "_mat") as Material;

			if(game_manager.player2_inst != null) {
				game_manager.player2_inst.renderer_.material = player2_material;
			}

			if(game_manager.network_player2_inst != null) {
				game_manager.network_player2_inst.renderer_.material = player2_material;
			}

			Random.seed = last_seed;
		}
	}

	public static void apply_fracture(Fracture fracture, Vector3 force_pos, float force_mag) {
		fracture.transform.gameObject.SetActive(true);

		for(int i = 0; i < fracture.cells.Length; i++) {
			FractureCell cell = fracture.cells[i];

			Vector3 force = (cell.transform.position - force_pos).normalized;
			force.y = Random.value;

			cell.rigidbody.AddForce(force * force_mag);
		}
	}

	public static void remove_fracture(Fracture fracture) {
		fracture.transform.gameObject.SetActive(false);

		for(int i = 0; i < fracture.cells.Length; i++) {
			FractureCell cell = fracture.cells[i];
			cell.rigidbody.velocity = Vector3.zero;
			cell.transform.localPosition = cell.initial_local_pos;
			cell.transform.localRotation = Quaternion.Euler(270.0f, 0.0f, 0.0f);
		}
	}

	public static void hide_controls_hint(GameManager game_manager, Environment env) {
		if(!env.controls_hidden) {
			env.controls_hidden = true;
			game_manager.StartCoroutine(hide_controls_hint_(env.controls_hint));
		}
	}

	public static IEnumerator hide_controls_hint_(Renderer hint) {
		Color hint_color = hint.material.color;

		float f = 0.5f;
		float t = 0.0f;
		while(t < 1.0f) {
			float a = t * t;
			Color color = new Color(hint_color.r, hint_color.g, hint_color.b, 1.0f - a);
			hint.material.color = color;

			t += Time.deltaTime * (1.0f / f);
			yield return Util.wait_for_frame;
		}

		hint.gameObject.SetActive(false);
	}

	public static IEnumerator play_explosion_smoke(MonoBehaviour player, Renderer sphere) {
		sphere.enabled = true;
		
		yield return player.StartCoroutine(Util.lerp_local_scale(sphere.transform, Vector3.zero, Vector3.one * EXPLOSION_RADIUS * 2.0f, 0.2f));
		yield return player.StartCoroutine(Util.lerp_material_color(sphere, Util.black, Util.black_no_alpha, 8.0f));

		sphere.enabled = false;
	}

	public static IEnumerator play_explosion_shock_wave(Renderer sphere) {
		sphere.enabled = true;

		float scale = EXPLOSION_RADIUS * 2.0f;
		float max_scale = 120.0f;

		float t = 0.0f;
		while(t < 1.0f) {
			sphere.transform.localScale = Vector3.one * Mathf.Lerp(scale, max_scale, t);
			sphere.material.color = Util.new_color(Util.black, Mathf.Lerp(0.5f, 0.0f, t));

			t += Time.deltaTime;
			yield return Util.wait_for_frame;
		}

		sphere.material.color = Util.black_no_alpha;
		sphere.enabled = false;
	}

	public static void play_explosion_(MonoBehaviour player, Environment env, Vector3 hit_pos) {
		env.explosion.transform.position = hit_pos;

		player.StartCoroutine(play_explosion_smoke(player, env.explosion.smoke));
		player.StartCoroutine(play_explosion_shock_wave(env.explosion.shock_wave));
	}

	public static void play_explosion(GameManager game_manager, MonoBehaviour player, Environment env, Vector3 hit_pos) {
		play_explosion_(player, env, hit_pos);

		float fracture_force = 400.0f;
		if(game_manager.first_missile_hit) {
			fracture_force *= 3.0f;
		}

		for(int i = 0; i < env.buildings.Length; i++) {
			Building building = env.buildings[i];

			Vector3 point_on_bounds = building.collider.ClosestPointOnBounds(hit_pos);
			if(Vector3.Distance(hit_pos, point_on_bounds) < EXPLOSION_RADIUS) {
				//TODO: Only do this if the building can be fractured??
				building.renderer.enabled = false;
				building.collider.enabled = false;

				if(building.fracture != null) {
					apply_fracture(building.fracture, hit_pos, fracture_force);
				}
			}
		}

		for(int i = 0; i < env.npcs.Length; i++) {
			NpcController npc = env.npcs[i];

			Vector3 point_on_bounds = npc.collider_.ClosestPointOnBounds(hit_pos);
			if(Vector3.Distance(hit_pos, point_on_bounds) < EXPLOSION_RADIUS) {
				npc.renderer_.enabled = false;
				npc.collider_.enabled = false;
				npc.nav_agent.enabled = false;

				if(npc.anim != null) {
					npc.anim.gameObject.SetActive(false);
				}

				// Transform fracture = npc.transform.Find("FracturedMesh");
				if(npc.fracture != null) {
					apply_fracture(npc.fracture, hit_pos, fracture_force);
				}
			}
			else {
				if(npc.anim != null) {
					npc.anim.Stop();
					npc.anim.gameObject.SetActive(false);

					npc.renderer_.enabled = true;
				}

				Transform safe_point = null;
				float shortest_distance = Mathf.Infinity;
				for(int ii = 0; ii < game_manager.scenario.safe_points.childCount; ii++) {
					Transform safe_point_transform = game_manager.scenario.safe_points.GetChild(ii);

					float dist = Vector3.Distance(npc.transform.position, safe_point_transform.position);
					if(dist < shortest_distance) {
						shortest_distance = dist;
						safe_point = safe_point_transform;
					}
				}

				npc.nav_agent.speed = 7.0f;
				npc.nav_agent.SetDestination(safe_point.position);
			}
		}

		game_manager.first_missile_hit = true;
	}
}
