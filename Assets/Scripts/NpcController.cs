
using UnityEngine;
using System.Collections;

public enum NpcType {
	ADULT,
	CHILD,
	CHICKEN,
	BIRD,
}

//TODO: Remove awake/update/etc.!!
public class NpcController : MonoBehaviour {
	[System.NonSerialized] public Collider collider_;
	[System.NonSerialized] public Animation anim;
	[System.NonSerialized] public Renderer renderer_;
	[System.NonSerialized] public ParticleSystem particle_system;

	public NpcType type = NpcType.ADULT;
	[System.NonSerialized] public bool is_human;
	[System.NonSerialized] public Audio.Clip clip_pool;

	[System.NonSerialized] public NavMeshAgent nav_agent;
	[System.NonSerialized] public MotionPathAgent path_agent;
	public MotionPathController motion_path;
	public Transform safe_point = null;
	public bool runs_from_player = false;
	public bool screams = false;

	Vector3 initial_pos;

	[System.NonSerialized] public int color_index;

	public static float ACTIVAITON_DIST = 2.5f;

	bool activated;
	float emission;
	[System.NonSerialized] public float delay_time;

	[System.NonSerialized] public Transform blood;

	[System.NonSerialized] public AudioSource scream_source;

	[System.NonSerialized] public static Color[] COLOR_POOL = {
		Util.new_color(47, 147, 246),
		Util.new_color(51, 203, 152),
		Util.new_color(220, 126, 225),
	};

	void Awake() {
		nav_agent = GetComponent<NavMeshAgent>();
		particle_system = GetComponent<ParticleSystem>();

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

		Component[] colliders = anim_transform.GetComponentsInChildren(typeof(Collider), true);
		if(colliders != null && colliders.Length == 1) {
			collider_ = (Collider)colliders[0];
		}
		else {
			collider_ = transform.Find("Collider").GetComponent<Collider>();
		}
		Assert.is_true(collider_ != null);

		Assert.is_true(safe_point != null);

		blood = transform.Find("Blood");

		initial_pos = transform.position;

		is_human = type == NpcType.ADULT || type == NpcType.CHILD;
		clip_pool = Audio.Clip.COUNT;
		//TODO: If we don't need the sfx in remove some of these!!
		if(type == NpcType.ADULT) {
			clip_pool = Audio.Clip.NPC_ADULT;
		}
		else if(type == NpcType.CHILD) {
			clip_pool = Audio.Clip.NPC_CHILD;
		}
		else if(type == NpcType.CHICKEN) {
			clip_pool = Audio.Clip.NPC_CHICKEN;
		}

		if(screams) {
			scream_source = Util.new_audio_source(transform, "ScreamAudioSource");
			scream_source.transform.localPosition = Vector3.zero;
			scream_source.loop = true;
			scream_source.spatialBlend = 1.0f;
			scream_source.dopplerLevel = 0.0f;
			scream_source.minDistance = 5.0f;
			scream_source.maxDistance = 300.0f;
		}
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
			else {
				if(type == NpcType.CHILD) {
					path_agent.walk_speed = 2.0f + Random.value * 2.0f;
					path_agent.run_speed = 10.0f + Random.value * 4.0f;
				}
			}
		}

		anim.gameObject.SetActive(true);
		Util.offset_first_anim(anim);
		anim.Play();

		activated = false;
		emission = 0.0f;
		delay_time = 0.0f;

		if(blood) {
			blood.gameObject.SetActive(false);
			Animation blood_anim = blood.GetComponent<Animation>();
			blood_anim.Stop();
		}
	}

	public static void update(GameManager game_manager, NpcController npc, Transform player2_transform) {
		if(!game_manager.first_missile_hit) {
			bool entered_player_radius = false;

			if(player2_transform != null && game_manager.player_type == PlayerType.PLAYER2) {
				Vector3 closest_point = npc.collider_.ClosestPointOnBounds(player2_transform.position);
				float distance_to_player = Vector3.Distance(closest_point, player2_transform.position);

				if(!npc.activated && distance_to_player < ACTIVAITON_DIST) {
					if(npc.clip_pool != Audio.Clip.COUNT) {
						AudioClip clip = Audio.get_random_clip(game_manager.audio, npc.clip_pool);
						Audio.play(game_manager.audio, clip);
					}

					if(npc.is_human) {
						if(npc.particle_system != null) {
							npc.particle_system.Emit(npc.particle_system.maxParticles);
						}

						if(COLOR_POOL.Length > 1) {
							int next_index = npc.color_index;
							while(next_index == npc.color_index) {
								next_index = (int)(Random.value * COLOR_POOL.Length);
								if(next_index >= COLOR_POOL.Length) {
									next_index = COLOR_POOL.Length - 1;
								}
							}
							npc.color_index = next_index;
							set_color(npc, COLOR_POOL[npc.color_index]);
						}
						else {
							Assert.invalid_path();
						}

						// npc.color_index++;
						// if(npc.color_index >= COLOR_POOL.Length) {
						// 	npc.color_index = 0;
						// }
					}

					entered_player_radius = true;
					npc.activated = true;
				}
				else if(npc.activated && distance_to_player > ACTIVAITON_DIST) {
					npc.activated = false;
				}
			}

			MotionPathAgent path_agent = npc.path_agent;
			if(path_agent != null) {
				MotionPath.move_agent(path_agent, Time.deltaTime, entered_player_radius && npc.runs_from_player);
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
		}
		else {
			if(npc.nav_agent) {
				float new_time = npc.delay_time - Time.deltaTime;
				if(new_time <= 0.0f && npc.delay_time > 0.0f) {
					Util.cross_fade_anim(npc.anim, "moving");

					npc.nav_agent.enabled = true;
					//TODO: Tweak this!!
					npc.nav_agent.speed = 6.0f + Random.value * 4.0f;
					npc.nav_agent.SetDestination(npc.safe_point.position);
				}

				npc.delay_time = new_time;
			}

			npc.activated = false;
		}

		//TODO: We don't need to be doing this every frame!!
		npc.emission = Mathf.Lerp(npc.emission, npc.activated ? 0.5f : 0.0f, Time.deltaTime * 8.0f);
		npc.renderer_.material.SetFloat("_Emission", npc.emission);
	}

	public static void on_kill(NpcController npc) {
		npc.collider_.enabled = false;
		npc.anim.gameObject.SetActive(false);
		npc.path_agent = null;
	}

	public static void on_explosion(GameManager game_manager, NpcController npc, TargetPointController target_point, Vector3 hit_pos) {
		if(npc.is_human) {
			Vector3 point_on_bounds = npc.collider_.ClosestPointOnBounds(hit_pos);
			float blast_radius = Environment.EXPLOSION_RADIUS * 2.0f;
			if(Vector3.Distance(hit_pos, point_on_bounds) < blast_radius) {
				npc.collider_.enabled = false;
				npc.anim.gameObject.SetActive(false);

				if(npc.nav_agent && npc.nav_agent.isOnNavMesh) {
					npc.nav_agent.Stop();
				}
				npc.path_agent = null;

				if(npc.blood) {
					npc.blood.gameObject.SetActive(true);
					npc.blood.rotation = Quaternion.Euler(0.0f, 360.0f * Random.value, 0.0f);
				}
			}
			else {
				Util.cross_fade_anim(npc.anim, "idle");

				npc.delay_time = 7.5f;

				// if(npc.nav_agent) {
				// 	Util.cross_fade_anim(npc.anim, "moving");

				// 	npc.nav_agent.enabled = true;
				// 	//TODO: Tweak this!!
				// 	npc.nav_agent.speed = 5.0f + Random.value * 5.0f;
				// 	npc.nav_agent.SetDestination(npc.safe_point.position);
				// }
			}
		}
		else if(npc.type == NpcType.BIRD) {
			npc.anim.gameObject.SetActive(false);
		}
	}

	public static void play_screams(Environment env, Audio audio) {
		NpcController npc = env.npcs[env.npc_scream_index];
		if(npc.scream_source != null) {
			if(npc.scream_source.clip == null) {
				npc.scream_source.clip = Audio.get_clip(audio, Audio.Clip.NPC_SCREAM);
			}
			npc.scream_source.Play();
		}
	}

	public static void stop_screams(Environment env) {
		NpcController npc = env.npcs[env.npc_scream_index];
		if(npc.scream_source != null) {
			npc.scream_source.Stop();
		}
	}

	public static void set_color(NpcController npc, Color color) {
		npc.renderer_.material.color = color;
		if(npc.blood) {
			Renderer body = npc.blood.Find("Body").GetComponent<Renderer>();
			body.material.color = color;
		}
	}

	public static void on_pov_change(NpcController npc, PlayerType pov, Material material) {
		if(npc.is_human) {
			npc.renderer_.material = material;
			if(npc.blood) {
				Renderer body = npc.blood.Find("Body").GetComponent<Renderer>();
				body.material = npc.renderer_.material;
			}

			Color color = npc.renderer_.material.color;
			if(pov == PlayerType.PLAYER2) {
				npc.color_index = Util.random_index(COLOR_POOL.Length);
				color = COLOR_POOL[npc.color_index];
			}

			set_color(npc, color);
		}
	}
}
