
using UnityEngine;
using System.Collections;

public enum NpcType {
	HUMAN,
	ANIMAL,
}

//TODO: Remove awake/update/etc.!!
public class NpcController : MonoBehaviour {
	[System.NonSerialized] public Renderer renderer_;
	[System.NonSerialized] public Collider collider_;
	[System.NonSerialized] public Animation anim;
	[System.NonSerialized] public Renderer anim_renderer;
	[System.NonSerialized] public AudioSource audio_source;
	[System.NonSerialized] public ParticleSystem particle_system;

	public NpcType type = NpcType.HUMAN;

	Renderer main_renderer;

	[System.NonSerialized] public Environment.Fracture fracture;

	[System.NonSerialized] public NavMeshAgent nav_agent;
	[System.NonSerialized] public MotionPathAgent path_agent;
	public MotionPathController motion_path;
	public bool runs_from_player = false;

	Vector3 initial_pos;

	[System.NonSerialized] public int color_index;

	public static float ACTIVAITON_DIST = 2.5f;

	bool activated;
	float emission;

	void Awake() {
		Transform mesh = transform.Find("Mesh");
		if(mesh != null) {
			mesh.gameObject.SetActive(true);

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
			Assert.is_true(skinned_mesh_renderers != null && skinned_mesh_renderers.Length == 1);
			anim_renderer = (SkinnedMeshRenderer)skinned_mesh_renderers[0];
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

			Util.offset_first_anim(anim);
			anim.Play();

			renderer_.enabled = false;
		}

		activated = false;
		emission = 0.0f;
	}

	public static void update(GameManager game_manager, NpcController npc) {
		MotionPathAgent path_agent = npc.path_agent;
		if(path_agent != null) {
			Transform player2 = game_manager.player2_inst != null ? game_manager.player2_inst.transform : null;
			MotionPath.move_agent(path_agent, Time.deltaTime, player2, game_manager.first_missile_hit);

			if(npc.anim != null) {
				if(path_agent.started) {
					npc.anim.CrossFade("moving");
				}
				else if(path_agent.stopped && path_agent.prev_node != null) {
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

					if(npc.anim[next_anim] != null) {
						npc.anim.CrossFade(next_anim);
					}
				}
			}

			if(path_agent.entered_player_radius) {
				if(npc.type == NpcType.HUMAN) {
					npc.color_index++;
					if(npc.color_index >= game_manager.npc_color_pool.Length) {
						npc.color_index = 0;
					}

					npc.renderer_.material.color = game_manager.npc_color_pool[npc.color_index];
					if(npc.anim_renderer != null) {
						npc.anim_renderer.material.color = npc.renderer_.material.color;
					}
				}
			}
		}

		//TODO: Collapse this!!
		if(game_manager.player2_inst != null) {
			Transform player2 = game_manager.player2_inst.transform;

			if(game_manager.player_type == PlayerType.PLAYER2 && game_manager.first_missile_hit == false) {
				Vector3 closest_point = npc.collider_.ClosestPointOnBounds(player2.position);
				float distance_to_player = Vector3.Distance(closest_point, player2.position);

				if(!npc.activated && distance_to_player < ACTIVAITON_DIST) {
					if(npc.type == NpcType.HUMAN) {
						npc.audio_source.clip = Audio.get_random_clip(game_manager.audio, Audio.Clip.NPC);
						npc.audio_source.Play();

						if(npc.particle_system != null) {
							npc.particle_system.Emit(npc.particle_system.maxParticles);
						}
					}

					npc.activated = true;
				}
				else if(npc.activated && distance_to_player > ACTIVAITON_DIST) {
					npc.activated = false;
				}
			}
			else {
				npc.activated = false;
			}
		}

		npc.emission = Mathf.Lerp(npc.emission, npc.activated ? 0.5f : 0.0f, Time.deltaTime * 8.0f);
		npc.main_renderer.material.SetFloat("_Emission", npc.emission);
	}

	public static void on_explosion(GameManager game_manager, NpcController npc, Vector3 hit_pos, float force) {
		Vector3 point_on_bounds = npc.collider_.ClosestPointOnBounds(hit_pos);
		if(Vector3.Distance(hit_pos, point_on_bounds) < Environment.EXPLOSION_RADIUS) {
			npc.renderer_.enabled = false;
			npc.collider_.enabled = false;
			if(npc.nav_agent) {
				npc.nav_agent.enabled = false;
			}

			if(npc.anim != null) {
				npc.anim.gameObject.SetActive(false);
			}

			if(npc.fracture != null) {
				Environment.apply_fracture(npc.fracture, hit_pos, force);
			}
		}
		else {
			if(npc.type == NpcType.HUMAN) {
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

				if(npc.nav_agent) {
					npc.nav_agent.enabled = true;
					npc.nav_agent.speed = 7.0f;
					npc.nav_agent.SetDestination(safe_point.position);
				}
			}
			else {
				//TODO: Animal case!!
			}
		}
	}
}
