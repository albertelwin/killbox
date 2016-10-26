
using UnityEngine;
using System.Collections;

public class Collectable {
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

		float height_above_ground = 0.5f;
		collectable.transform.position += Vector3.up * height_above_ground;

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
}
