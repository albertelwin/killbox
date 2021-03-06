
/* TODO ✓

DOING:

TODO:
	Camera clipping
	Dump password/kills/etc. to Google Drive

DONE:

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
		public GameObject go;
		public Transform transform;
		public Renderer biome;
		public Renderer killbox;
	}

	public class PlayerButton {
		public Transform transform;
		public Collider collider;

		public TextMesh text;

		public Renderer head;
		public Renderer body;
		public Renderer helmet;

		public static PlayerButton new_inst(Transform parent, string name) {
			PlayerButton inst = new PlayerButton();
			inst.transform = parent.Find(name);
			inst.collider = inst.transform.GetComponent<Collider>();

			inst.text = inst.transform.Find("Text").GetComponent<TextMesh>();

			inst.head = inst.transform.Find("Head").GetComponent<Renderer>();
			Transform body = inst.transform.Find("Body");
			if(body) {
				inst.body = body.GetComponent<Renderer>();
			}
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

		public Renderer info_page;
		public PlayerButton info;
		public Collider hyperlink;
	}

	public class LanScreen {
		public Transform transform;

		public Renderer intro_renderer;
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

	public static float AUTO_PAUSE_TIME = 90.0f;
	public static float AUTO_RESTART_FROM_PAUSED_TIME = 30.0f;

	public static int VSYNC_COUNT = 1;

	[System.NonSerialized] public NetworkView network_view;

	public SplashScreen splash_screen;
	public MainScreen main_screen;
	public LanScreen lan_screen;
	public EndScreen end_screen;
	public PauseScreen pause_screen;

	[System.NonSerialized] public TextMesh game_log;
	[System.NonSerialized] public TextMesh network_log;
	[System.NonSerialized] public TextMesh loading_log;

	public Color player1_text_color = Color.red;
	public Color player2_text_color = Color.green;

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
	static string game_name = "killbox_server_v" + Settings.VERSION;

	Camera menu_camera = null;
	AudioSource bees_source = null;
	Coroutine bees_coroutine = null;

	public static bool splash_shown = false;
	public static PlayerType last_player_type = PlayerType.NONE;

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
	[System.NonSerialized] public AudioSource[] menu_player_sources;

	[System.NonSerialized] public Transform player1_prefab = null;
	[System.NonSerialized] public Transform player2_prefab = null;
	//TODO: Remove this!!
	[System.NonSerialized] public Transform explosion_prefab = null;

	static public IEnumerator set_world_brightness(GameManager game_manager, float from, float to, float d = 1.0f, Camera camera = null) {
		float t = 0.0f;
		while(t < 1.0f) {
			t += Time.deltaTime * (1.0f / d);
			set_world_brightness_(game_manager, Mathf.Lerp(from, to, t), camera);
			yield return Util.wait_for_frame;
		}

		set_world_brightness_(game_manager, to, camera);
	}

	static public void set_world_brightness_(GameManager game_manager, float brightness, Camera camera = null) {
		Shader.SetGlobalFloat("_Brightness", brightness);
		if(RenderSettings.skybox != null) {
			RenderSettings.skybox.color = Util.sky * brightness;
		}

		if(camera != null) {
			camera.backgroundColor = Util.sky * brightness;
		}

		if(game_manager != null) {
			if(game_manager.player2_inst != null) {
				if(game_manager.player2_inst.renderer_ != null) {
					game_manager.player2_inst.renderer_.material.SetFloat("_Brightness", 1.0f);
				}
			}

			if(game_manager.network_player2_inst != null) {
				if(game_manager.network_player2_inst.renderer_ != null) {
					game_manager.network_player2_inst.renderer_.material.SetFloat("_Brightness", 1.0f);
				}
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

	public void show_stats() {
		StartCoroutine(show_stats_());
	}

	public IEnumerator show_stats_() {
		bool show_info = last_player_type != PlayerType.NONE;
		if(show_info) {
			menu_camera.gameObject.SetActive(true);
			main_screen.transform.gameObject.SetActive(false);
			lan_screen.transform.gameObject.SetActive(false);
			end_screen.transform.gameObject.SetActive(true);

			showing_stats = true;
			set_pause(this, false);

			QualitySettings.vSyncCount = 0;

			end_screen.outro_renderer.enabled = true;
			end_screen.outro_renderer.material.color = Util.white;

			MovieTexture movie = end_screen.outro_movie;
			movie.loop = false;
			movie.Stop();
			movie.Play();
			while(movie.isPlaying) {
				yield return Util.wait_for_frame;
			}
			movie.Stop();

			end_screen.outro_renderer.enabled = false;

#if !UNITY_EDITOR
			QualitySettings.vSyncCount = VSYNC_COUNT;
#endif
		}
		else {
			last_player_type = player_type == PlayerType.PLAYER1 ? PlayerType.PLAYER2 : PlayerType.PLAYER1;
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

			PlayerPrefs.SetInt(PREF_KEY_PLAY_COUNT, PlayerPrefs.GetInt(PREF_KEY_PLAY_COUNT) + 1);
			PlayerPrefs.Save();

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

				QualitySettings.shadowDistance = 450.0f;
			}
			else {
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = !(Cursor.lockState == CursorLockMode.Locked);

				Transform spawn_point = env.target_point.spawn_points.GetChild(0);

				if(connection_type != ConnectionType.OFFLINE) {
					Network.Instantiate(player2_prefab, spawn_point.position, spawn_point.rotation, 0);
				}
				else {
					Instantiate(player2_prefab, spawn_point.position, spawn_point.rotation);
				}

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
			last_player_type = PlayerType.NONE;

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

	public static void stop_bees_coroutine(GameManager game_manager) {
		if(game_manager.bees_coroutine != null) {
			game_manager.StopCoroutine(game_manager.bees_coroutine);
			game_manager.bees_coroutine = null;
		}
	}

	IEnumerator start_game_from_main_screen(PlayerType player_type) {
		this.player_type = player_type;

		main_screen.player1.text.color = Util.black_no_alpha;
		main_screen.player2.text.color = Util.black_no_alpha;
		main_screen.info.text.color = Util.black_no_alpha;

		float fade_duration = 0.25f;

		if(Settings.USE_TRANSITIONS) {
			if(player_type == PlayerType.PLAYER1) {
				Util.lerp_alpha(this, main_screen.player2.head, 0.0f, fade_duration);
				Util.lerp_alpha(this, main_screen.player2.body, 0.0f, fade_duration);
			}
			else {
				Util.lerp_alpha(this, main_screen.player1.head, 0.0f, fade_duration);
				Util.lerp_alpha(this, main_screen.player1.helmet, 0.0f, fade_duration);
				Util.lerp_alpha(this, main_screen.player1.body, 0.0f, fade_duration);
			}
			Util.lerp_alpha(this, main_screen.info.head, 0.0f, fade_duration);
			yield return Util.lerp_alpha(this, main_screen.cursor.GetComponent<Renderer>(), 0.0f, fade_duration);
			main_screen.cursor.gameObject.SetActive(false);

			stop_bees_coroutine(this);
			bees_coroutine = StartCoroutine(Util.lerp_audio_volume(bees_source, bees_source.volume, 0.0f, 4.0f * bees_source.volume));
			if(!bees_source.isPlaying) {
				bees_source.Play();
			}

			if(player_type == PlayerType.PLAYER1) {
				yield return new WaitForSeconds(0.5f);
				Util.lerp_alpha(this, main_screen.player1.helmet, 0.0f);
				yield return Util.lerp_alpha(this, main_screen.player1.body, 0.0f);
				yield return new WaitForSeconds(0.5f);
				yield return Util.lerp_alpha(this, main_screen.player1.head, 0.0f);
			}
			else {
				yield return new WaitForSeconds(0.5f);
				yield return Util.lerp_alpha(this, main_screen.player2.body, 0.0f);
				yield return new WaitForSeconds(0.5f);
				yield return Util.lerp_alpha(this, main_screen.player2.head, 0.0f);
			}
		}
		else {
			stop_bees_coroutine(this);
			bees_source.volume = 0.0f;
			yield return Util.wait_for_frame;
			bees_source.Stop();
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
				if(last_player_type == PlayerType.NONE) {
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
			if(!connected_to_another_player() && last_player_type == PlayerType.NONE) {
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
					yield return Util.wait_for_250ms;
				}

				network_log.gameObject.SetActive(false);
			}
		}

		if(last_player_type == PlayerType.NONE) {
			{
				loading_log.gameObject.SetActive(true);

				string wait_str = "LOADING";
				int max_len = wait_str.Length + 3;

				loading_log.text = wait_str;

				while(true) {
					if(audio.load_state == Audio.LoadState.LOADED) {
						break;
					}
					else {
						if(loading_log.text.Length >= max_len) {
							loading_log.text = wait_str;
						}
						else {
#if UNITY_EDITOR || !UNITY_STANDALONE_OSX
							loading_log.text += ".";
#endif
						}

						yield return Util.wait_for_250ms;
					}
				}

				loading_log.gameObject.SetActive(false);
			}

			bool show_splash = Settings.USE_SPLASH && !splash_shown;
			splash_shown = true;

			if(show_splash) {
				splash_screen.transform.gameObject.SetActive(true);
				splash_screen.biome.enabled = false;
				splash_screen.killbox.enabled = false;

				{
					splash_screen.biome.enabled = true;
					splash_screen.biome.material.color = Util.white_no_alpha;

					yield return new WaitForSeconds(0.5f);
					yield return Util.lerp_alpha(this, splash_screen.biome, 1.0f, 2.0f);

					yield return new WaitForSeconds(1.5f);

					// Util.lerp_alpha(this, splash_screen.biome, 0.0f, 3.5f);
					yield return Util.lerp_alpha(this, splash_screen.biome, 0.0f, 2.0f);
					splash_screen.biome.enabled = false;
				}

				{
					splash_screen.killbox.enabled = true;
					splash_screen.killbox.material.color = Util.white_no_alpha;

					yield return new WaitForSeconds(0.5f);
					yield return Util.lerp_alpha(this, splash_screen.killbox, 1.0f, 2.0f);

					yield return new WaitForSeconds(1.5f);

					Util.lerp_alpha(this, splash_screen.killbox, 0.0f, 3.5f);
				}
			}

			stop_bees_coroutine(this);
			bees_coroutine = StartCoroutine(Util.lerp_audio_volume(bees_source, 0.0f, 1.0f, 5.0f));
			if(!bees_source.isPlaying) {
				bees_source.Play();
			}

			if(show_splash) {
				yield return new WaitForSeconds(4.0f);
				splash_screen.transform.gameObject.SetActive(false);
			}

			// if(Settings.LAN_MODE) {
			{
				if(Settings.LAN_MODE || !show_splash) {
					lan_screen.transform.gameObject.SetActive(true);

					PlayerType start_player = Settings.LAN_SERVER_MACHINE ? PlayerType.PLAYER1 : PlayerType.PLAYER2;

					QualitySettings.vSyncCount = 0;

					Renderer intro_renderer = lan_screen.intro_renderer;
					intro_renderer.gameObject.SetActive(true);
					intro_renderer.material.color = Util.white;

					MovieTexture intro_movie = (MovieTexture)intro_renderer.material.mainTexture;
					intro_movie.loop = true;
					intro_movie.Stop();
					intro_movie.Play();

					yield return Util.wait_for_2s;

					while(Input.anyKey) {
						yield return Util.wait_for_frame;
					}

					bool reconnect = false;

					while(true) {
#if !UNITY_EDITOR
						if(Settings.LAN_MODE && Settings.LAN_FORCE_CONNECTION && !connected_to_another_player()) {
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
						Assert.is_true(Settings.LAN_MODE);

						stop_bees_coroutine(this);
						bees_source.volume = 0.0f;
						yield return Util.wait_for_frame;
						bees_source.Stop();

						StartCoroutine(show_splash_screen());
					}
					else {
						game_log.gameObject.SetActive(false);

						if(Settings.LAN_MODE && Settings.USE_TRANSITIONS) {
							stop_bees_coroutine(this);
							bees_coroutine = StartCoroutine(Util.lerp_audio_volume(bees_source, bees_source.volume, 0.0f, 5.0f, true));
						}
						yield return Util.lerp_alpha(this, intro_renderer, 0.0f, 3.0f);
						intro_movie.Stop();
						intro_renderer.gameObject.SetActive(false);

						if(Settings.LAN_MODE) {
							StartCoroutine(start_game(start_player));
						}
					}

#if !UNITY_EDITOR
					QualitySettings.vSyncCount = VSYNC_COUNT;
#endif
				}

				if(!Settings.LAN_MODE) {
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

					Color info_color = new Color(0.302f, 0.302f, 0.302f, 1.0f);

					main_screen.info.transform.gameObject.SetActive(true);
					main_screen.info.head.material.color = Util.new_color(info_color, 0.0f);
					main_screen.info.text.color = Util.black_no_alpha;
					main_screen.info.text.text = "ABOUT KILLBOX";

					main_screen.info_page.enabled = true;
					Util.set_renderer_alpha(main_screen.info_page, 0.0f);

					if(Settings.USE_TRANSITIONS) {
						main_screen.player1.head.material.color = Util.new_color(player1_text_color, 0.0f);
						main_screen.player2.head.material.color = Util.new_color(player2_text_color, 0.0f);

						yield return Util.lerp_color(this, main_screen.player1.head, main_screen.player1.head.material.color, player1_text_color);
						yield return Util.lerp_color(this, main_screen.player2.head, main_screen.player2.head.material.color, player2_text_color);
						yield return Util.lerp_alpha(this, main_screen.info.head, 1.0f);
					}

					Cursor.lockState = CursorLockMode.Locked;
					main_screen.cursor.gameObject.SetActive(true);
					Util.lerp_color(this, main_screen.cursor.GetComponent<Renderer>(), Util.white_no_alpha, Util.white);
					Cursor.lockState = CursorLockMode.None;

					bool mouse_down = Input.GetKey(KeyCode.Mouse0);

					object current_screen = main_screen;

					object fade_screen = null;
					float fade_time = 0.0f;

					while(true) {
						Ray cursor_ray = move_cursor_(main_screen.cursor);

						bool mouse_was_down = mouse_down;
						mouse_down = Input.GetKey(KeyCode.Mouse0);
						bool clicked = mouse_down && !mouse_was_down;

						for(int i = 0; i < menu_player_sources.Length; i++) {
							menu_player_sources[i].volume = 0.0f;
						}

						bool player1_hover = Util.raycast_collider(main_screen.player1.collider, cursor_ray);
						bool player2_hover = Util.raycast_collider(main_screen.player2.collider, cursor_ray);
						bool info_hover = Util.raycast_collider(main_screen.info.collider, cursor_ray);
						bool hyperlink_hover = Util.raycast_collider(main_screen.hyperlink, cursor_ray);

// #if UNITY_EDITOR
						if(Input.GetKey(KeyCode.Alpha1)) {
							player1_hover = true;
							clicked = true;
						}
						if(Input.GetKey(KeyCode.Alpha2)) {
							player2_hover = true;
							clicked = true;
						}
// #endif

						if(fade_screen != null) {
							fade_time += Time.deltaTime;

							bool end_fade = false;

							if(fade_screen == main_screen) {
								float fade_out_time = fade_time;
								if(fade_out_time > 0.0f) {
									float fade_out_alpha = 1.0f - fade_out_time;
									if(fade_out_alpha < 0.0f) {
										fade_out_alpha = 0.0f;
									}

									Util.set_renderer_alpha(main_screen.info_page, fade_out_alpha);

									float fade_in_time = fade_out_time - 1.0f;
									if(fade_in_time > 0.0f) {
										float fade_in_alpha = fade_in_time;
										if(fade_in_alpha > 1.0f) {
											fade_in_alpha = 1.0f;

											main_screen.info.text.text = "ABOUT KILLBOX";

											end_fade = true;
										}

										Util.set_renderer_alpha(main_screen.player1.head, fade_in_alpha);
										Util.set_renderer_alpha(main_screen.player2.head, fade_in_alpha);
									}
								}
							}
							else if(fade_screen == main_screen.info_page) {
								float fade_out_time = fade_time;
								if(fade_out_time > 0.0f) {
									float fade_out_alpha = 1.0f - fade_out_time;
									if(fade_out_alpha < 0.0f) {
										fade_out_alpha = 0.0f;
									}

									Util.set_renderer_alpha(main_screen.player1.head, fade_out_alpha);
									Util.set_renderer_alpha(main_screen.player2.head, fade_out_alpha);

									float fade_in_time = fade_out_time - 1.0f;
									if(fade_in_time > 0.0f) {
										float fade_in_alpha = fade_in_time;
										if(fade_in_alpha > 1.0f) {
											fade_in_alpha = 1.0f;

											main_screen.info.text.text = "BACK";

											end_fade = true;
										}

										Util.set_renderer_alpha(main_screen.info_page, fade_in_alpha);
									}
								}
							}
							else {
								Assert.invalid_path();
							}

							if(end_fade) {
								current_screen = fade_screen;
								fade_screen = null;
								fade_time = 0.0f;
							}
						}
						else {
							if(current_screen == main_screen) {
								if(player1_hover) {
									main_screen.player1.text.color = Util.white;
									main_screen.player1.helmet.material.color = player1_text_color;
									main_screen.player1.body.material.color = player1_text_color;

									menu_player_sources[0].volume = 1.0f;

									if(clicked) {
										menu_player_sources[0].volume = 0.0f;
										StartCoroutine(start_game_from_main_screen(PlayerType.PLAYER1));
										break;
									}
								}
								else {
									main_screen.player1.text.color = Util.black_no_alpha;
									main_screen.player1.helmet.material.color = Util.black_no_alpha;
									main_screen.player1.body.material.color = Util.black_no_alpha;
								}

								if(player2_hover) {
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

									menu_player_sources[1].volume = 1.0f;

									if(clicked) {
										main_screen.player2.body.material.mainTexture = Random.value > 0.5f ? player2_boy_texture : player2_girl_texture;
										menu_player_sources[1].volume = 0.0f;
										StartCoroutine(start_game_from_main_screen(PlayerType.PLAYER2));
										break;
									}
								}
								else {
									main_screen.player2.text.color = Util.black_no_alpha;
									main_screen.player2.body.material.color = Util.black_no_alpha;
								}

								if(info_hover) {
									main_screen.info.text.color = Util.white;

									if(clicked) {
										main_screen.info.text.color = Util.white_no_alpha;
										fade_screen = main_screen.info_page;
									}
								}
								else {
									main_screen.info.text.color = Util.black_no_alpha;
								}
							}
							else if(current_screen == main_screen.info_page) {
								if(info_hover) {
									main_screen.info.text.color = Util.white;

									if(clicked) {
										fade_screen = main_screen;
										main_screen.info.text.color = Util.white_no_alpha;
									}
								}
								else {
									main_screen.info.text.color = Util.black_no_alpha;
								}

								if(hyperlink_hover && clicked) {
									Application.OpenURL("https://www.killbox.info/");
								}
							}
							else {
								Assert.invalid_path();
							}
						}

						main_screen.info.head.material.color = info_hover ? Util.white : info_color;

						yield return Util.wait_for_frame;
					}

					for(int i = 0; i < menu_player_sources.Length; i++) {
						menu_player_sources[i].volume = 0.0f;
					}
				}
			}
		}
		else {
			Assert.is_true(!main_screen.transform.gameObject.activeSelf);
			Assert.is_true(!lan_screen.transform.gameObject.activeSelf);

			StartCoroutine(start_game(last_player_type));
		}

		yield return null;
	}

	void Awake() {
		Debug.Log("Awake(), " + Time.realtimeSinceStartup);

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
				Settings.LAN_FORCE_CONNECTION = true;

				break;
			}

			case 2: {
				Settings.LAN_MODE = true;
				Settings.LAN_SERVER_MACHINE = false;
				Settings.LAN_FORCE_CONNECTION = true;

				break;
			}
		}

		Settings.LAN_FORCE_CONNECTION = true;
		Settings.FORCE_OFFLINE_MODE = false;

		Settings.USE_TRANSITIONS = true;

		QualitySettings.vSyncCount = VSYNC_COUNT;
		QualitySettings.antiAliasing = 4;
#endif

		network_view = GetComponent<NetworkView>();

		menu_camera = transform.Find("Camera").GetComponent<Camera>();
		bees_source = GetComponent<AudioSource>();

		splash_screen = new SplashScreen();
		splash_screen.transform = transform.Find("Camera/Splash");
		splash_screen.go = splash_screen.transform.gameObject;
		splash_screen.go.SetActive(false);
		splash_screen.biome = splash_screen.transform.Find("Biome").GetComponent<Renderer>();
		splash_screen.killbox = splash_screen.transform.Find("Killbox").GetComponent<Renderer>();
		splash_screen.killbox.enabled = false;

		main_screen = new MainScreen();
		main_screen.transform = transform.Find("Camera/Main");
		main_screen.cursor = main_screen.transform.Find("Cursor");
		main_screen.player1 = PlayerButton.new_inst(main_screen.transform, "Player1Button");
		main_screen.player2 = PlayerButton.new_inst(main_screen.transform, "Player2Button");
		main_screen.info_page = main_screen.transform.Find("InfoPage").GetComponent<Renderer>();
		main_screen.info = PlayerButton.new_inst(main_screen.transform, "InfoButton");
		main_screen.hyperlink = main_screen.info_page.GetComponent<Collider>();

		lan_screen = new LanScreen();
		lan_screen.transform = transform.Find("Camera/LAN");
		lan_screen.intro_renderer = lan_screen.transform.Find("IntroMovie").GetComponent<Renderer>();

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
		loading_log = transform.Find("Camera/LoadingLog").GetComponent<TextMesh>();

		player1_prefab = Util.load_prefab("Player1Prefab");
		player2_prefab = Util.load_prefab("Player2Prefab");
		explosion_prefab = Util.load_prefab("ExplosionPrefab");

		player_type = PlayerType.NONE;

		sun = GameObject.Find("Sun").GetComponent<Light>();
		time_of_day = 0.0f;

		audio = Audio.new_inst();
		Audio.load(audio, this, true);

		env = Environment.new_inst(this, GameObject.Find("Environment").transform);

		menu_player_sources = new AudioSource[2];
		for(int i = 0; i < menu_player_sources.Length; i++) {
			int player_id = i + 1;
			AudioSource source = menu_player_sources[i] = Util.new_audio_source(transform, "MenuPlayer" + player_id + "Source");
			source.clip = (AudioClip)Resources.Load("menu_player" + player_id + "_hover");
			source.loop = true;
			source.volume = 0.0f;
			source.Play();
		}
		menu_player_sources[1].pitch = 1.75f;

		Util.shuffle_array<Texture2D>(player2_body_textures);
	}

	void Start() {
		Debug.Log("Start(), " + Time.realtimeSinceStartup);

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
					Debug.Log("created_player");
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
			if(Settings.INSTALLATION_BUILD && created_player) {
				pause_screen.time_since_last_response += Time.deltaTime;
				if(pause_screen.time_since_last_response >= AUTO_PAUSE_TIME) {
					set_pause(this, true);
				}
			}
		}

		// Debug.Log(pause_screen.time_since_last_response.ToString());
	}
}
