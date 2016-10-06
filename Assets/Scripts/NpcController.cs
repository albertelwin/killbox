
using UnityEngine;
using System.Collections;

public enum NpcType {
	HUMAN,
	ANIMAL,
	BIRD,
}

//TODO: Remove awake/update/etc.!!
public class NpcController : MonoBehaviour {
	[System.NonSerialized] public Collider collider_;
	[System.NonSerialized] public Animation anim;
	[System.NonSerialized] public Renderer renderer_;
	[System.NonSerialized] public AudioSource audio_source;
	[System.NonSerialized] public ParticleSystem particle_system;

	public NpcType type = NpcType.HUMAN;

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

	[System.NonSerialized] public static Color[] COLOR_POOL = {
		Util.new_color(47, 147, 246),
		Util.new_color(51, 203, 152),
		Util.new_color(220, 126, 225),
	};

	void Awake() {
		collider_ = transform.Find("Collider").GetComponent<Collider>();

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
			renderer_ = (SkinnedMeshRenderer)skinned_mesh_renderers[0];
			Assert.is_true(renderer_ != null);
		}
		else {
			Assert.invalid_path();
		}

		initial_pos = transform.position;
	}

	public void Start() {
		transform.position = initial_pos;

		collider_.enabled = true;

		if(nav_agent) {
			nav_agent.enabled = true;
			nav_agent.speed = 7.0f;

			path_agent = motion_path ? MotionPath.new_agent(transform, nav_agent, motion_path) : null;
			if(path_agent == null) {
				nav_agent.enabled = false;
			}
		}

		if(fracture != null) {
			Environment.remove_fracture(fracture);
		}

		anim.gameObject.SetActive(true);
		Util.offset_first_anim(anim);
		anim.Play();

		activated = false;
		emission = 0.0f;
	}

	public static void update(GameManager game_manager, NpcController npc) {
		bool entered_player_radius = false;
		if(game_manager.player2_inst != null) {
			Transform player2 = game_manager.player2_inst.transform;

			if(game_manager.player_type == PlayerType.PLAYER2 && !game_manager.first_missile_hit) {
				Vector3 closest_point = npc.collider_.ClosestPointOnBounds(player2.position);
				float distance_to_player = Vector3.Distance(closest_point, player2.position);

				if(!npc.activated && distance_to_player < ACTIVAITON_DIST) {
					if(npc.type == NpcType.HUMAN) {
						npc.audio_source.clip = Audio.get_random_clip(game_manager.audio, Audio.Clip.NPC);
						npc.audio_source.Play();

						if(npc.particle_system != null) {
							npc.particle_system.Emit(npc.particle_system.maxParticles);
						}

						npc.color_index++;
						if(npc.color_index >= COLOR_POOL.Length) {
							npc.color_index = 0;
						}
						npc.renderer_.material.color = COLOR_POOL[npc.color_index];
					}

					entered_player_radius = true;
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

		MotionPathAgent path_agent = npc.path_agent;
		if(path_agent != null) {
			if(!game_manager.first_missile_hit) {
				MotionPath.move_agent(path_agent, Time.deltaTime, entered_player_radius && npc.runs_from_player);
			}

			if(path_agent.started) {
				Util.cross_fade_anim(npc.anim, "moving");
			}
			else if(path_agent.stopped && path_agent.prev_node != null) {
				switch(path_agent.prev_node.stop_animation) {
					case MotionPathAnimationType.IDLE: {
						Util.cross_fade_anim(npc.anim, "idle");
						break;
					}

					case MotionPathAnimationType.MOVING: {
						Util.cross_fade_anim(npc.anim, "moving");
						break;
					}

					case MotionPathAnimationType.ACTION: {
						Util.cross_fade_anim(npc.anim, "action");
						break;
					}
				}
			}
		}

		npc.emission = Mathf.Lerp(npc.emission, npc.activated ? 0.5f : 0.0f, Time.deltaTime * 8.0f);
		npc.renderer_.material.SetFloat("_Emission", npc.emission);
	}

	public static void on_explosion(GameManager game_manager, NpcController npc, Vector3 hit_pos, float force) {
		Vector3 point_on_bounds = npc.collider_.ClosestPointOnBounds(hit_pos);
		if(Vector3.Distance(hit_pos, point_on_bounds) < Environment.EXPLOSION_RADIUS) {
			npc.collider_.enabled = false;
			npc.anim.gameObject.SetActive(false);

			if(npc.fracture != null) {
				Environment.apply_fracture(npc.fracture, hit_pos, force);
			}

			//TODO: Seperate npc type for targets!!
			Assert.is_true(npc.nav_agent == null);
			npc.path_agent = null;
		}
		else {
			if(npc.type == NpcType.HUMAN) {
				Util.cross_fade_anim(npc.anim, "moving");

				Transform safe_point = null;
				float shortest_distance = Mathf.Infinity;
				for(int index = 0; index < game_manager.scenario.safe_points.childCount; index++) {
					Transform safe_point_transform = game_manager.scenario.safe_points.GetChild(index);

					float dist = Vector3.Distance(npc.transform.position, safe_point_transform.position);
					if(dist < shortest_distance) {
						shortest_distance = dist;
						safe_point = safe_point_transform;
					}
				}

				if(npc.nav_agent) {
					if(safe_point) {
						npc.nav_agent.enabled = true;
						npc.nav_agent.speed = 7.0f;
						npc.nav_agent.SetDestination(safe_point.position);
					}
					else {
						npc.nav_agent.enabled = false;
					}
				}
			}
			else if(npc.type == NpcType.BIRD) {
				npc.anim.gameObject.SetActive(false);
			}
		}
	}

	public static void on_pov_change(NpcController npc, PlayerType pov, Material material) {
		if(npc.type == NpcType.HUMAN) {
			npc.renderer_.material = material;

			if(pov == PlayerType.PLAYER2) {
				npc.color_index = Util.random_index(COLOR_POOL.Length);
				npc.renderer_.material.color = COLOR_POOL[npc.color_index];
			}
		}
	}
}
