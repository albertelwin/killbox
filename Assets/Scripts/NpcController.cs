using UnityEngine;
using System.Collections;

//TODO: Remove awake/update/etc.!!
public class NpcController : MonoBehaviour {
	GameManager game_manager;

	[System.NonSerialized] public Renderer renderer_;
	[System.NonSerialized] public Collider collider_;
	[System.NonSerialized] public Animation anim;
	[System.NonSerialized] public Renderer anim_renderer;
	[System.NonSerialized] public AudioSource audio_source;
	[System.NonSerialized] public ParticleSystem particle_system;

	Renderer main_renderer;

	[System.NonSerialized] public Environment.Fracture fracture;

	[System.NonSerialized] public NavMeshAgent nav_agent;
	public MotionPathController motion_path;
	[System.NonSerialized] public int next_node_index;
	[System.NonSerialized] public MotionPathNode motion_node;
	[System.NonSerialized] public MotionPathNode prev_motion_node;
	[System.NonSerialized] public Vector3 motion_path_target;

	Vector3 initial_pos;

	[System.NonSerialized] public float walk_speed;
	[System.NonSerialized] public float walk_accel;
	[System.NonSerialized] public float run_speed;
	[System.NonSerialized] public float run_accel;
	

	[System.NonSerialized] public bool in_player_radius;
	[System.NonSerialized] public int color_index;

	float activation_dist;
	bool activated;
	float emission;

	void Awake() {
		game_manager = GameObject.Find("GameManager").GetComponent<GameManager>();

		Transform mesh = transform.Find("Mesh");
		if(mesh != null) {
			renderer_ = mesh.GetComponent<Renderer>();
			collider_ = mesh.GetComponent<Collider>();
		}
		else {
			renderer_ = GetComponent<Renderer>();
			collider_ = GetComponent<Collider>();
		}

		nav_agent = GetComponent<NavMeshAgent>();
		audio_source = GetComponent<AudioSource>();
		particle_system = GetComponent<ParticleSystem>();

		Transform fractured_mesh = transform.Find("FracturedMesh");
		if(fractured_mesh != null) {
			fracture = Environment.Fracture.new_inst(fractured_mesh);
		}

		Transform anim_transform = transform.Find("Animation");
		if(anim_transform != null) {
			anim = anim_transform.GetComponent<Animation>();
			//TODO: Make sure this is the right child??
			anim_renderer = anim_transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
			main_renderer = anim_renderer;
		}
		else {
			main_renderer = renderer_;
		}

		initial_pos = transform.position;
	}

	public void Start() {
		transform.position = initial_pos;

		renderer_.enabled = true;
		collider_.enabled = true;

		nav_agent.enabled = true;
		nav_agent.speed = 7.0f;
		nav_agent.Warp(initial_pos);
		nav_agent.SetDestination(initial_pos);

		next_node_index = 0;
		motion_node = null;
		prev_motion_node = null;

		if(fracture != null) {
			Environment.remove_fracture(fracture);
		}

		if(anim != null) {
			anim.gameObject.SetActive(true);

			anim["Take 001"].time = Random.Range(0.0f, anim["Take 001"].length);
			anim.Play();

			renderer_.enabled = false;
		}

		//TODO: Tweak these!!
		walk_speed = 2.0f;
		walk_accel = 8.0f;
		run_speed = 12.0f;
		run_accel = 48.0f;

		activation_dist = 2.5f;
		activated = false;
		emission = 0.0f;
	}

	void Update() {
		if(nav_agent.enabled && motion_path && !game_manager.first_missile_hit) {
			if(!motion_node) {
				if(next_node_index >= MotionPathController.get_node_count(motion_path)) {
					next_node_index = 0;
				}

				motion_node = MotionPathController.get_node(motion_path, next_node_index);
			}

			Assert.is_true(motion_node != null);

			//TODO: Calculate this from velocity/distance/etc.!!
			float min_dist = Mathf.Max(nav_agent.speed * 0.5f, 1.0f);
			if(nav_agent.remainingDistance <= min_dist) {
				prev_motion_node = motion_node;

				next_node_index = MotionPathController.get_node_index(motion_path, motion_node) + 1;
				if(next_node_index >= MotionPathController.get_node_count(motion_path)) {
					next_node_index = 0;
				}

				motion_node = MotionPathController.get_node(motion_path, next_node_index++);
				
				if(next_node_index >= MotionPathController.get_node_count(motion_path)) {
					next_node_index = 0;
				}
			}

			float speed_modifier = motion_path.global_speed;
			if(prev_motion_node && prev_motion_node.override_speed) {
				speed_modifier = prev_motion_node.speed;
			}

			if(motion_path_target != motion_node.transform.position) {
				motion_path_target = motion_node.transform.position;
				nav_agent.SetDestination(motion_node.transform.position);
			}

			nav_agent.speed = walk_speed * speed_modifier;
			nav_agent.acceleration = walk_accel;

			// if(game_manager.player2_inst != null) {
			// 	Transform player2 = game_manager.player2_inst.transform;

			// 	float dist_to_player = Vector3.Distance(transform.position, player2.position);
			// 	if(dist_to_player <= 2.5f) {
			// 		if(!in_player_radius) {
			// 			color_index++;
			// 			if(color_index >= game_manager.npc_color_pool.Length) {
			// 				color_index = 0;
			// 			}

			// 			renderer_.material.color = game_manager.npc_color_pool[color_index];
			// 			if(anim_renderer != null) {
			// 				anim_renderer.material.color = renderer_.material.color;
			// 			}
			// 		}

			// 		in_player_radius = true;
			// 		nav_agent.speed = run_speed;
			// 		nav_agent.acceleration = run_accel;
			// 	}
			// 	else {
			// 		in_player_radius = false;
			// 	}
			// }
		}

		//TODO: Collapse this!!
		if(game_manager.player2_inst != null) {
			Transform player2 = game_manager.player2_inst.transform;

			if(game_manager.player_type == PlayerType.PLAYER2 && game_manager.first_missile_hit == false) {
				Vector3 closest_point = collider_.ClosestPointOnBounds(player2.position);
				float distance_to_player = Vector3.Distance(closest_point, player2.position);
				
				if(!activated && distance_to_player < activation_dist) {
					audio_source.clip = Audio.get_random_clip(game_manager.audio, Audio.Clip.NPC);
					audio_source.Play();

					if(particle_system != null) {
						particle_system.Emit(particle_system.maxParticles);
					}

					activated = true;
				}
				else if(activated && distance_to_player > activation_dist) {
					activated = false;
				}
			}
			else {
				activated = false;
			}
		}

		emission = Mathf.Lerp(emission, activated ? 0.5f : 0.0f, Time.deltaTime * 8.0f);
		main_renderer.material.SetFloat("_Emission", emission);
	}
}
