
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
	[System.NonSerialized] public MotionPathAgent path_agent;
	public MotionPathController motion_path;
	public bool runs_from_player = false;

	Vector3 initial_pos;

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

			Component[] skinned_mesh_renderers = anim_transform.GetComponentsInChildren(typeof(SkinnedMeshRenderer), true);
			anim_renderer = (SkinnedMeshRenderer)skinned_mesh_renderers[0];

			// anim_renderer = anim_transform.GetComponentsInChildren(typeof(SkinnedMeshRenderer), true)[0];
			Assert.is_true(anim_renderer != null);

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

		if(nav_agent) {
			nav_agent.enabled = true;
			nav_agent.speed = 7.0f;

			path_agent = motion_path ? MotionPath.new_agent(transform, nav_agent, motion_path) : null;
			if(path_agent != null) {
				path_agent.runs_from_player = runs_from_player;
			}
			else {
				nav_agent.enabled = false;
			}
		}

		if(fracture != null) {
			Environment.remove_fracture(fracture);
		}

		if(anim != null) {
			anim.gameObject.SetActive(true);

			if(anim["moving"] != null) {
				anim["moving"].time = Random.Range(0.0f, anim["moving"].length);
			}

			anim.Play();

			renderer_.enabled = false;
		}

		activation_dist = 2.5f;
		activated = false;
		emission = 0.0f;
	}

	//TODO: Update these guys somewhere else, this func is a perf hog!!
	void Update() {
		if(path_agent != null) {
			Transform player2 = game_manager.player2_inst != null ? game_manager.player2_inst.transform : null;
			MotionPath.move_agent(path_agent, Time.deltaTime, player2, game_manager.first_missile_hit);

			if(anim != null) {
				if(path_agent.started) {
					if(name == "Chicken") {
						Debug.Log("MOVING");
					}

					anim.CrossFade("moving");
				}
				else if(path_agent.stopped && path_agent.prev_node != null) {

					if(name == "Chicken") {
						Debug.Log(path_agent.prev_node.stop_animation.ToString());
					}

					string next_anim = "idle";
					switch(path_agent.prev_node.stop_animation) {
						case MotionPathAnimationType.IDLE: {
							next_anim = "idle";
							break;
						}

						case MotionPathAnimationType.MOVING: {
							next_anim = "moving";
							break;
						}

						case MotionPathAnimationType.ACTION: {
							next_anim = "action";
							break;
						}
					}

					if(anim[next_anim] != null) {
						anim.CrossFade(next_anim);
					}
				}
			}

			if(path_agent.entered_player_radius) {
				color_index++;
				if(color_index >= game_manager.npc_color_pool.Length) {
					color_index = 0;
				}

				renderer_.material.color = game_manager.npc_color_pool[color_index];
				if(anim_renderer != null) {
					anim_renderer.material.color = renderer_.material.color;
				}
			}
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
