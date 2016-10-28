
using UnityEngine;
using System.Collections;

public class Player2Controller : MonoBehaviour {
	public class ControlsHint {
		public GameObject go;
		public TextMesh text_mesh;
		public Renderer renderer;
		public string text;
		public bool hidden;
	}

	GameManager game_manager = null;

	[System.NonSerialized] public Renderer renderer_;
	[System.NonSerialized] public Collider collider_;
	[System.NonSerialized] public Rigidbody rigidbody_;
	[System.NonSerialized] public NetworkView network_view;

	[System.NonSerialized] public float default_speed;
	[System.NonSerialized] public float speed;
	[System.NonSerialized] public float max_velocity_change = 0.9f;

	float jump_speed = 12.0f;
	float angular_speed = 4.0f;
	float max_angular_speed = 15.0f;

	public static int HAS_LOOKED = 0x1;
	public static int HAS_LOOKED_UP = 0x2;
	public static int HAS_MOVED = 0x4;
	public static int HAS_JUMPED = 0x8;
	public static int CAN_LOOK = 0x10;
	public static int CAN_MOVE = 0x20;
	public static int CAN_JUMP = 0x40;
	int control_flags = 0;

	bool jump_key = false;
	bool on_ground = true;
	float look_time = 0.0f;

	[System.NonSerialized] public ControlsHint hint = null;

	Transform transform_ref = null;
	Transform transform_ref_ = null;

	[System.NonSerialized] public Transform camera_ref;
	[System.NonSerialized] public Camera camera_;
	[System.NonSerialized] public UnityStandardAssets.ImageEffects.MotionBlur camera_blur_effect;
	[System.NonSerialized] public ColorGradingImageEffect camera_grading_effect;

	float camera_dist;
	float camera_theta;
	bool camera_looking_up;
	FadeImageEffect camera_fade = null;
	Vector3 camera_shake_offset = Vector3.zero;

	public enum CameraType {
		ALIVE,
		DEATH,
		ENDING,

		COUNT,
	};
	[System.NonSerialized] public CameraType camera_type;

	Transform mesh;
	[System.NonSerialized] public float mesh_radius = 1.0f;
	Animation anim;
	// [System.NonSerialized] public Renderer anim_renderer;

	AudioSource[] audio_sources = null;
	AudioSource sfx_audio_source = null;

	AudioSource walk_sfx_source;
	float walk_sfx_volume;
	float walk_sfx_volume_pos;

	bool first_missile_fired = false;
	float first_missile_fired_time = 0.0f;
	bool second_missile_fired = false;
	bool fade_out_triggered = false;

	[System.NonSerialized] public ParticleSystem ash_particles = null;
	[System.NonSerialized] public ParticleSystem dust_particles = null;

	[RPC]
	public void missile_fired(Vector3 position, Vector3 direction, float time) {
		if(first_missile_fired == false) {
			first_missile_fired = true;
			first_missile_fired_time = game_manager.total_playing_time;

			StartCoroutine(fire_first_missile(game_manager.env.target_point.pos, time));
		}
		else {
			second_missile_fired = true;
		}
	}

	IEnumerator wait_for_hit(float hit_time) {
		AudioClip sfx_missile = Audio.get_random_clip(game_manager.audio, Audio.Clip.MISSILE);
		sfx_audio_source.clip = sfx_missile;
		if(hit_time < sfx_missile.length) {
			sfx_audio_source.time = sfx_missile.length - hit_time;
		}
		else {
			yield return new WaitForSeconds(hit_time - sfx_audio_source.clip.length);
		}

		sfx_audio_source.Play();
		//NOTE: Assumes hit_time is greater than 0.2f!!
		StartCoroutine(Util.lerp_audio_volume(sfx_audio_source, 0.0f, 1.0f, 0.2f));
		while(sfx_audio_source.isPlaying) {
			yield return Util.wait_for_frame;
		}
	}

	IEnumerator fire_first_missile(Vector3 hit_pos, float hit_time) {
		yield return StartCoroutine(wait_for_hit(hit_time));

		bool in_blast_radius = (Vector3.Distance(hit_pos, mesh.position) < (Environment.EXPLOSION_RADIUS + mesh_radius));
		speed = default_speed * 0.5f;

		control_flags &= ~CAN_JUMP;

		if(!in_blast_radius && Settings.USE_DEATH_VIEW) {
			Environment.play_explosion(game_manager, this, game_manager.env, hit_pos);
			audio_sources[0].volume = 0.0f;

			for(int i = 0; i < game_manager.env.collectables.Length; i++) {
				Collectable.mark_as_used(game_manager.env.collectables[i], true);
			}

			Environment.play_screams(game_manager.env);

			dust_particles.Stop();
			dust_particles.Clear();
			ash_particles.Play();
		}

		if(!in_blast_radius) {
			AudioSource exp_source = game_manager.env.explosion.audio_source;
			exp_source.clip = Audio.get_random_clip(game_manager.audio, Audio.Clip.EXPLOSION_BIRDS);
			exp_source.Play();

			camera_fade.alpha = 1.0f;
			camera_blur_effect.enabled = true;

			float time_step = 0.01f;
			YieldInstruction wait_for_time_step = new WaitForSeconds(time_step);

			float d = 0.7f;
			float t = 0.0f;
			while(t < d) {
				float x = 1.0f - t / d;

				float rx = (Random.value - 0.5f) * 2.0f;
				float ry = (Random.value - 0.5f) * 2.0f;
				camera_shake_offset = new Vector3(rx, ry, 0.0f) * 0.25f * x;;

				t += time_step;

				bool flash = Random.value < (0.5f * x);
				camera_fade.alpha = flash ? 1.0f : 0.0f;
				if((flash || t >= d) && !camera_grading_effect.enabled) {
					camera_grading_effect.enabled = true;
				}

				yield return wait_for_time_step;
			}

			while(!second_missile_fired) {
				yield return Util.wait_for_frame;
			}

			yield return StartCoroutine(wait_for_hit(hit_time));
		}
		else {
			if(Settings.USE_DEATH_VIEW) {
				camera_type = CameraType.DEATH;

				camera_blur_effect.enabled = true;
				camera_blur_effect.blurAmount = 3.2f;
				camera_grading_effect.enabled = true;

				transform_ref_.forward = Vector3.up;

				camera_theta = 60.0f;
				camera_ref.position = mesh.position;
				camera_ref.rotation = transform_ref_.rotation;
				camera_.transform.localPosition = Vector3.zero;
				camera_.transform.localRotation = Quaternion.Euler(0.0f, camera_theta, 0.0f);

				rigidbody_.isKinematic = true;

				camera_fade.alpha = 1.0f;
				yield return new WaitForSeconds(3.0f);
				yield return StartCoroutine(FadeImageEffect.lerp_alpha(camera_fade, 0.0f, 5.0f));

				//TODO: SOUND!!

				while(!second_missile_fired) {
					yield return Util.wait_for_frame;
				}

				yield return StartCoroutine(wait_for_hit(hit_time));
			}
		}

		Environment env = game_manager.env;
		Environment.stop_screams(env);
		for(int i = 0; i < env.npcs.Length; i++) {
			NpcController.on_explosion(game_manager, env.npcs[i], env.target_point, hit_pos);
		}

		control_flags = 0;
		camera_fade.alpha = 1.0f;

		renderer_.enabled = false;
		ash_particles.Stop();
		ash_particles.Clear();

		yield return Util.wait_for_2s;

		camera_type = CameraType.ENDING;
		camera_ref.position = game_manager.env.target_point.pos + Vector3.up * mesh_radius;

		yield return StartCoroutine(FadeImageEffect.lerp_alpha(camera_fade, 0.0f, 5.0f));

		yield return new WaitForSeconds(10.0f);

		StartCoroutine(FadeImageEffect.lerp_alpha(camera_fade, 1.0f, 5.0f));
		yield return StartCoroutine(Util.lerp_audio_volume(audio_sources[1], 1.0f, 0.0f, 5.0f));
		audio_sources[0].Stop();
		audio_sources[1].Stop();

		game_manager.show_stats(camera_);
		yield return null;
	}

	IEnumerator fade_out_and_end() {
		first_missile_fired = true;
		second_missile_fired = true;

		yield return StartCoroutine(GameManager.set_world_brightness(game_manager, 1.0f, 0.0f, 5.0f));

		control_flags &= ~(CAN_LOOK | CAN_MOVE | CAN_JUMP);
		GameManager.set_world_brightness_(game_manager, 0.0f);

		StartCoroutine(Util.lerp_audio_volume(audio_sources[0], 1.0f, 0.0f, 2.0f));
		yield return StartCoroutine(Util.lerp_audio_volume(audio_sources[1], 1.0f, 0.0f, 2.0f));
		audio_sources[0].Stop();
		audio_sources[1].Stop();

		camera_fade.alpha = 0.0f;
		yield return StartCoroutine(FadeImageEffect.lerp_alpha(camera_fade, 1.0f, 5.0f));

		game_manager.show_stats(camera_);
		yield return null;
	}

	IEnumerator fade_in_hint_str(ControlsHint hint, string str) {
		float fade = 1.0f;
		float r_fade = 1.0f / fade;

		float t = 0.0f;
		while(t < 1.0f) {
			float a = t * t;
			string a_hex = ((int)(a * 255.0f)).ToString("X2");

			hint.text_mesh.text = hint.text + "<color=#FFFFFF" + a_hex + ">" + str + "</color>";

			t += Time.deltaTime * r_fade;
			yield return Util.wait_for_frame;
		}

		hint.text += str;
		hint.text_mesh.text = hint.text;
	}

	IEnumerator fade_in_and_start() {
		if(Settings.USE_TRANSITIONS) {
			camera_fade.alpha = 1.0f;

			yield return StartCoroutine(Util.lerp_audio_volume(audio_sources[1], 0.0f, 1.0f, 3.0f));
			StartCoroutine(Util.lerp_audio_volume(audio_sources[0], 0.0f, 1.0f, 2.0f));

			control_flags |= CAN_LOOK;
			if(!first_missile_fired) {
				control_flags |= CAN_JUMP;
			}
			speed = 0.0f;

			GameManager.set_world_brightness_(game_manager, 0.0f);
			yield return StartCoroutine(FadeImageEffect.lerp_alpha(camera_fade, 0.0f, 2.0f));

			speed = default_speed;

			GameObject hint_prefab = game_manager.env.transform.Find("Controls").gameObject;

			hint = new ControlsHint();
			hint.go = (GameObject)Object.Instantiate(hint_prefab, hint_prefab.transform.position, hint_prefab.transform.rotation);
			hint.go.SetActive(true);
			hint.renderer = hint.go.GetComponent<Renderer>();
			hint.text_mesh = hint.go.GetComponent<TextMesh>();
			hint.text_mesh.text = hint.text = "";
			hint.hidden = false;

			yield return StartCoroutine(GameManager.set_world_brightness(game_manager, 0.0f, 1.0f, 2.0f));
			dust_particles.Play();

			yield return StartCoroutine(fade_in_hint_str(hint, "WAZIRISTAN, PAKISTAN\n"));

			yield return StartCoroutine(fade_in_hint_str(hint, "LOOK: MOUSE\n"));
			while((control_flags & HAS_LOOKED) == 0) {
				yield return Util.wait_for_frame;
			}

			yield return StartCoroutine(fade_in_hint_str(hint, "LOOK UP: LEFT CLICK & HOLD\n"));
			while((control_flags & HAS_LOOKED_UP) == 0) {
				yield return Util.wait_for_frame;
			}

			yield return StartCoroutine(fade_in_hint_str(hint, "JUMP: SPACE\n"));
			while(!first_missile_fired && (control_flags & HAS_JUMPED) == 0) {
				yield return Util.wait_for_frame;
			}

			control_flags |= CAN_MOVE;

			yield return StartCoroutine(fade_in_hint_str(hint, "MOVE: W\n"));
			while((control_flags & HAS_MOVED) != 0) {
				yield return Util.wait_for_frame;
			}

			float t = 0.0f;
			while(t < 1.0f) {
				float a = 1.0f - t * t;
				hint.text_mesh.color = Util.new_color(Util.white, a);

				t += Time.deltaTime;
				yield return Util.wait_for_frame;
			}
			hint.go.SetActive(false);
		}
		else {
			control_flags |= CAN_LOOK | CAN_MOVE | CAN_JUMP;
			yield return StartCoroutine(Util.lerp_audio_volume(audio_sources[1], 0.0f, 1.0f, 3.0f));
			StartCoroutine(Util.lerp_audio_volume(audio_sources[0], 0.0f, 1.0f, 2.0f));
			camera_fade.alpha = 0.0f;
			dust_particles.Play();
		}

		yield return null;
	}

	public static void destroy(Player2Controller player2) {
		Destroy(player2.camera_ref.gameObject);
		Network.Destroy(player2.gameObject);
		if(player2.hint != null) {
			Destroy(player2.hint.go);
		}
	}

	public void set_walk_sfx_volume(float volume) {
		walk_sfx_volume = volume;
		walk_sfx_volume_pos = 0.0f;
	}

	void Start() {
		game_manager = GameObject.Find("GameManager").GetComponent<GameManager>();

		transform_ref = transform.Find("TransformRef");
		transform_ref_ = transform.Find("TransformRef_");

		default_speed = 12.0f;
		speed = default_speed;

		camera_ref = transform.Find("CameraRef");
		camera_ = camera_ref.Find("Camera").GetComponent<Camera>();
		camera_blur_effect = camera_.GetComponent<UnityStandardAssets.ImageEffects.MotionBlur>();
		camera_grading_effect = camera_.GetComponent<ColorGradingImageEffect>();

		camera_dist = camera_.transform.localPosition.magnitude;
		camera_theta = camera_.transform.localRotation.eulerAngles.x;
		camera_looking_up = false;
		camera_fade = camera_.GetComponent<FadeImageEffect>();

		camera_type = CameraType.ALIVE;

		mesh = transform.Find("Mesh");
		mesh_radius = mesh.localScale.y * 0.5f;

		anim = mesh.Find("Animation").GetComponent<Animation>();
		renderer_ = GetComponentInChildren<SkinnedMeshRenderer>();
		collider_ = mesh.GetComponent<Collider>();

		string material_id = Environment.get_pov_material_id(game_manager.env.pov);
		renderer_.material = (Material)Resources.Load("other_" + material_id + "_mat");

		network_view = GetComponent<NetworkView>();
		bool is_local_inst = network_view.isMine || (game_manager.connection_type == ConnectionType.OFFLINE);
		if(is_local_inst) {
			name = "Player2";
			game_manager.player2_inst = this;

			if(game_manager.network_player2_inst != null) {
				game_manager.network_player2_inst.renderer_.enabled = false;
				game_manager.network_player2_inst.collider_.enabled = false;

				Debug.Log("LOG: Hiding existing network player2!");
			}
		}
		else {
			name = "NetworkPlayer2";
			game_manager.network_player2_inst = this;

			if(game_manager.player_type != PlayerType.PLAYER1) {
				renderer_.enabled = false;
				collider_.enabled = false;

				Debug.Log("LOG: Hiding newly joined network player2!");
			}
		}

		rigidbody_ = GetComponent<Rigidbody>();
		rigidbody_.isKinematic = !is_local_inst;

		camera_.gameObject.SetActive(is_local_inst && game_manager.player_type == PlayerType.PLAYER2);
		if(camera_.gameObject.activeSelf) {
			camera_ref.transform.localPosition = new Vector3(0.0f, mesh_radius, 0.0f);
			camera_ref.transform.parent = null;

			audio_sources = new AudioSource[2];
			for(int i = 0; i < audio_sources.Length; i++) {
				AudioSource source = Util.new_audio_source(transform, "AudioSource" + i);
				source.clip = (AudioClip)Resources.Load("player2_track" + i);
				source.loop = true;
				source.volume = 0.0f;
				source.Play();

				audio_sources[i] = source;
			}

			sfx_audio_source = (new GameObject()).AddComponent<AudioSource>();
			sfx_audio_source.name = "SfxAudioSource";
			sfx_audio_source.transform.parent = transform;

			walk_sfx_source = Util.new_audio_source(transform, "WalkAudioSource");
			walk_sfx_source.clip = Audio.get_random_clip(game_manager.audio, Audio.Clip.PLAYER_WALK);
			walk_sfx_source.loop = true;
			walk_sfx_source.volume = 0.0f;
			walk_sfx_volume = 0.0f;
			walk_sfx_volume_pos = 0.0f;

			ash_particles = mesh.Find("AshParticleSystem").GetComponent<ParticleSystem>();
			dust_particles = mesh.Find("DustParticleSystem").GetComponent<ParticleSystem>();

			StartCoroutine(fade_in_and_start());
		}
		else {
			enabled = false;
		}
	}

	void Update() {
		walk_sfx_volume_pos += Time.deltaTime / 0.1f;
		walk_sfx_source.volume = Mathf.Lerp(walk_sfx_source.volume, walk_sfx_volume, walk_sfx_volume_pos);

		if(walk_sfx_source.volume == 0.0f && walk_sfx_source.isPlaying) {
			walk_sfx_source.Stop();
		}

		if(walk_sfx_source.volume > 0.0f && !walk_sfx_source.isPlaying) {
			walk_sfx_source.time = 0.0f;
			walk_sfx_source.Play();
		}

		if(!game_manager.connected_to_another_player() || game_manager.network_player1_inst == null) {
			float time_until_auto_fire = 180.0f;
			float hit_time = Settings.USE_TRANSITIONS ? 10.0f : 1.0f;

			if(!first_missile_fired) {
				bool auto_fire = game_manager.total_playing_time > time_until_auto_fire;
#if UNITY_EDITOR
				auto_fire = false;
				if(game_manager.get_key_down(KeyCode.Alpha1)) {
					auto_fire = true;
				}
#endif

				if(auto_fire) {
					if(!Settings.LAN_MODE) {
						game_manager.network_disconnect();
					}

					missile_fired(Vector3.up * GameManager.drone_height + game_manager.env.target_point.pos, -Vector3.up, hit_time);
				}
			}
			else if(!second_missile_fired) {
				if(game_manager.total_playing_time > (first_missile_fired_time + 30.0f)) {
					missile_fired(Vector3.up * GameManager.drone_height + game_manager.env.target_point.pos, -Vector3.up, hit_time);
				}
			}
		}
		else {
			if(!fade_out_triggered && !first_missile_fired) {
				float connected_playing_time = game_manager.total_playing_time;
				if(game_manager.network_player1_inst != null) {
					float time_since_player1_joined = Time.time - game_manager.network_player1_inst.join_time_stamp;
					if(time_since_player1_joined < connected_playing_time) {
						connected_playing_time = time_since_player1_joined;
					}
				}

				float max_playing_time = 240.0f;
				if(connected_playing_time > max_playing_time) {
					fade_out_triggered = true;
					StartCoroutine(fade_out_and_end());
				}
			}
		}

		float mouse_x = 0.0f;
		float mouse_y = 0.0f;

		if((control_flags & CAN_LOOK) != 0) {
			mouse_x = game_manager.get_axis("Mouse X");
			mouse_y = game_manager.get_axis("Mouse Y");

			if(mouse_x != 0.0f || mouse_y != 0.0f) {
				look_time += Time.deltaTime;
				if(look_time > 0.1f) {
					control_flags |= HAS_LOOKED;
				}
			}

			camera_looking_up = game_manager.get_key(KeyCode.Mouse0);
		}

		float rotation_y = Mathf.Clamp(mouse_x * angular_speed, -max_angular_speed, max_angular_speed);
		float slerp_t = 0.08f;

		switch(camera_type) {
			case CameraType.ALIVE: {
				transform_ref_.rotation = transform_ref_.rotation * Quaternion.AngleAxis(rotation_y, Vector3.up);

				camera_ref.position = transform.position + Vector3.up * mesh_radius;
				camera_ref.rotation = Quaternion.Slerp(camera_ref.rotation, transform_ref_.rotation, slerp_t);

				float rotation_x = angular_speed;
				if(camera_looking_up) {
					rotation_x *= -1.0f;
				}

				float min_theta = -30.0f;
				float max_theta = 15.0f;

				camera_theta = Mathf.Clamp(camera_theta + rotation_x, min_theta, max_theta);
				camera_.transform.localRotation = Quaternion.Slerp(camera_.transform.localRotation, Quaternion.Euler(camera_theta, 0.0f, 0.0f), slerp_t);

				float smooth_theta = camera_.transform.localRotation.eulerAngles.x;
				if(smooth_theta > 180.0f) {
					smooth_theta -= 360.0f;
				}

				if(camera_looking_up) {
					if(smooth_theta < Mathf.Lerp(max_theta, min_theta, 0.9f)) {
						control_flags |= HAS_LOOKED_UP;
					}
				}

				Vector3 camera_offset = -camera_.transform.forward * camera_dist;
				float horizon_theta = 0.0f;
				if(smooth_theta < horizon_theta) {
					float dist_t = 1.0f - (smooth_theta - min_theta) / (horizon_theta - min_theta);
					float dist_scale = Mathf.Lerp(1.0f, 0.5f, dist_t);

					Vector3 forward = camera_ref.rotation * Quaternion.Euler(horizon_theta, 0.0f, 0.0f) * Vector3.forward;
					camera_offset = -forward * (camera_dist * dist_scale);
				}

				camera_.transform.position = camera_ref.position + camera_offset;
				camera_.transform.localPosition += camera_shake_offset;

				//TODO: Fix camera clipping!!
				// RaycastHit hit_info;
				// if(Physics.Raycast(camera_ref.position, camera_offset.normalized, out hit_info, camera_offset.magnitude)) {
				// 	bool inside_collider = true;
				// 	// RaycastHit[] hit_array = Physics.RaycastAll(camera_.transform.position, camera_.transform.forward, camera_dist);
				// 	// for(int i = 0; i < hit_array.Length; i++) {
				// 	// 	if(hit_array[i].collider == hit_info.collider) {
				// 	// 		inside_collider = false;
				// 	// 		break;
				// 	// 	}
				// 	// }

				// 	if(inside_collider) {
				// 		Vector3 near_offset = camera_.nearClipPlane * camera_offset.normalized;
				// 		camera_.transform.position = hit_info.point + near_offset;
				// 	}
				// }

				break;
			}

			case CameraType.DEATH: {
				camera_theta += rotation_y * 0.25f;
				camera_theta += Mathf.Sin(Time.time) * 0.02f;
				camera_theta = Mathf.Clamp(camera_theta, -75.0f, 75.0f);

				camera_.transform.localRotation = Quaternion.Slerp(camera_.transform.localRotation, Quaternion.Euler(0.0f, camera_theta, 0.0f), slerp_t);

				break;
			}

			case CameraType.ENDING: {
				float rising_speed = 4.0f;
				camera_ref.position += Vector3.up * rising_speed * Time.deltaTime;

				camera_.transform.forward = game_manager.env.target_point.pos - camera_.transform.position;

				break;
			}
		}

		float min_ground_distance = 0.01f;

		RaycastHit hit_info_;
		if(Physics.SphereCast(mesh.position + Vector3.up * min_ground_distance, mesh_radius, -mesh.up, out hit_info_, min_ground_distance * 2.0f)) {
			transform_ref.rotation = transform_ref_.rotation;
			mesh.rotation = transform_ref.rotation;

			if((control_flags & CAN_JUMP) != 0) {
				if(game_manager.get_key_down(KeyCode.Space)) {
					jump_key = true;
					control_flags |= HAS_JUMPED;
				}
			}
		}
	}

	void FixedUpdate() {
		Vector3 acceleration = Vector3.zero;

		if(!camera_looking_up && (control_flags & CAN_MOVE) != 0) {
			if(game_manager.get_key(KeyCode.W)) {
				acceleration += Vector3.forward;
			}

			if(game_manager.get_key(KeyCode.S)) {
				acceleration -= Vector3.forward;
			}

			if(game_manager.get_key(KeyCode.A)) {
				acceleration -= Vector3.right;
			}

			if(game_manager.get_key(KeyCode.D)) {
				acceleration += Vector3.right;
			}

			acceleration = acceleration.normalized;
		}

		float min_ground_distance = 0.01f;

		RaycastHit hit_info_;
		if(Physics.SphereCast(mesh.position + Vector3.up * min_ground_distance, mesh_radius, -mesh.up, out hit_info_, min_ground_distance * 2.0f)) {
			if(!on_ground) {
				anim.Play("land");

				AudioClip clip = Audio.get_random_clip(game_manager.audio, Audio.Clip.PLAYER_LAND);
				Audio.play(game_manager.audio, clip, 1.0f, 0.9f + Random.value * 0.2f);
			}

			on_ground = true;
		}
		else {
			on_ground = false;
			set_walk_sfx_volume(0.0f);
		}

		if(on_ground) {
			if(acceleration == Vector3.zero) {
				anim.CrossFade("idle");
				set_walk_sfx_volume(0.0f);
			}
			else {
				control_flags |= HAS_MOVED;
				anim.CrossFade("walk");
				set_walk_sfx_volume(camera_.gameObject.activeSelf ? 1.0f : 0.0f);
			}
		}

		Vector3 velocity = transform_ref.TransformDirection(acceleration) * speed;

		Vector3 velocity_change = (velocity - rigidbody_.velocity);
		velocity_change.y = 0.0f;
		velocity_change = velocity_change.normalized * Mathf.Min(velocity_change.magnitude, max_velocity_change);

		rigidbody_.AddForce(velocity_change, ForceMode.VelocityChange);

		if(!camera_looking_up && jump_key) {
			rigidbody_.velocity += transform.up * jump_speed;
			anim.CrossFade("jump");
			set_walk_sfx_volume(0.0f);

			AudioClip clip = Audio.get_random_clip(game_manager.audio, Audio.Clip.PLAYER_JUMP);
			Audio.play(game_manager.audio, clip, 1.0f, 0.9f + Random.value * 0.2f);
		}

		jump_key = false;
	}
}
