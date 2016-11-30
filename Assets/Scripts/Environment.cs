
using UnityEngine;
using System.Collections;

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

	public class Prop {
		public Transform transform;
		public Renderer renderer;
		public Collider collider;
		public Rigidbody rigidbody;
		public Fracture fracture;
		public Vector3 initial_pos;
	}

	public class Explosion {
		public Transform transform;
		public AudioSource audio_source;
		public Renderer sphere;
	}

	public class Crater {
		public Transform transform;
		public Transform before;
		public Transform after;
	}

	public Transform transform;

	public TargetPointController target_point;

	public Building[] buildings;
	public Prop[] props;
	public Collectable[] collectables;
	public NpcController[] npcs;
	public int npc_scream_index;

	public Foliage foliage;

	public static float EXPLOSION_RADIUS = 10.0f;
	public Explosion explosion;

	public Transform explosion_prefab;
	public Transform explosion_;
	public Transform smoke_prefab;
	public Transform smoke_;

	public Crater crater;

	public PlayerType pov;

	public static Environment new_inst(GameManager game_manager, Transform transform) {
		Environment env = new Environment();
		env.transform = transform;

		env.target_point = transform.Find("TargetPoint").GetComponent<TargetPointController>();

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

		Transform props_parent = transform.Find("Props");
		env.props = new Prop[props_parent.childCount];
		for(int i = 0; i < props_parent.childCount; i++) {
			Prop prop = new Prop();
			prop.transform = props_parent.GetChild(i);
			prop.renderer = prop.transform.GetComponent<Renderer>();
			prop.collider = prop.transform.GetComponent<Collider>();
			prop.rigidbody = prop.transform.GetComponent<Rigidbody>();

			Transform fractured_mesh = prop.transform.Find("FracturedMesh");
			if(fractured_mesh != null) {
				prop.fracture = Fracture.new_inst(fractured_mesh);
			}

			prop.initial_pos = prop.transform.position;

			env.props[i] = prop;
		}

		Transform collectables_parent = transform.Find("Collectables");
		env.collectables = new Collectable[collectables_parent.childCount];
		for(int i = 0; i < env.collectables.Length; i++) {
			env.collectables[i] = Collectable.new_inst(collectables_parent.GetChild(i));
		}

		env.npc_scream_index = -1;

		Transform npcs_parent = transform.Find("Npcs");
		env.npcs = new NpcController[npcs_parent.childCount];
		for(int i = 0; i < npcs_parent.childCount; i++) {
			NpcController npc = npcs_parent.GetChild(i).GetComponent<NpcController>();
			env.npcs[i] = npc;
			if(npc.screams) {
				env.npc_scream_index = i;
			}
		}

		Assert.is_true(env.npc_scream_index >= 0);

		env.foliage = Foliage.new_inst(game_manager, transform.Find("Foliage"));

		env.explosion = new Explosion();
		env.explosion.transform = Util.new_transform(transform, "Explosion");
		env.explosion.audio_source = env.explosion.transform.gameObject.AddComponent<AudioSource>();
		env.explosion.sphere = ((Transform)Object.Instantiate(game_manager.explosion_prefab, Vector3.zero, Quaternion.identity)).GetComponent<Renderer>();
		env.explosion.sphere.enabled = false;
		env.explosion.sphere.transform.parent = env.explosion.transform;
		env.explosion.sphere.name = "Sphere";

		env.explosion_prefab = Util.load_prefab("ExplosionPrefab_");
		env.smoke_prefab = Util.load_prefab("SmokePrefab");

		env.crater = new Crater();
		env.crater.transform = transform.Find("Crater");
		if(env.crater.transform != null) {
			env.crater.before = env.crater.transform.Find("Before");
			env.crater.before.gameObject.SetActive(true);

			env.crater.after = env.crater.transform.Find("After");
			env.crater.after.gameObject.SetActive(false);
		}

		//TODO: Temp!!
		// GameObject.Find("Sun").GetComponent<Light>().shadows = LightShadows.None;
		// env.transform.Find("FarmBuildings").gameObject.SetActive(false);
		// env.transform.Find("Terrain").gameObject.SetActive(false);
		// env.transform.Find("Trees").gameObject.SetActive(false);
		// env.transform.Find("Buildings").gameObject.SetActive(false);
		// env.transform.Find("Props").gameObject.SetActive(false);
		// env.transform.Find("Foliage").gameObject.SetActive(false);
		// env.transform.Find("Collectables").gameObject.SetActive(false);
		// Component[] ground_renderers = env.transform.Find("Ground").GetComponentsInChildren(typeof(MeshRenderer), true);
		// for(int i = 0; i < ground_renderers.Length; i++) {
		// 	((MeshRenderer)ground_renderers[i]).enabled = false;
		// }
		// for(int i = 0; i < env.npcs.Length; i++) {
		// 	Transform anim = env.npcs[i].transform.Find("Animation");
		// 	SkinnedMeshRenderer renderer = anim.GetComponentInChildren<SkinnedMeshRenderer>();
		// 	renderer.enabled = false;
		// }

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

		for(int i = 0; i < env.props.Length; i++) {
			Prop prop = env.props[i];
			prop.renderer.enabled = true;
			prop.rigidbody.detectCollisions = true;
			prop.collider.enabled = true;

			if(prop.fracture != null) {
				remove_fracture(prop.fracture);
			}

			prop.transform.position = prop.initial_pos;
			prop.transform.rotation = Quaternion.identity;

			prop.rigidbody.velocity = Vector3.zero;
			prop.rigidbody.angularVelocity = Vector3.zero;
		}

		for(int i = 0; i < env.npcs.Length; i++) {
			env.npcs[i].Start();
		}
		NpcController.stop_screams(env);

		for(int i = 0; i < env.collectables.Length; i++) {
			Collectable.reset(env.collectables[i]);
		}

		Foliage.on_reset(env.foliage);

		env.explosion.sphere.enabled = false;
		env.explosion.sphere.material.color = Util.black;

		if(env.explosion_) {
			Object.Destroy(env.explosion_.gameObject);
			env.explosion_ = null;
		}

		if(env.smoke_) {
			Object.Destroy(env.smoke_.gameObject);
			env.smoke_ = null;
		}

		if(env.crater.transform != null) {
			env.crater.before.gameObject.SetActive(true);
			env.crater.after.gameObject.SetActive(false);
		}
	}

	public static void update(GameManager game_manager, Environment env) {
		Player2Controller player2 = game_manager.player2_inst;
		Transform player2_transform = null;
		if(player2 != null) {
			player2_transform = player2.transform;
		}

		for(int i = 0; i < env.npcs.Length; i++) {
			NpcController.update(game_manager, env.npcs[i], player2_transform);
		}

		Collectable.update_collectables(game_manager, env.collectables);

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
				NpcController.on_pov_change(npc, pov, npc_material);
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

	public static IEnumerator play_explosion_sphere(MonoBehaviour player, Renderer sphere) {
		sphere.enabled = true;

		yield return player.StartCoroutine(Util.lerp_local_scale(sphere.transform, Vector3.zero, Vector3.one * EXPLOSION_RADIUS * 2.0f, 0.2f));
		yield return Util.lerp_color(player, sphere, Util.black, Util.black_no_alpha, 8.0f);

		sphere.enabled = false;
	}

	public static void play_explosion_(MonoBehaviour player, Environment env, Vector3 hit_pos) {
		env.explosion.transform.position = hit_pos;

		env.explosion_ = (Transform)Object.Instantiate(env.explosion_prefab, hit_pos, Quaternion.identity);
		if(env.smoke_prefab && env.smoke_ == null) {
			env.smoke_ = (Transform)Object.Instantiate(env.smoke_prefab, hit_pos, Quaternion.identity);
		}
		player.StartCoroutine(play_explosion_sphere(player, env.explosion.sphere));
	}

	public static void play_explosion(GameManager game_manager, MonoBehaviour player, Environment env, Vector3 hit_pos) {
		play_explosion_(player, env, hit_pos);

		float force = 2000.0f;
		if(game_manager.first_missile_hit) {
			force *= 3.0f;
		}

		for(int i = 0; i < env.buildings.Length; i++) {
			Building building = env.buildings[i];
			if(building.fracture != null) {
				building.mesh.gameObject.SetActive(false);

				if(building.post_fracture_mesh) {
					building.post_fracture_mesh.gameObject.SetActive(true);
				}

				apply_fracture(building.fracture, hit_pos, force);
			}
		}

		for(int i = 0; i < env.props.Length; i++) {
			Prop prop = env.props[i];
			if(prop.fracture != null) {
				prop.renderer.enabled = false;
				prop.collider.enabled = false;
				prop.rigidbody.detectCollisions = false;

				apply_fracture(prop.fracture, hit_pos, force);
			}
		}

		Foliage.on_explosion(env.foliage, hit_pos);

		if(env.crater.transform != null) {
			env.crater.before.gameObject.SetActive(false);
			env.crater.after.gameObject.SetActive(true);
		}

		for(int i = 0; i < env.npcs.Length; i++) {
			NpcController.on_explosion(game_manager, env.npcs[i], env.target_point, hit_pos);
		}

		game_manager.first_missile_hit = true;
	}
}
