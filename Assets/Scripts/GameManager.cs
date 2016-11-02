
/* TODO ✓

DOING:

TODO:
	Clear to solid color
	Missile camera contrast
	Shuffle npc colors
	Optimise pilot view (clear -> render camera feeds -> render ui)
	Camera clipping
	Dump password/kills/etc. to Google Drive
	Load scene async

DONE:
	1 deaths confirmed
	Log death count and session time
	Bucket physics
	Capsule colliders for adult npcs
	Npc blood
	Interrupt stop event on player react with npc
	Remove Mesh from NPC
	Remove walk speed etc. from MotionPathAgent
	Remove game dependencies from move_agent
	Use StringBuilder in console
	Remove birds

*/

using UnityEngine;
using UnityEngine.Analytics;
using System.Collections;
using System.Collections.Generic;

public enum PlayerType {
	PLAYER1,
	PLAYER2,
	NONE,
};

public enum ConnectionType {
	SERVER,
	CLIENT,
	OFFLINE,
	NONE,
};

public class GameManager : MonoBehaviour {
	public class SplashScreen {
		public Transform transform;

		public Renderer logo;
		public TextMesh name;
	}

	public class PlayerButton {
		public Transform transform;
		public Collider collider;

		public TextMesh text;

		public Renderer head;
		public Renderer body;
		public Renderer helmet;

		public static PlayerButton new_inst(Transform transform, string name) {
			PlayerButton inst = new PlayerButton();
			inst.transform = transform.Find(name);
			inst.collider = inst.transform.GetComponent<Collider>();

			inst.text = inst.transform.Find("Text").GetComponent<TextMesh>();

			inst.head = inst.transform.Find("Head").GetComponent<Renderer>();
			inst.body = inst.transform.Find("Body").GetComponent<Renderer>();
			Transform helmet = inst.transform.Find("Helmet");
			if(helmet) {
				inst.helmet = helmet.GetComponent<Renderer>();
			}

			return inst;
		}
	}

	public class MainScreen {
		public Transform transform;

		public Transform cursor;

		public PlayerButton player1;
		public PlayerButton player2;
	}

	public class LanScreen {
		public Transform transform;

		public Renderer killbox_movie;
		public Renderer[] player_movies;
	}

	public class EndScreen {
		public Transform transform;

		public Renderer outro_renderer;
		public MovieTexture outro_movie;
	}

	public class PauseScreen {
		public Transform transform;
		public GameObject game_object;

		public Camera camera;

		public Transform menu;
		public TextMesh continue_button;
		public TextMesh menu_button;
		public TextMesh exit_button;

		public Transform installation_menu;
		public TextMesh installation_continue_button;
		public TextMesh installation_restart_button;
		public TextMesh installation_text;

		public bool active;
		public float time;
		public float time_since_last_response;
	}

	public static string PREF_KEY_PLAY_COUNT = "killbox_play_count";

	public static float AUTO_PAUSE_TIME = 120.0f;
	public static float AUTO_RESTART_FROM_PAUSED_TIME = 30.0f;

	[System.NonSerialized] public NetworkView network_view;

	public SplashScreen splash_screen;
	public MainScreen main_screen;
	public LanScreen lan_screen;
	public EndScreen end_screen;
	public PauseScreen pause_screen;

	[System.NonSerialized] public TextMesh game_log;
	[System.NonSerialized] public TextMesh network_log;

	public Color player1_text_color = Color.green;
	public Color player2_text_color = Color.red;

	public Texture2D player1_head_texture = null;
	public Texture2D player1_alt_head_texture = null;

	public Texture2D player2_boy_texture = null;
	public Texture2D player2_girl_texture = null;
	public Texture2D[] player2_body_textures = null;
	float player2_texture_flip_rate = 60.0f;
	float player2_texture_flip_time = 0.0f;
	int player2_texture_id = 0;

	static string game_type_player1 = "killbox_player1_server";
	static string game_type_player2 = "killbox_player2_server";
	//TODO: Stable naming scheme!!
	// static string game_name = "killbox_online_server";
	static string game_name = "killbox_server_____";

	Camera menu_camera = null;
	AudioSource menu_audio_source = null;

	public static PlayerType persistent_player_type = PlayerType.NONE;

	// public static float drone_height = 160.0f;
	// public static float drone_radius = 60.0f;
	public static float drone_height = 240.0f;
	public static float drone_radius = 90.0f;

	[System.NonSerialized] public PlayerType player_type = PlayerType.NONE;
	[System.NonSerialized] public ConnectionType connection_type = ConnectionType.NONE;
	[System.NonSerialized] public bool created_player = false;
	[System.NonSerialized] public float total_playing_time = 0.0f;
	[System.NonSerialized] public bool first_missile_hit = false;
	[System.NonSerialized] public bool showing_stats = false;

	[System.NonSerialized] public Player1Controller player1_inst;
	[System.NonSerialized] public Player1Controller network_player1_inst;

	[System.NonSerialized] public Player2Controller player2_inst;
	[System.NonSerialized] public Player2Controller network_player2_inst;

	[System.NonSerialized] public Light sun;
	[System.NonSerialized] public float time_of_day;

	[System.NonSerialized] public Environment env;

	[System.NonSerialized] new public Audio audio;
	[System.NonSerialized] public AudioSource menu_sfx_source;

	[System.NonSerialized] public Transform player1_prefab = null;
	[System.NonSerialized] public Transform player2_prefab = null;
	[System.NonSerialized] public Transform missile_prefab = null;
	//TODO: Remove this!!
	[System.NonSerialized] public Transform explosion_prefab = null;

	public static GameManager get_inst() {
		return GameObject.Find("GameManager").GetComponent<GameManager>();
	}

	static public IEnumerator set_world_brightness(GameManager game_manager, float from, float to, float d = 1.0f) {
		float t = 0.0f;
		while(t < 1.0f) {
			t += Time.deltaTime * (1.0f / d);
			set_world_brightness_(game_manager, Mathf.Lerp(from, to, t));
			yield return Util.wait_for_frame;
		}

		set_world_brightness_(game_manager, to);
	}

	static public void set_world_brightness_(GameManager game_manager, float brightness) {
		Shader.SetGlobalFloat("_Brightness", brightness);
		RenderSettings.skybox.color = Util.sky * brightness;

		//TODO: Tidy this up somehow??
		if(game_manager != null) {
			// if(game_manager.env.controls_hint != null) {
			// 	game_manager.env.controls_hint.renderer.material.color = Util.white * brightness;
			// }

			if(game_manager.player2_inst != null) {
				game_manager.player2_inst.renderer_.material.SetFloat("_Brightness", 1.0f);
			}

			if(game_manager.network_player2_inst != null) {
				game_manager.network_player2_inst.renderer_.material.SetFloat("_Brightness", 1.0f);
			}
		}
	}

	static public void set_infrared_mode(bool enabled) {
		Shader.SetGlobalFloat("_InfraredAmount", enabled ? 1.0f : 0.0f);
	}

	public string get_input_str() {
		string input_str = "";
		if(!pause_screen.active) {
#if UNITY_STANDALONE_OSX
			//NOTE: Input.inputString doesn't work on Mac, bug in Unity :(
			bool shift_modifier = false;
			if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
				shift_modifier = true;
			}

			int first_char = shift_modifier ? 65 : 97;

			int first_key_index = (int)KeyCode.A;
			int last_key_index = (int)KeyCode.Z;
			for(int i = first_key_index; i < (last_key_index + 1); i++) {
				if(Input.GetKeyDown((KeyCode)i)) {
					char character_code = (char)(first_char + (i - first_key_index));
					input_str += character_code;
				}
			}

			for(int i = 0; i < Util.numeric_key_chars.Length; i++) {
				Util.KeyValue key_val = Util.numeric_key_chars[i];
				if(Input.GetKeyDown(key_val.key)) {
					input_str += key_val.val;
				}
			}

			if(Input.GetKeyDown(KeyCode.Return)) {
				input_str += '\n';
			}

			if(Input.GetKeyDown(KeyCode.Backspace)) {
				input_str += '\b';
			}
#else
			input_str = Input.inputString;
#endif

			if(input_str.Length > 0) {
				pause_screen.time_since_last_response = 0.0f;
			}
		}

		return input_str;
	}

	public bool get_key(KeyCode key_code) {
		bool val = false;
		if(!pause_screen.active) {
			val = Input.GetKey(key_code);
			if(val) {
				pause_screen.time_since_last_response = 0.0f;
			}
		}

		return val;
	}

	public bool get_key_down(KeyCode key_code) {
		bool val = false;
		if(!pause_screen.active) {
			val = Input.GetKeyDown(key_code);
			if(val) {
				pause_screen.time_since_last_response = 0.0f;
			}
		}

		return val;
	}

	public float get_axis(string axis_id) {
		float val = 0.0f;
		if(!pause_screen.active) {
			val = Input.GetAxis(axis_id);
			if(val != 0.0f) {
				pause_screen.time_since_last_response = 0.0f;
			}
		}

		return val;
	}

	public static void set_pause(GameManager game_manager, bool pause) {
		if(pause) {
			if(!game_manager.pause_screen.active) {
				game_manager.pause_screen.game_object.SetActive(true);
				game_manager.pause_screen.active = true;
				game_manager.pause_screen.time = 0.0f;
				game_manager.pause_screen.time_since_last_response = 0.0f;
			}

			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
		else {
			game_manager.pause_screen.game_object.SetActive(false);
			game_manager.pause_screen.active = false;
			game_manager.pause_screen.time = 0.0f;
			game_manager.pause_screen.time_since_last_response = 0.0f;

			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}

	public void show_stats(Camera camera) {
		StartCoroutine(show_stats_(camera));
	}

	public IEnumerator show_stats_(Camera camera) {
		bool show_info = persistent_player_type != PlayerType.NONE;
		if(show_info) {
			camera.gameObject.SetActive(false);

			menu_camera.gameObject.SetActive(true);
			main_screen.transform.gameObject.SetActive(false);
			lan_screen.transform.gameObject.SetActive(false);
			end_screen.transform.gameObject.SetActive(true);

			showing_stats = true;
			set_pause(this, false);

			end_screen.outro_renderer.enabled = true;
			end_screen.outro_movie.Stop();
			end_screen.outro_movie.Play();

			while(end_screen.outro_movie.isPlaying) {
				yield return Util.wait_for_frame;
			}

			end_screen.outro_renderer.enabled = false;
		}
		else {
			persistent_player_type = player_type == PlayerType.PLAYER1 ? PlayerType.PLAYER2 : PlayerType.PLAYER1;
		}

		game_over(show_info);
		yield return null;
	}

	public Ray move_cursor_(Transform cursor) {
		Ray cursor_ray = menu_camera.ScreenPointToRay(Input.mousePosition);
		Plane screen_plane = new Plane(-menu_camera.transform.forward, menu_camera.transform.position + menu_camera.transform.forward);
		float hit_distance;
		if(screen_plane.Raycast(cursor_ray, out hit_distance)) {
			cursor.position = cursor_ray.GetPoint(hit_distance);
			//TODO: Depth hack!!
			cursor.position -= Vector3.forward * 0.001f;
		}

		return cursor_ray;
	}

	public IEnumerator move_cursor(Transform cursor, float time) {
		float t = 0.0f;
		while(t < time) {
			move_cursor_(cursor);

			yield return Util.wait_for_frame;
		}
	}

	void create_player(ConnectionType connection_type) {
		if(created_player) {
			Assert.invalid_path("Something went wrong, player already exists!!");
		}
		else {
			this.connection_type = connection_type;
			created_player = true;

			string player_type_str = player_type == PlayerType.PLAYER1 ? "player1" : "player2";

			Analytics.CustomEvent("game_started", new Dictionary<string, object> {
				{ "player_type", player_type_str },
			});

			menu_camera.gameObject.SetActive(false);

			if(player_type == PlayerType.PLAYER1) {
				if(connection_type != ConnectionType.OFFLINE) {
					Network.Instantiate(player1_prefab, Vector3.zero, Quaternion.identity, 0);
				}
				else {
					Instantiate(player1_prefab, Vector3.zero, Quaternion.identity);
				}

				for(int i = 0; i < env.collectables.Length; i++) {
					Collectable.mark_as_used(env.collectables[i]);
				}

				QualitySettings.shadowCascades = 0;
				QualitySettings.shadowDistance = 400.0f;
			}
			else {
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = !(Cursor.lockState == CursorLockMode.Locked);

				//TODO: Pick this randomly from pool of points!!
				// Transform spawn_point = env.target_point.spawn_point;
				Transform spawn_point = env.target_point.spawn_points.GetChild(0);

				if(connection_type != ConnectionType.OFFLINE) {
					Network.Instantiate(player2_prefab, spawn_point.position, spawn_point.rotation, 0);
				}
				else {
					Instantiate(player2_prefab, spawn_point.position, spawn_point.rotation);
				}

				// QualitySettings.shadowCascades = 4;
				// QualitySettings.shadowDistance = 140.0f;
				QualitySettings.shadowCascades = 0;
				QualitySettings.shadowDistance = 100.0f;
			}

			Environment.apply_pov(this, env, player_type);
		}
	}

	public bool connected_to_another_player() {
		//TODO: This makes a 40 byte allocation somehow??
		return Network.connections.Length > 0;
	}

	public void network_disconnect() {
		Network.Disconnect();
		if(Network.isServer) {
			MasterServer.UnregisterHost();
		}
	}

	public void game_over(bool reset_persistent_state = false) {
		if(reset_persistent_state) {
			persistent_player_type = PlayerType.NONE;

			if(!Settings.LAN_MODE) {
				network_disconnect();
			}
		}

		if(player1_inst != null) {
			Player1Controller.destroy(player1_inst);
		}

		if(player2_inst != null) {
			Player2Controller.destroy(player2_inst);
		}

		Environment.reset(env);
		set_world_brightness_(this, 1.0f);
		set_infrared_mode(false);

		player_type = PlayerType.NONE;
		connection_type = ConnectionType.NONE;
		created_player = false;
		total_playing_time = 0.0f;
		first_missile_hit = false;
		showing_stats = false;

		game_log.gameObject.SetActive(false);
		set_pause(this, false);
		menu_camera.gameObject.SetActive(true);

		//TODO: We should really only be doing this when the game is completely over!!
		StartCoroutine(show_splash_screen());
	}

	void OnServerInitialized() {
		Debug.Log("Server created!");
		if(!Settings.LAN_MODE) {
			create_player(ConnectionType.SERVER);
		}
	}

	void OnConnectedToServer() {
		Debug.Log("Connected to server!");
		if(!Settings.LAN_MODE) {
			create_player(ConnectionType.CLIENT);
		}
	}

	void OnFailedToConnect(NetworkConnectionError error) {
		Debug.Log("ERROR: " + error);

		if(Settings.LAN_MODE && !Settings.LAN_SERVER_MACHINE) {
			Network.Connect(Settings.LAN_SERVER_IP, Settings.LAN_SERVER_PORT);
		}
	}

	void OnFailedToConnectToMasterServer(NetworkConnectionError error) {
		Debug.Log("Could not connect to master server!");
	}

	void OnPlayerConnected(NetworkPlayer network_player) {
		Debug.Log("Player connected!");
	}

	void OnPlayerDisconnected(NetworkPlayer network_player) {
		Debug.Log("Player disconnected!");
		Network.Disconnect();
		MasterServer.UnregisterHost();
	}

	void OnMasterServerEvent(MasterServerEvent evt) {
		if(evt == MasterServerEvent.HostListReceived) {
			HostData[] host_list = MasterServer.PollHostList();
			if(Network.isClient == false && Network.isServer == false) {
				if(host_list.Length > 0) {
					Debug.Log("Found server, now connecting...");

					Network.Connect(host_list[0]);
				}
				else {
					Debug.Log("Could not find an open server, now creating a new one...");

					string game_type = (player_type == PlayerType.PLAYER1) ? game_type_player1 : game_type_player2;

					Network.InitializeServer(1, 25002, !Network.HavePublicAddress());
					MasterServer.RegisterHost(game_type, game_name, "");
				}
			}
		}
		else {
			Debug.Log("Caught MasterServerEvent." + evt);
		}
	}

	IEnumerator start_game_from_main_screen(PlayerType player_type) {
		this.player_type = player_type;

		main_screen.player1.text.color = Util.black_no_alpha;
		main_screen.player2.text.color = Util.black_no_alpha;

		float fade_duration = 0.25f;

		if(Settings.USE_TRANSITIONS) {
			if(player_type == PlayerType.PLAYER1) {
				StartCoroutine(Util.lerp_material_alpha(main_screen.player2.head, 0.0f, fade_duration));
				StartCoroutine(Util.lerp_material_alpha(main_screen.player2.body, 0.0f, fade_duration));

				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.cursor.GetComponent<Renderer>(), 0.0f, fade_duration));
				main_screen.cursor.gameObject.SetActive(false);

				StartCoroutine(Util.lerp_audio_volume(menu_audio_source, 1.0f, 0.0f, 4.0f));

				yield return new WaitForSeconds(0.5f);
				StartCoroutine(Util.lerp_material_alpha(main_screen.player1.helmet, 0.0f));
				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.player1.body, 0.0f));
				yield return new WaitForSeconds(0.5f);
				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.player1.head, 0.0f));
			}
			else {
				StartCoroutine(Util.lerp_material_alpha(main_screen.player1.head, 0.0f, fade_duration));
				StartCoroutine(Util.lerp_material_alpha(main_screen.player1.helmet, 0.0f, fade_duration));
				StartCoroutine(Util.lerp_material_alpha(main_screen.player1.body, 0.0f, fade_duration));

				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.cursor.GetComponent<Renderer>(), 0.0f, fade_duration));
				main_screen.cursor.gameObject.SetActive(false);

				StartCoroutine(Util.lerp_audio_volume(menu_audio_source, 1.0f, 0.0f, 4.0f));

				yield return new WaitForSeconds(0.5f);
				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.player2.body, 0.0f));
				yield return new WaitForSeconds(0.5f);
				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.player2.head, 0.0f));
			}
		}
		else {
			menu_audio_source.volume = 0.0f;
		}

		StartCoroutine(start_game(player_type));
	}

	IEnumerator start_game(PlayerType player_type) {
		this.player_type = player_type;

		if(Settings.LAN_MODE) {
			ConnectionType connection_type = Settings.LAN_SERVER_MACHINE ? ConnectionType.SERVER : ConnectionType.CLIENT;
			if(!connected_to_another_player()) {
				connection_type = ConnectionType.OFFLINE;
				Debug.Log("LOG: No network connection, switching to offline game.");
			}

			create_player(connection_type);
		}
		else {
			if(Settings.FORCE_OFFLINE_MODE) {
				create_player(ConnectionType.OFFLINE);
			}
			else {
				if(persistent_player_type == PlayerType.NONE) {
					string request_game_type = (player_type == PlayerType.PLAYER1) ? game_type_player2 : game_type_player1;
					MasterServer.RequestHostList(request_game_type);
					Debug.Log("Looking for " + request_game_type + "...");

					float request_time_out = 5.0f;
					yield return new WaitForSeconds(request_time_out);
				}
				else {
					if(connected_to_another_player()) {
						ConnectionType connection_type = Network.isServer ? ConnectionType.SERVER : ConnectionType.CLIENT;
						create_player(connection_type);
					}
				}

				//TODO: Confirm host list hasn't been received!!
				if(!created_player) {
					Debug.Log("Request timed out, playing offline!");
					create_player(ConnectionType.OFFLINE);
				}
			}
		}

		yield return null;
	}

	IEnumerator show_splash_screen() {
		end_screen.transform.gameObject.SetActive(false);
		main_screen.transform.gameObject.SetActive(false);
		lan_screen.transform.gameObject.SetActive(false);

		if(Settings.LAN_MODE && Settings.LAN_FORCE_CONNECTION) {
			if(!connected_to_another_player() && persistent_player_type == PlayerType.NONE) {
				splash_screen.transform.gameObject.SetActive(false);

				if(Settings.LAN_SERVER_MACHINE) {
					Network.InitializeServer(1, Settings.LAN_SERVER_PORT, false);
				}
				else {
					Network.Connect(Settings.LAN_SERVER_IP, Settings.LAN_SERVER_PORT);
				}

				network_log.gameObject.SetActive(true);
				string wait_str = "ESTABLISHING LAN CONNECTION";
				int wait_index = 4;

				while(!connected_to_another_player()) {
					//TODO: Why does this crash on OSX??
#if UNITY_EDITOR || !UNITY_STANDALONE_OSX
					network_log.text += ".";

					if(wait_index > 3) {
						wait_index = 0;
						network_log.text = wait_str;
					}

					wait_index++;
#endif
					yield return Util.wait_for_500ms;
				}

				network_log.gameObject.SetActive(false);
			}
		}

		if(persistent_player_type == PlayerType.NONE) {
			if(Settings.USE_SPLASH) {
				splash_screen.transform.gameObject.SetActive(true);

				splash_screen.logo.material.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
				splash_screen.name.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);

				yield return new WaitForSeconds(0.5f);
				yield return StartCoroutine(Util.lerp_material_alpha(splash_screen.logo, 1.0f, 2.0f));
				yield return new WaitForSeconds(0.5f);
				yield return StartCoroutine(Util.lerp_text_alpha(splash_screen.name, 1.0f));

				yield return new WaitForSeconds(1.5f);

				StartCoroutine(Util.lerp_material_alpha(splash_screen.logo, 0.0f, 3.5f));
				StartCoroutine(Util.lerp_text_alpha(splash_screen.name, 0.0f, 3.5f));

				StartCoroutine(Util.lerp_audio_volume(menu_audio_source, 0.0f, 1.0f, 6.0f));
				yield return new WaitForSeconds(5.0f);

				splash_screen.transform.gameObject.SetActive(false);
			}
			else {
				Assert.is_true(!splash_screen.transform.gameObject.activeSelf);
				StartCoroutine(Util.lerp_audio_volume(menu_audio_source, menu_audio_source.volume, 1.0f));
			}

			if(Settings.LAN_MODE) {
				lan_screen.transform.gameObject.SetActive(true);

				PlayerType start_player = Settings.LAN_SERVER_MACHINE ? PlayerType.PLAYER1 : PlayerType.PLAYER2;

				lan_screen.player_movies[0].gameObject.SetActive(false);
				lan_screen.player_movies[1].gameObject.SetActive(false);

				Renderer killbox_movie = lan_screen.killbox_movie;
				killbox_movie.gameObject.SetActive(true);
				killbox_movie.material.color = Util.white_no_alpha;

#if !UNITY_EDITOR
				yield return Util.wait_for_3s;
#endif

				MovieTexture killbox_texture = (MovieTexture)killbox_movie.material.mainTexture;
				killbox_texture.loop = true;
				killbox_texture.Stop();
				killbox_texture.Play();

				StartCoroutine(Util.lerp_material_alpha(killbox_movie, 1.0f));

				while(Input.anyKey) {
					yield return Util.wait_for_frame;
				}

				bool reconnect = false;

				while(true) {
#if !UNITY_EDITOR
					if(Settings.LAN_FORCE_CONNECTION && !connected_to_another_player()) {
						reconnect = true;
						break;
					}
#endif

					//TODO: Do we want to remove certain keys - escape/etc.??
					if(Input.anyKey) {
#if UNITY_EDITOR
						if(Input.GetKey(KeyCode.Alpha1)) {
							start_player = PlayerType.PLAYER1;
						}
						else if(Input.GetKey(KeyCode.Alpha2)){
							start_player = PlayerType.PLAYER2;
						}
#endif
						break;
					}

					yield return Util.wait_for_frame;
				}

				if(reconnect) {
					StartCoroutine(show_splash_screen());
				}
				else {
					PlayerPrefs.SetInt(PREF_KEY_PLAY_COUNT, PlayerPrefs.GetInt(PREF_KEY_PLAY_COUNT) + 1);
					PlayerPrefs.Save();
					game_log.gameObject.SetActive(false);

					killbox_texture.Stop();
					killbox_movie.gameObject.SetActive(false);

					Renderer player_movie = null;
					if(start_player == PlayerType.PLAYER1) {
						player_movie = lan_screen.player_movies[0];
						lan_screen.player_movies[1].gameObject.SetActive(false);
					}
					else {
						player_movie = lan_screen.player_movies[1];
						lan_screen.player_movies[0].gameObject.SetActive(false);
					}
					player_movie.gameObject.SetActive(true);
					player_movie.material.color = Util.white;

					MovieTexture player_texture = (MovieTexture)player_movie.material.mainTexture;
					player_texture.loop = false;
					player_texture.Stop();
					player_texture.Play();

					menu_sfx_source.volume = 1.0f;

					while(player_texture.isPlaying) {
						yield return Util.wait_for_frame;
					}

					menu_sfx_source.volume = 0.0f;
					menu_audio_source.volume = 0.0f;

					StartCoroutine(start_game(start_player));
				}
			}
			else {
				main_screen.transform.gameObject.SetActive(true);

				main_screen.cursor.gameObject.SetActive(false);

				main_screen.player1.transform.gameObject.SetActive(true);
				main_screen.player1.helmet.material.color = Util.black_no_alpha;
				main_screen.player1.head.material.color = player1_text_color;
				main_screen.player1.body.material.color = Util.black_no_alpha;
				main_screen.player1.text.color = Util.black_no_alpha;

				main_screen.player2.transform.gameObject.SetActive(true);
				main_screen.player2.head.material.color = player2_text_color;
				main_screen.player2.body.material.color = Util.black_no_alpha;
				main_screen.player2.text.color = Util.black_no_alpha;

				if(Settings.USE_TRANSITIONS) {
					main_screen.player1.head.material.color = Util.new_color(player1_text_color, 0.0f);
					main_screen.player2.head.material.color = Util.new_color(player2_text_color, 0.0f);

					yield return StartCoroutine(Util.lerp_material_color(main_screen.player1.head, main_screen.player1.head.material.color, player1_text_color));
					yield return StartCoroutine(Util.lerp_material_color(main_screen.player2.head, main_screen.player2.head.material.color, player2_text_color));
				}

				Cursor.lockState = CursorLockMode.Locked;
				main_screen.cursor.gameObject.SetActive(true);
				StartCoroutine(Util.lerp_material_color(main_screen.cursor.GetComponent<Renderer>(), Util.white_no_alpha, Util.white));
				Cursor.lockState = CursorLockMode.None;

				while(true) {
					Ray cursor_ray = move_cursor_(main_screen.cursor);
					bool clicked = Input.GetKey(KeyCode.Mouse0);

					menu_sfx_source.volume = 0.0f;

					if(Util.raycast_collider(main_screen.player1.collider, cursor_ray)) {
						main_screen.player1.text.color = Color.white;
						main_screen.player1.helmet.material.color = player1_text_color;
						main_screen.player1.body.material.color = player1_text_color;

						if(clicked) {
							StartCoroutine(start_game_from_main_screen(PlayerType.PLAYER1));
							break;
						}
					}
					else {
						main_screen.player1.text.color = Util.black_no_alpha;
						main_screen.player1.helmet.material.color = Util.black_no_alpha;
						main_screen.player1.body.material.color = Util.black_no_alpha;
					}

					if(Util.raycast_collider(main_screen.player2.collider, cursor_ray)) {
						player2_texture_flip_time += Time.deltaTime * player2_texture_flip_rate;
						if(player2_texture_flip_time > 1.0f) {
							player2_texture_id++;
							if(player2_texture_id == player2_body_textures.Length) {
								Util.shuffle_array<Texture2D>(player2_body_textures);
								player2_texture_id = 0;
							}

							player2_texture_flip_time = 0.0f;
							main_screen.player2.body.material.mainTexture = player2_body_textures[player2_texture_id];
						}

						main_screen.player2.text.color = Color.white;
						main_screen.player2.body.material.color = player2_text_color;

						menu_sfx_source.volume = 1.0f;

						if(clicked) {
							main_screen.player2.body.material.mainTexture = Random.value > 0.5f ? player2_boy_texture : player2_girl_texture;
							menu_sfx_source.volume = 0.0f;
							StartCoroutine(start_game_from_main_screen(PlayerType.PLAYER2));
							break;
						}
					}
					else {
						main_screen.player2.text.color = Util.black_no_alpha;
						main_screen.player2.body.material.color = Util.black_no_alpha;
					}

					yield return Util.wait_for_frame;
				}
			}
		}
		else {
			Assert.is_true(!main_screen.transform.gameObject.activeSelf);
			Assert.is_true(!lan_screen.transform.gameObject.activeSelf);

			StartCoroutine(start_game(persistent_player_type));
		}

		yield return null;
	}

	void Awake() {
#if !UNITY_EDITOR
		//TODO: Make sure these are always in sync!!
		int quality_level = QualitySettings.GetQualityLevel();
		switch(quality_level) {
			case 0: {
				Settings.LAN_MODE = false;

				break;
			}

			case 1: {
				Settings.LAN_MODE = true;
				Settings.LAN_SERVER_MACHINE = true;

				break;
			}

			case 2: {
				Settings.LAN_MODE = true;
				Settings.LAN_SERVER_MACHINE = false;

				break;
			}
		}

		// Settings.USE_TRANSITIONS = true;

		QualitySettings.vSyncCount = 1;
		QualitySettings.antiAliasing = 4;
#endif

		network_view = GetComponent<NetworkView>();

		menu_camera = transform.Find("Camera").GetComponent<Camera>();
		menu_audio_source = GetComponent<AudioSource>();

		splash_screen = new SplashScreen();
		splash_screen.transform = transform.Find("Camera/Splash");
		splash_screen.logo = splash_screen.transform.Find("Logo").GetComponent<Renderer>();
		splash_screen.name = splash_screen.transform.Find("Name").GetComponent<TextMesh>();

		main_screen = new MainScreen();
		main_screen.transform = transform.Find("Camera/Main");
		main_screen.cursor = main_screen.transform.Find("Cursor");
		main_screen.player1 = PlayerButton.new_inst(main_screen.transform, "Player1Button");
		main_screen.player2 = PlayerButton.new_inst(main_screen.transform, "Player2Button");

		lan_screen = new LanScreen();
		lan_screen.transform = transform.Find("Camera/LAN");
		lan_screen.killbox_movie = lan_screen.transform.Find("KillboxMovie").GetComponent<Renderer>();
		lan_screen.player_movies = new Renderer[2];
		lan_screen.player_movies[0] = lan_screen.transform.Find("Player1Movie").GetComponent<Renderer>();
		lan_screen.player_movies[1] = lan_screen.transform.Find("Player2Movie").GetComponent<Renderer>();

		end_screen = new EndScreen();
		end_screen.transform = transform.Find("Camera/End");
		end_screen.outro_renderer = end_screen.transform.Find("OutroMovie").GetComponent<Renderer>();
		end_screen.outro_movie = (MovieTexture)end_screen.outro_renderer.material.mainTexture;

		pause_screen = new PauseScreen();
		pause_screen.transform = transform.Find("PauseCamera");
		pause_screen.game_object = pause_screen.transform.gameObject;
		pause_screen.camera = pause_screen.transform.GetComponent<Camera>();

		pause_screen.menu = pause_screen.transform.Find("Menu");
		pause_screen.continue_button = pause_screen.menu.Find("Continue").GetComponent<TextMesh>();
		pause_screen.menu_button = pause_screen.menu.Find("Menu").GetComponent<TextMesh>();
		pause_screen.exit_button = pause_screen.menu.Find("Exit").GetComponent<TextMesh>();

		pause_screen.installation_menu = pause_screen.transform.Find("InstallationMenu");
		pause_screen.installation_continue_button = pause_screen.installation_menu.Find("Continue").GetComponent<TextMesh>();
		pause_screen.installation_restart_button = pause_screen.installation_menu.Find("Restart").GetComponent<TextMesh>();
		pause_screen.installation_text = pause_screen.installation_menu.Find("Text").GetComponent<TextMesh>();

		pause_screen.active = false;
		pause_screen.time = 0.0f;
		pause_screen.time_since_last_response = 0.0f;
		pause_screen.menu.gameObject.SetActive(!Settings.INSTALLATION_BUILD);
		pause_screen.installation_menu.gameObject.SetActive(Settings.INSTALLATION_BUILD);
		pause_screen.installation_text.text = "";

		game_log = transform.Find("Camera/GameLog").GetComponent<TextMesh>();
		game_log.gameObject.SetActive(false);

		network_log = transform.Find("Camera/NetworkLog").GetComponent<TextMesh>();

		player1_prefab = Util.load_prefab("Player1Prefab");
		player2_prefab = Util.load_prefab("Player2Prefab");
		missile_prefab = Util.load_prefab("MissilePrefab");
		explosion_prefab = Util.load_prefab("ExplosionPrefab");

		player_type = PlayerType.NONE;

		sun = GameObject.Find("Sun").GetComponent<Light>();
		time_of_day = 0.0f;

		audio = Audio.new_inst();
		env = Environment.new_inst(this, GameObject.Find("Environment").transform);

		menu_sfx_source = Util.new_audio_source(transform, "MenuSfxSource");
		menu_sfx_source.clip = (AudioClip)Resources.Load("menu_hover");
		menu_sfx_source.loop = true;
		menu_sfx_source.volume = 0.0f;
		menu_sfx_source.pitch = 1.75f;
		menu_sfx_source.Play();

		Util.shuffle_array<Texture2D>(player2_body_textures);
	}

	void Start() {
		set_world_brightness_(this, 1.0f);
		set_infrared_mode(false);

		Cursor.visible = false;
		StartCoroutine(show_splash_screen());
	}

	void OnDestroy() {
		set_world_brightness_(this, 1.0f);
		set_infrared_mode(false);
	}

	void Update() {
		if(!connected_to_another_player()) {
			if(network_player1_inst != null) {
				Destroy(network_player1_inst.gameObject);
				network_player1_inst = null;
			}

			if(network_player2_inst != null) {
				Destroy(network_player2_inst.gameObject);
				network_player2_inst = null;
			}
		}

		Environment.update(this, env);

		//TODO: Test this!!
		if(created_player) {
			total_playing_time += Time.deltaTime;
		}

		if(Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Alpha0)) {
			game_log.gameObject.SetActive(!game_log.gameObject.activeSelf);
		}

		if(game_log.gameObject.activeInHierarchy) {
			float aspect_ratio = (float)Screen.width / (float)Screen.height;
			game_log.transform.position = new Vector3(-0.57375f * aspect_ratio, game_log.transform.position.y, game_log.transform.position.z);

			if(Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Backspace)) {
				PlayerPrefs.DeleteKey(PREF_KEY_PLAY_COUNT);
				PlayerPrefs.Save();
			}

			int play_count = PlayerPrefs.GetInt(PREF_KEY_PLAY_COUNT);
			game_log.text = string.Format("PLAYS: {0}", play_count);
		}

		if(Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Escape)) {
			game_over(true);
		}
		else {
			if(Input.GetKeyDown(KeyCode.Escape)) {
				if(player_type != PlayerType.NONE && created_player && !showing_stats) {
					set_pause(this, !pause_screen.active);
				}
				else {
					if(!Settings.INSTALLATION_BUILD) {
						Application.Quit();
					}
				}
			}
		}

#if UNITY_EDITOR
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
#endif

		if(pause_screen.active) {
			pause_screen.time += Time.deltaTime;

			Ray mouse_ray = pause_screen.camera.ScreenPointToRay(Input.mousePosition);

			Color active_color = Util.white;
			Color inactive_color = Util.new_color(Util.white, 0.5f);

			TextMesh continue_button = pause_screen.continue_button;
			TextMesh menu_button = pause_screen.menu_button;
			TextMesh exit_button = pause_screen.exit_button;

			if(Settings.INSTALLATION_BUILD) {
				continue_button = pause_screen.installation_continue_button;
				menu_button = pause_screen.installation_restart_button;
				exit_button = null;

				float time_remaining = AUTO_RESTART_FROM_PAUSED_TIME - pause_screen.time;
				if(time_remaining < 15.0f) {
					pause_screen.installation_text.text = "RESTARTING IN " + ((int)time_remaining + 1);

					int dot_count = (int)(pause_screen.time * 4.0f) % 4;
					for(int i = 0; i < dot_count; i++) {
						pause_screen.installation_text.text += ".";
					}

					if(time_remaining <= 0.0f) {
						game_over(true);
					}
				}
				else {
					pause_screen.installation_text.text = "";
				}

				pause_screen.installation_text.color = active_color;
			}

			continue_button.color = inactive_color;
			menu_button.color = inactive_color;
			if(exit_button) {
				exit_button.color = inactive_color;
			}

			if(pause_screen.active) {
				RaycastHit hit_info;
				if(Physics.Raycast(mouse_ray, out hit_info, 2.0f)) {
					Transform button = hit_info.collider.transform.parent;
					if(button == continue_button.transform) {
						continue_button.color = active_color;

						if(Input.GetKeyDown(KeyCode.Mouse0)) {
							set_pause(this, false);
						}
					}
					else if(button == menu_button.transform) {
						menu_button.color = active_color;

						if(Input.GetKeyDown(KeyCode.Mouse0)) {
							game_over(true);
						}
					}
					else if(button == exit_button.transform && exit_button != null) {
						exit_button.color = active_color;

						if(Input.GetKey(KeyCode.Mouse0)) {
							Application.Quit();
						}
					}
				}
			}
		}
		else {
			if(created_player) {
				pause_screen.time_since_last_response += Time.deltaTime;
				if(pause_screen.time_since_last_response >= AUTO_PAUSE_TIME) {
					set_pause(this, true);
				}
			}
		}

		// Debug.Log(pause_screen.time_since_last_response.ToString());
	}
}
