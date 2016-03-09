using UnityEngine;
using System.Collections;

public class Collectable {
	static float HEIGHT_ABOVE_GROUND = 0.5f;

	public Transform transform;
	public Renderer renderer;
	public Collider collider;
	public ParticleSystem particle_system = null;
	public AudioSource audio_source = null;

	public bool dropped = false;
	public bool used = false;

	public Vector3 initial_pos;
	public float rnd_offset;

	public static Collectable new_inst(Transform transform) {
		Collectable collectable = new Collectable();
		collectable.transform = transform;

		collectable.renderer = transform.GetComponent<Renderer>();
		collectable.collider = transform.GetComponent<Collider>();
		collectable.particle_system = transform.GetComponent<ParticleSystem>();
		collectable.audio_source = transform.GetComponent<AudioSource>();
		collectable.audio_source.loop = false;
		
		collectable.initial_pos = transform.position;
		collectable.rnd_offset = Random.value * Util.TAU;

		return collectable;
	}

	public static void reset(Collectable collectable) {
		collectable.transform.position = collectable.initial_pos;
		collectable.renderer.enabled = true;
		collectable.collider.enabled = true;
		collectable.dropped = false;
		collectable.used = false;
	}

	public static void mark_as_used(Collectable collectable, bool emit_particles = false) {
		if(!collectable.used) {
			collectable.used = true;
			collectable.renderer.enabled = false;
			collectable.collider.enabled = false;

			if(emit_particles) {
				collectable.particle_system.Emit(collectable.particle_system.maxParticles);
			}			
		}
	}

	public static void update(GameManager game_manager, Collectable collectable) {
		if(!collectable.used) {
			float pos_y = collectable.initial_pos.y + HEIGHT_ABOVE_GROUND;
			Vector3 pos = new Vector3(collectable.transform.position.x, pos_y, collectable.transform.position.z);

			Player2Controller player2 = game_manager.player2_inst;
			if(player2 != null) {
				Vector3 player_pos = player2.transform.position;

				float player_dist = Vector3.Distance(player_pos + Vector3.up * player2.mesh_radius, pos) - (collectable.transform.localScale.y * 0.5f + player2.mesh_radius);
				if(player_dist < 0.0f) {
					mark_as_used(collectable, true);

					collectable.audio_source.clip = Audio.get_random_clip(game_manager.audio, Audio.Clip.COLLECTABLE);
					collectable.audio_source.Play();
				}
				else {
					bool player_above = (player_pos.y + collectable.transform.localScale.y) >= collectable.initial_pos.y;
					float dist = Mathf.Max(Vector3.Distance(player_pos, pos) - 0.5f, 0.0f);
					float max_dist = 5.0f;

					if(dist < max_dist && player_above) {
						Vector3 direction_to_player = player_pos - pos;
						direction_to_player.y = 0.0f;

						float distance_to_move = Time.deltaTime * (max_dist / dist);
						pos += direction_to_player * Mathf.Min(dist, distance_to_move);
					}
					else {
						pos = Vector3.Lerp(pos, new Vector3(collectable.initial_pos.x, pos_y, collectable.initial_pos.z), Time.deltaTime * 0.5f);
					}
				}
			}

			float y_offset = Mathf.Sin(Time.time + collectable.rnd_offset) * 0.1f;
			pos.y += y_offset;
			collectable.transform.position = pos;
		}
	}
}
