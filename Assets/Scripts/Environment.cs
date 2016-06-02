
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
		public Transform mesh;
		public Transform post_fracture_mesh;
		public Fracture fracture;
	}

	public struct Animal {
		public Transform transform;
		public Animation anim;
		public Vector3 initial_pos;
	}

	public class Explosion {
		public Transform transform;
		public AudioSource audio_source;
		public Renderer smoke;
		public Renderer shock_wave;
	}

	public struct Crater {
		public Transform transform;
		public Transform before;
		public Transform after;
	}

	public Transform transform;

	public Building[] buildings;
	public Animal[] animals;
	public Collectable[] collectables;
	public NpcController[] npcs;

	public Foliage foliage;

	public static float EXPLOSION_RADIUS = 10.0f;
	public Explosion explosion;

	public Crater crater;

	public KillboxController killbox;

	public Renderer controls_hint;
	public bool controls_hidden;

	public PlayerType pov;

	public static Environment new_inst(GameManager game_manager, Transform transform) {
		Environment env = new Environment();
		env.transform = transform;

		Transform buildings_parent = transform.Find("Buildings");
		env.buildings = new Building[buildings_parent.childCount];
		for(int i = 0; i < buildings_parent.childCount; i++) {
			Building building = new Building();
			building.transform = buildings_parent.GetChild(i);
			building.mesh = building.transform.Find("Mesh");

			building.post_fracture_mesh = building.transform.Find("PostFractureMesh");
			if(building.post_fracture_mesh) {
				building.post_fracture_mesh.gameObject.SetActive(false);
			}

			Transform fractured_mesh = building.transform.Find("FracturedMesh");
			if(fractured_mesh != null) {
				building.fracture = Fracture.new_inst(fractured_mesh);
			}

			env.buildings[i] = building;
		}

		Transform animals_parent = transform.Find("Animals");
		env.animals = new Animal[animals_parent.childCount];
		for(int i = 0; i < env.animals.Length; i++) {
			Animal animal = new Animal();
			animal.transform = animals_parent.GetChild(i);
			animal.anim = animal.transform.GetComponent<Animation>();
			Assert.is_true(animal.anim != null);
			animal.initial_pos = animal.transform.position;

			Util.offset_first_anim(animal.anim);
			animal.anim.Play();

			env.animals[i] = animal;
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

		env.foliage = Foliage.new_inst(game_manager, transform.Find("Foliage"));

		env.explosion = new Explosion();
		env.explosion.transform = Util.new_transform(transform, "Explosion");
		env.explosion.audio_source = env.explosion.transform.gameObject.AddComponent<AudioSource>();
		env.explosion.smoke = ((Transform)Object.Instantiate(game_manager.explosion_prefab, Vector3.zero, Quaternion.identity)).GetComponent<Renderer>();
		env.explosion.smoke.enabled = false;
		env.explosion.smoke.transform.parent = env.explosion.transform;
		env.explosion.smoke.name = "Smoke";
		env.explosion.shock_wave = ((Transform)Object.Instantiate(game_manager.explosion_prefab, Vector3.zero, Quaternion.identity)).GetComponent<Renderer>();
		env.explosion.shock_wave.enabled = false;
		env.explosion.shock_wave.transform.parent = env.explosion.transform;
		env.explosion.shock_wave.name = "ShockWave";

		//TODO: Handle multiple of these!!
		env.crater.transform = transform.Find("Crater");
		if(env.crater.transform != null) {
			env.crater.before = env.crater.transform.Find("Before");
			env.crater.before.gameObject.SetActive(true);

			env.crater.after = env.crater.transform.Find("After");
			env.crater.after.gameObject.SetActive(false);
		}

		env.killbox = transform.Find("Killbox").GetComponent<KillboxController>();

		env.controls_hint = transform.Find("Controls").GetComponent<Renderer>();
		env.controls_hidden = false;

		return env;
	}

	public static void reset(Environment env) {
		for(int i = 0; i < env.buildings.Length; i++) {
			Building building = env.buildings[i];

			building.mesh.gameObject.SetActive(true);

			if(building.post_fracture_mesh != null) {
				building.post_fracture_mesh.gameObject.SetActive(false);
			}

			if(building.fracture != null) {
				remove_fracture(building.fracture);
			}
		}

		for(int i = 0; i < env.animals.Length; i++) {
			Animal animal = env.animals[i];
			animal.transform.position = animal.initial_pos;

			animal.anim.Stop();
			Util.offset_first_anim(animal.anim);
			animal.anim.Play();
		}

		for(int i = 0; i < env.npcs.Length; i++) {
			env.npcs[i].Start();
		}

		for(int i = 0; i < env.collectables.Length; i++) {
			Collectable.reset(env.collectables[i]);
		}

		Foliage.on_reset(env.foliage);

		env.explosion.smoke.enabled = false;
		env.explosion.smoke.material.color = Util.black;
		env.explosion.shock_wave.enabled = false;
		env.explosion.shock_wave.material.color = Util.black;

		if(env.crater.transform != null) {
			env.crater.before.gameObject.SetActive(true);
			env.crater.after.gameObject.SetActive(false);
		}

		Killbox.hide(env.killbox);

		env.controls_hint.gameObject.SetActive(true);
		env.controls_hint.material.color = Util.white;
		env.controls_hidden = false;
	}

	public static void update(GameManager game_manager, Environment env) {
		Killbox.update(env.killbox);

		for(int i = 0; i < env.npcs.Length; i++) {
			NpcController.update(game_manager, env.npcs[i]);
		}

		for(int i = 0; i < env.collectables.Length; i++) {
			Collectable.update(game_manager, env.collectables[i]);
		}

		Foliage.update(game_manager, env.foliage);
	}

	public static string get_pov_material_id(PlayerType pov) {
		return pov == PlayerType.PLAYER1 ? "default" : "alive";
	}

	public static void apply_pov(GameManager game_manager, Environment env, PlayerType pov) {
		if(env.pov != pov) {
			env.pov = pov;

			int last_seed = Random.seed;
			//TODO: Pick a better seed??
			Random.seed = 1234;

			string material_id = get_pov_material_id(env.pov);
			Material npc_material = Resources.Load("npc_" + material_id + "_mat") as Material;

			for(int i = 0; i < env.npcs.Length; i++) {
				NpcController npc = env.npcs[i];
				if(npc.type == NpcType.HUMAN) {
					npc.renderer_.material = npc_material;
					if(npc.anim_renderer != null) {
						npc.anim_renderer.material = npc_material;
					}

					if(env.pov == PlayerType.PLAYER2) {
						npc.color_index = Util.random_index(game_manager.npc_color_pool.Length);
						npc.renderer_.material.color = game_manager.npc_color_pool[npc.color_index];
						if(npc.anim_renderer != null) {
							npc.anim_renderer.material.color = npc.renderer_.material.color;
						}
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

		// Transform explosion_prefab = ((GameObject)Resources.Load("ExplosionPrefab_")).transform;
		// Transform explosion = (Transform)Object.Instantiate(explosion_prefab, hit_pos, Quaternion.identity);
	}

	public static void play_explosion(GameManager game_manager, MonoBehaviour player, Environment env, Vector3 hit_pos) {
		play_explosion_(player, env, hit_pos);

		float force = 400.0f;
		if(game_manager.first_missile_hit) {
			force *= 3.0f;
		}

		for(int i = 0; i < env.buildings.Length; i++) {
			Building building = env.buildings[i];

			// Vector3 point_on_bounds = building.collider.ClosestPointOnBounds(hit_pos);
			// if(Vector3.Distance(hit_pos, point_on_bounds) < EXPLOSION_RADIUS) {
			// 	//TODO: Only do this if the building can be fractured??
			// 	building.renderer.enabled = false;
			// 	building.collider.enabled = false;

			// 	if(building.fracture != null) {
			// 		apply_fracture(building.fracture, hit_pos, force);
			// 	}
			// }

			if(building.fracture != null) {
				building.mesh.gameObject.SetActive(false);

				if(building.post_fracture_mesh) {
					building.post_fracture_mesh.gameObject.SetActive(true);
				}

				apply_fracture(building.fracture, hit_pos, force);
			}
		}

		Foliage.on_explosion(env.foliage, hit_pos);

		if(env.crater.transform != null) {
			env.crater.before.gameObject.SetActive(false);
			env.crater.after.gameObject.SetActive(true);
		}

		for(int i = 0; i < env.npcs.Length; i++) {
			NpcController.on_explosion(game_manager, env.npcs[i], hit_pos, force);
		}

		game_manager.first_missile_hit = true;
	}
}
