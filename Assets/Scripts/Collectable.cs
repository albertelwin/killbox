
using UnityEngine;
using System.Collections;

public class Collectable {
	public Transform transform;
	public Renderer renderer;
	public ParticleSystem particle_system;

	public bool used = false;

	public Vector3 initial_pos;
	public float rnd_offset;

	public static Collectable new_inst(Transform transform) {
		Collectable collectable = new Collectable();
		collectable.transform = transform;

		collectable.renderer = transform.GetComponent<Renderer>();
		collectable.particle_system = transform.GetComponent<ParticleSystem>();

		float height_above_ground = 0.5f;
		collectable.transform.position += Vector3.up * height_above_ground;

		collectable.initial_pos = transform.position;
		collectable.rnd_offset = Random.value * Util.TAU;

		return collectable;
	}

	public static void reset(Collectable collectable) {
		collectable.transform.position = collectable.initial_pos;
		collectable.renderer.enabled = true;
		collectable.used = false;
	}

	public static void mark_as_used(Collectable collectable, bool emit_particles = false) {
		if(!collectable.used) {
			collectable.used = true;
			collectable.renderer.enabled = false;

			if(emit_particles) {
				collectable.particle_system.Emit(collectable.particle_system.maxParticles);
			}
		}
	}

	public static void update_collectables(GameManager game_manager, Collectable[] collectables) {
		Player2Controller player2 = game_manager.player2_inst;
		if(player2 != null) {
			Vector3 player_pos = player2.transform.position + Vector3.up * player2.mesh_radius;

			float time = Time.time;
			float dt = Time.deltaTime;

			float radius = 0.25f;
			float radius2 = radius * 2.0f;
			float collision_dist = radius + player2.mesh_radius;
			float collision_dist_sqr = collision_dist * collision_dist;

			float cull_radius = 75.0f;
			float cull_radius_sqr = cull_radius * cull_radius;

			float blend_radius = 25.0f;
			float r_blend_radius = 1.0f / blend_radius;
			float blend_start = (cull_radius - blend_radius) * r_blend_radius;

			for(int i = 0; i < collectables.Length; i++) {
				Collectable collectable = collectables[i];
				if(!collectable.used && collectable.renderer.isVisible) {
					Transform transform = collectable.transform;

					Vector3 dir_to_player = player_pos - transform.position;
					float player_dist_sqr = dir_to_player.x * dir_to_player.x + dir_to_player.y * dir_to_player.y + dir_to_player.z * dir_to_player.z;

					if(player_dist_sqr < cull_radius_sqr) {
						float player_dist = Mathf.Sqrt(player_dist_sqr);

						float y_blend = Mathf.Max(0.0f, player_dist * r_blend_radius - blend_start);

						float x = transform.position.x;
						float y = (collectable.initial_pos.y + Mathf.Sin(time + collectable.rnd_offset) * 0.1f) * (1.0f - y_blend) + transform.position.y * y_blend;
						float z = transform.position.z;

						if(player_dist_sqr < collision_dist_sqr) {
							Collectable.mark_as_used(collectable, true);
							AudioClip clip = Audio.get_random_clip(game_manager.audio, Audio.Clip.COLLECTABLE);
							Audio.play(game_manager.audio, clip);
						}
						else {
							float dist = Mathf.Max(0.0f, player_dist - 0.5f);
							float max_dist = 5.0f;
							float min_y = collectable.initial_pos.y - radius2;

							if(dist < max_dist && min_y < player_pos.y) {
								float distance_to_move = dt * (max_dist / dist);
								if(distance_to_move > dist) {
									distance_to_move = dist;
								}

								x += dir_to_player.x * distance_to_move;
								z += dir_to_player.z * distance_to_move;
							}
							else {
								float t = dt * 0.5f;
								x = x * (1.0f - t) + collectable.initial_pos.x * t;
								z = z * (1.0f - t) + collectable.initial_pos.z * t;
							}
						}

						transform.position = new Vector3(x, y, z);
					}
				}
			}
		}
	}
}
