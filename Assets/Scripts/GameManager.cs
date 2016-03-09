using UnityEngine;
using UnityEngine.Analytics;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable 0108
#pragma warning disable 0162
#pragma warning disable 0414
#pragma warning disable 0618

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

	public class MainScreen {
		public Transform transform;

		public Transform player1_button;
		public Renderer player1_head;
		public Renderer player1_body;
		public Renderer player1_helmet;
		public TextMesh player1_text;
		
		public Transform player2_button;
		public Renderer player2_head;
		public Renderer player2_body;
		public TextMesh player2_text;

		public Transform cursor;

		public Transform killbox;
	}

	public class EndScreen {
		public Transform transform;

		// public TextMesh passage;
		public TextMesh passage0;
		public TextMesh passage1;
		public TextMesh hint;

		public TextMesh info;

		public Transform cursor;

		public Transform play_button;
		public Collider play_collider;
		public Renderer play_circle;
		public TextMesh play_text;

		public Transform info_button;
		public Collider info_collider;
		public Renderer info_circle;
		public TextMesh info_text;
	}

	[System.NonSerialized] public NetworkView network_view;

	public static bool splash_screen_closed = false;
	public SplashScreen splash_screen;
	public MainScreen main_screen;
	public EndScreen end_screen;
	[System.NonSerialized] public TextMesh log_text_mesh;

	public Color player1_text_color = Color.green;
	public Color player2_text_color = Color.red;

	public Texture2D player1_head_texture = null;
	public Texture2D player1_alt_head_texture = null;

	public Texture2D player2_body_texture = null;
	public Texture2D[] player2_body_textures = null;
	float player2_texture_flip_rate = 60.0f;
	float player2_texture_flip_time = 0.0f;
	int player2_texture_id = 0;

	static string game_type_player1 = "killbox_player1_server";
	static string game_type_player2 = "killbox_player2_server";
	//TODO: Stable naming scheme!!
	// static string game_name = "killbox_online_server";
	static string game_name = "killbox_server___";

	Camera menu_camera = null;
	Camera pause_camera = null;

	AudioSource menu_audio_source = null;

	public static PlayerType persistent_player_type = PlayerType.NONE;
	public static ScenarioType persistent_scenario_type = ScenarioType.NONE;

	// public static float drone_height = 80.0f;
	public static float drone_height = 160.0f;
	// public static float drone_radius = 30.0f;
	public static float drone_radius = 60.0f;

	[System.NonSerialized] public PlayerType player_type = PlayerType.NONE;
	[System.NonSerialized] public ConnectionType connection_type = ConnectionType.NONE;
	[System.NonSerialized] public bool created_player = false;
	[System.NonSerialized] public float total_playing_time = 0.0f;
	[System.NonSerialized] public bool paused = false;
	[System.NonSerialized] public bool first_missile_hit = false;
	[System.NonSerialized] public bool showing_stats = false;

	[System.NonSerialized] public TargetPointController scenario;

	[System.NonSerialized] public Player1Controller player1_inst;
	[System.NonSerialized] public Player1Controller network_player1_inst;

	[System.NonSerialized] public Player2Controller player2_inst;
	[System.NonSerialized] public Player2Controller network_player2_inst;

	[System.NonSerialized] public ScenarioType network_scenario_type = ScenarioType.NONE;

	[System.NonSerialized] public Light sun;
	[System.NonSerialized] public float time_of_day;

	[System.NonSerialized] public Environment env;
	[System.NonSerialized] public Transform scenarios = null;

	[System.NonSerialized] public Audio audio;
	[System.NonSerialized] public AudioSource menu_sfx_source;

	[System.NonSerialized] public Transform player1_prefab = null;
	[System.NonSerialized] public Transform player2_prefab = null;
	[System.NonSerialized] public Transform missile_prefab = null;
	[System.NonSerialized] public Transform explosion_prefab = null;

	[System.NonSerialized] public Color[] npc_color_pool = {
		Util.new_color(47, 147, 246),
		Util.new_color(51, 203, 152),
		Util.new_color(220, 126, 225),
	};

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
			if(game_manager.env.controls_hint != null) {
				game_manager.env.controls_hint.GetComponent<Renderer>().material.color = Util.white * brightness;
			}

			if(game_manager.player2_inst != null) {
				game_manager.player2_inst.mesh_renderer.material.SetFloat("_Brightness", 1.0f);
				game_manager.player2_inst.anim_renderer.material.SetFloat("_Brightness", 1.0f);
			}

			if(game_manager.network_player2_inst != null) {
				game_manager.network_player2_inst.mesh_renderer.material.SetFloat("_Brightness", 1.0f);
				game_manager.network_player2_inst.anim_renderer.material.SetFloat("_Brightness", 1.0f);
			}
		}
	}

	static public void set_infrared_mode(bool enabled) {
		Shader.SetGlobalFloat("_InfraredAmount", enabled ? 1.0f : 0.0f);
	}

	public string get_input_str() {
		string input_str = "";
		if(!paused) {
#if UNITY_STANDALONE_OSX
			//NOTE: Input.inputString doesn't work on Mac, bug in Unity :(
			bool shift_modifier = false;
			if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
				shift_modifier = true;
			}

			int first_char = shift_modifier ? 65 : 97;

			int first_key_index = (int)KeyCode.A;
			int last_key_index = (int)KeyCode.Z;
			for(int i = first_key_index; i < last_key_index; i++) {
				if(Input.GetKeyDown((KeyCode)i)) {
					char character_code = (char)(first_char + (i - first_key_index));
					input_str += character_code;
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
		}

		return input_str;
	}

	public bool get_key(KeyCode key_code) {     
#if !UNITY_EDITOR && UNITY_ANDROID
		key_code = KeyCode.Mouse0;
#endif
		return paused ? false : Input.GetKey(key_code);
	}

	public bool get_key_down(KeyCode key_code) {
#if !UNITY_EDITOR && UNITY_ANDROID
		key_code = KeyCode.Mouse0;
#endif
		return paused ? false : Input.GetKeyDown(key_code);
	}

	public float get_axis(string axis_id) {
		return paused ? 0.0f : Input.GetAxis(axis_id);
	}

	public void show_stats(Camera camera) {
		StartCoroutine(show_stats_(camera));
	}

	public Renderer create_dot_quad() {
		Renderer dot_quad = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Renderer>();
		Destroy(dot_quad.GetComponent<Collider>());

		Material dot_material = (Material)Resources.Load("stat_dot_mat");
		dot_quad.material = dot_material;

		dot_quad.gameObject.layer = LayerMask.NameToLayer("UI");
		dot_quad.transform.parent = end_screen.transform;
		dot_quad.transform.localPosition = Vector3.zero;
		dot_quad.transform.localRotation = Quaternion.identity;
		dot_quad.transform.localScale = Vector3.zero;

		return dot_quad;        
	}

	public IEnumerator show_stats_(Camera camera) {
		bool show_info = true;
		if(persistent_player_type == PlayerType.NONE) {
			persistent_player_type = player_type == PlayerType.PLAYER1 ? PlayerType.PLAYER2 : PlayerType.PLAYER1;
			show_info = false;
		}

		if(show_info) {
			camera.gameObject.SetActive(false);

			menu_camera.gameObject.SetActive(true);
			main_screen.transform.gameObject.SetActive(false);
			end_screen.transform.gameObject.SetActive(true);

			showing_stats = true;
			paused = false;
			pause_camera.gameObject.SetActive(false);

			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;

			end_screen.hint.color = Util.white_no_alpha;
			end_screen.passage0.color = Util.white_no_alpha;
			end_screen.passage1.color = Util.white_no_alpha;

			//NOTE: Player2 dots!!
			// Renderer dot_quad = create_dot_quad();
			// Renderer dot_cache_quad = create_dot_quad();

			// int grid_size = 50;
			// int dot_count = grid_size * grid_size;

			// float dot_size = 0.01f;
			// float half_dot_size = dot_size * 0.5f;
			// float dot_display_time = 8.0f / (float)dot_count;
			// Debug.Log(dot_display_time.ToString());

			// float time_since_last_wait = 0.0f;
			// float frame_time = 1.0f / 60.0f;

			// for(int y = 0; y < grid_size; y++) {
			//  float x_ = grid_size;
			//  float y_ = (float)y;

			//  dot_cache_quad.transform.localScale = new Vector3(x_, y, 1.0f) * dot_size;
			//  dot_cache_quad.transform.localPosition = new Vector3(0.0f, 0.25f - y_ * half_dot_size, 0.0f);
			//  dot_cache_quad.material.mainTextureScale = new Vector3(x_, y_);

			//  for(int x = 0; x < grid_size; x++) {
			//      float x1 = (float)(x + 1);
			//      float y1 = (float)(y + 1);

			//      dot_quad.transform.localScale = new Vector3(x1, 1.0f, 1.0f) * dot_size;
			//      dot_quad.transform.localPosition = new Vector3(-0.25f + x1 * half_dot_size, 0.25f - (y1 * dot_size - half_dot_size), 0.0f);
			//      dot_quad.material.mainTextureScale = new Vector2(x1, 1);

			//      time_since_last_wait += dot_display_time;
			//      if(time_since_last_wait > frame_time) {
			//          time_since_last_wait -= frame_time;
			//          yield return Util.wait_for_frame;
			//      }
			//  }
			// }

			yield return Util.wait_for_2000ms;

			yield return StartCoroutine(Util.lerp_text_alpha(end_screen.passage0, 1.0f));
			yield return new WaitForSeconds(3.0f);
			yield return StartCoroutine(Util.lerp_text_alpha(end_screen.passage1, 1.0f));
			yield return new WaitForSeconds(4.0f);

			{
				float time_out = 30.0f;
				float t = 0.0f;
				while(!Input.anyKey && t < time_out) {
					end_screen.hint.color = Util.new_color(Color.white, t);

					t += Time.deltaTime;
					yield return Util.wait_for_frame;
				}
			}

			{
				StartCoroutine(Util.lerp_text_alpha(end_screen.passage0, 0.0f));
				StartCoroutine(Util.lerp_text_alpha(end_screen.passage1, 0.0f));

				float hint_a = end_screen.hint.color.a;

				float t = 0.0f;
				while(t < 1.0f) {
					end_screen.hint.color = Util.new_color(Color.white, Mathf.Lerp(hint_a, 0.0f, t));
 
					t += Time.deltaTime;
					yield return Util.wait_for_frame;
				}
			}

			end_screen.hint.color = Util.white_no_alpha;
		}

		//NOTE: We never want to show info screen in an installation build!!
		if(!Settings.INSTALLATION_BUILD && show_info) {
			//TODO: Network disconnect here!!
			yield return Util.wait_for_1000ms;

			end_screen.play_button.gameObject.SetActive(true);
			end_screen.play_circle.material.color = Util.white_no_alpha;
			end_screen.play_text.color = Util.white_no_alpha;

			end_screen.info_button.gameObject.SetActive(true);
			end_screen.info_circle.material.color = Util.white_no_alpha;
			end_screen.info_text.color = Util.white_no_alpha;

			yield return StartCoroutine(Util.lerp_material_alpha(end_screen.play_circle, 1.0f));
			yield return StartCoroutine(Util.lerp_material_alpha(end_screen.info_circle, 1.0f));

			Renderer cursor_renderer = end_screen.cursor.GetComponent<Renderer>();
			cursor_renderer.material.color = Util.white_no_alpha;

			Cursor.lockState = CursorLockMode.None;
			move_cursor_(end_screen.cursor);
			end_screen.cursor.gameObject.SetActive(true);
			yield return StartCoroutine(Util.lerp_material_alpha(cursor_renderer, 1.0f));
			// Cursor.lockState = CursorLockMode.None;

			bool hit_play_button = true;
			while(true) {
				Ray cursor_ray = move_cursor_(end_screen.cursor);

				if(Util.raycast_collider(end_screen.play_collider, cursor_ray)) {
					end_screen.play_text.color = Util.white;

					if(Input.GetKey(KeyCode.Mouse0)) {
						hit_play_button = true;
						break;
					}
				}
				else {
					end_screen.play_text.color = Util.white_no_alpha;
				}

				if(Util.raycast_collider(end_screen.info_collider, cursor_ray)) {
					end_screen.info_text.color = Util.white;

					if(Input.GetKey(KeyCode.Mouse0)) {
						hit_play_button = false;
						break;
					}
				}
				else {
					end_screen.info_text.color = Util.white_no_alpha;
				}

				yield return Util.wait_for_frame;
			}

			end_screen.play_text.color = Util.white_no_alpha;
			end_screen.info_text.color = Util.white_no_alpha;

			float fade_duration = 0.25f;

			if(hit_play_button) {
				StartCoroutine(Util.lerp_material_color(end_screen.info_circle, end_screen.info_circle.material.color, Util.white_no_alpha, fade_duration));
				yield return StartCoroutine(Util.lerp_material_alpha(cursor_renderer, 0.0f, fade_duration));
				end_screen.cursor.gameObject.SetActive(false);

				yield return StartCoroutine(Util.lerp_material_color(end_screen.play_circle, end_screen.play_circle.material.color, Util.white_no_alpha));
			}
			else {
				StartCoroutine(move_cursor(end_screen.cursor, 4.0f + fade_duration));

				yield return StartCoroutine(Util.lerp_material_color(end_screen.play_circle, end_screen.play_circle.material.color, Util.white_no_alpha, fade_duration));
				yield return StartCoroutine(Util.lerp_material_color(end_screen.info_circle, end_screen.info_circle.material.color, Util.white_no_alpha));

				yield return Util.wait_for_1000ms;

				end_screen.info.gameObject.SetActive(true);
				end_screen.info.color = Util.white_no_alpha;
				end_screen.play_button.transform.localPosition = new Vector3(0.0f, -0.6f, 0.0f);

				yield return StartCoroutine(Util.lerp_text_color(end_screen.info, end_screen.info.color, Util.white));

				yield return new WaitForSeconds(2.5f);

				yield return StartCoroutine(Util.lerp_material_color(end_screen.play_circle, Util.white_no_alpha, Util.white));

				while(true) {
					Ray cursor_ray = move_cursor_(end_screen.cursor);

					if(Util.raycast_collider(end_screen.play_collider, cursor_ray)) {
						end_screen.play_text.color = Util.white;

						if(Input.GetKey(KeyCode.Mouse0)) {
							break;
						}
					}
					else {
						end_screen.play_text.color = Util.white_no_alpha;
					}

					yield return Util.wait_for_frame;
				}

				end_screen.play_text.color = Util.white_no_alpha;
				yield return StartCoroutine(Util.lerp_material_alpha(cursor_renderer, 0.0f, fade_duration));
				end_screen.cursor.gameObject.SetActive(false);

				yield return StartCoroutine(Util.lerp_text_color(end_screen.info, end_screen.info.color, Util.white_no_alpha));
				yield return StartCoroutine(Util.lerp_material_color(end_screen.play_circle, end_screen.play_circle.material.color, Util.white_no_alpha));
			}

			yield return Util.wait_for_1000ms;
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

	TargetPointController get_scenario(ScenarioType scenario_type) {
		int index = (int)scenario_type;
		Assert.is_true(index < scenarios.childCount, "ASSERT: Scenario index out of range!!");

		TargetPointController scenario_ = scenarios.GetChild(index).GetComponent<TargetPointController>();
		scenario_.type = scenario_type;
		return scenario_;
	}

	[RPC]
	void send_scenario_type(int scenario_type) {
		//TODO: Is there a more robust way to do this??
		network_scenario_type = (ScenarioType)scenario_type;
		Debug.Log("Received " + network_scenario_type + " scenario!");
	}

	[RPC]
	void clear_scenario_type() {
		network_scenario_type = ScenarioType.NONE;
	}

	void create_player(ConnectionType connection_type, ScenarioType scenario_type) {
		if(created_player) {
			Assert.invalid_code_path("Something went wrong, player already exists!!");
		}
		else {
			this.connection_type = connection_type;
			created_player = true;

			string player_type_str = player_type == PlayerType.PLAYER1 ? "player1" : "player2";
			
			Analytics.CustomEvent("game_started", new Dictionary<string, object> {
				{ "player_type", player_type_str },
			});

			Assert.is_true(scenario_type != ScenarioType.NONE);
			scenario = get_scenario(persistent_scenario_type);
			if(Settings.INSTALLATION_BUILD) {
				network_view.RPC("send_scenario_type", RPCMode.Others, (int)persistent_scenario_type);
			}
				
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
				
				env.controls_hint.gameObject.SetActive(false);
				env.controls_hidden = true;

				QualitySettings.shadowCascades = 0;
				Environment.apply_look(this, env, Environment.Look.PLAYER1_POV);
			}
			else {
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = !(Cursor.lockState == CursorLockMode.Locked);

				//TODO: Pick this randomly from pool of points!!
				// Transform spawn_point = scenario.spawn_point;
				Transform spawn_point = scenario.spawn_points.GetChild(scenario.spawn_points.childCount - 1);

				if(connection_type != ConnectionType.OFFLINE) {
					Network.Instantiate(player2_prefab, spawn_point.position, spawn_point.rotation, 0);
				}
				else {
					Instantiate(player2_prefab, spawn_point.position, spawn_point.rotation);
				}

				QualitySettings.shadowCascades = 4;
				Environment.apply_look(this, env, Environment.Look.PLAYER2_POV);
			}           
		}
	}

	public bool connected_to_another_player() {
		return Network.connections.Length > 0;
	}

	public void network_disconnect() {
		Network.Disconnect();
		if(Network.isServer) {
			MasterServer.UnregisterHost();
		}
	}

	public void game_over(bool reset_persistent_state = false) {
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = false;

		if(reset_persistent_state) {
			persistent_player_type = PlayerType.NONE;
			persistent_scenario_type = ScenarioType.NONE;

			if(!Settings.INSTALLATION_BUILD) {
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
		paused = false;
		first_missile_hit = false;
		showing_stats = false;

		if(Settings.INSTALLATION_BUILD) {
			if(connected_to_another_player()) {
				network_view.RPC("clear_scenario_type", RPCMode.Others);
			}			
		}

		// if(!Settings.INSTALLATION_BUILD && reset_persistent_state) {
		// 	//TODO: Actually this should never happen!!
		// 	Application.LoadLevel(0);
		// }
		// else {
			pause_camera.gameObject.SetActive(false);
			menu_camera.gameObject.SetActive(true);

			StartCoroutine(show_splash_screen());			
		// }
	}

	void OnServerInitialized() {
		Debug.Log("Server created!");
		if(!Settings.INSTALLATION_BUILD) {
			create_player(ConnectionType.SERVER, persistent_scenario_type);
		}
	}

	void OnConnectedToServer() {
		Debug.Log("Connected to server!");
		if(!Settings.INSTALLATION_BUILD) {
			create_player(ConnectionType.CLIENT, persistent_scenario_type);
		}
	}

	void OnFailedToConnect(NetworkConnectionError error) {
		Debug.Log("ERROR: " + error);

		if(Settings.INSTALLATION_BUILD && !Settings.LAN_SERVER_MACHINE) {
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

					HostData host = host_list[0];

					ScenarioType host_scenario_type = ScenarioType.NONE;
					for(int i = 0; i < (int)ScenarioType.COUNT; i++) {
						ScenarioType scenario_type = (ScenarioType)i;
						if(host.comment == scenario_type.ToString()) {
							host_scenario_type = scenario_type;
							break;
						}
					}

					if(host_scenario_type != ScenarioType.NONE) {
						Debug.Log("Received " + host_scenario_type.ToString() + " scenario!");
						persistent_scenario_type = host_scenario_type;

						Network.Connect(host);
					}
					else {
						Debug.Log("Failed to connect, host had invalid scenario type: " + host.comment);
					}
				}
				else {
					Debug.Log("Could not find an open server, now creating a new one...");

					string game_type = (player_type == PlayerType.PLAYER1) ? game_type_player1 : game_type_player2;
					string game_comment = persistent_scenario_type.ToString();

					Network.InitializeServer(1, 25002, !Network.HavePublicAddress());
					MasterServer.RegisterHost(game_type, game_name, game_comment);
				}
			}
		}
		else {
			Debug.Log("Caught MasterServerEvent." + evt);
		}
	}

	IEnumerator start_game_from_main_screen(PlayerType player_type) {
		this.player_type = player_type;

		main_screen.player1_text.color = Util.black_no_alpha;
		main_screen.player2_text.color = Util.black_no_alpha;

		float fade_duration = 0.25f;

		if(Settings.USE_TRANSITIONS) {
			if(player_type == PlayerType.PLAYER1) {
				StartCoroutine(Util.lerp_material_alpha(main_screen.player2_head, 0.0f, fade_duration));
				StartCoroutine(Util.lerp_material_alpha(main_screen.player2_body, 0.0f, fade_duration));

				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.cursor.GetComponent<Renderer>(), 0.0f, fade_duration));
				main_screen.cursor.gameObject.SetActive(false);

				StartCoroutine(Util.lerp_audio_volume(menu_audio_source, 1.0f, 0.0f, 4.0f));

				yield return new WaitForSeconds(0.5f);
				StartCoroutine(Util.lerp_material_alpha(main_screen.player1_helmet, 0.0f));
				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.player1_body, 0.0f));
				yield return new WaitForSeconds(0.5f);
				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.player1_head, 0.0f));
			}
			else {
				StartCoroutine(Util.lerp_material_alpha(main_screen.player1_head, 0.0f, fade_duration));
				StartCoroutine(Util.lerp_material_alpha(main_screen.player1_helmet, 0.0f, fade_duration));
				StartCoroutine(Util.lerp_material_alpha(main_screen.player1_body, 0.0f, fade_duration));

				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.cursor.GetComponent<Renderer>(), 0.0f, fade_duration));
				main_screen.cursor.gameObject.SetActive(false);

				StartCoroutine(Util.lerp_audio_volume(menu_audio_source, 1.0f, 0.0f, 4.0f));

				yield return new WaitForSeconds(0.5f);
				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.player2_body, 0.0f));
				yield return new WaitForSeconds(0.5f);
				yield return StartCoroutine(Util.lerp_material_alpha(main_screen.player2_head, 0.0f));
			}
		}
		else {
			menu_audio_source.volume = 0.0f;
		}

		StartCoroutine(start_game(player_type));
	}

	IEnumerator start_game(PlayerType player_type) {
		this.player_type = player_type;

		persistent_scenario_type = ScenarioType.FARM;
		// if(persistent_scenario_type == ScenarioType.NONE) {
		// 	persistent_scenario_type = (ScenarioType)Util.random_index(scenarios.childCount);
		// }

		if(Settings.INSTALLATION_BUILD) {
			ConnectionType connection_type = Settings.LAN_SERVER_MACHINE ? ConnectionType.SERVER : ConnectionType.CLIENT;
			if(!connected_to_another_player()) {
				connection_type = ConnectionType.OFFLINE;
				Debug.Log("LOG: No network connection, switching to offline game.");
			}
			// else {
			// 	if(player_type == PlayerType.PLAYER1) {
			// 		if(network_player2_inst != null) {
			// 			persistent_scenario_type = network_scenario_type;
			// 		}
			// 	}
			// 	else {
			// 		if(network_player1_inst != null) {
			// 			persistent_scenario_type = network_scenario_type;
			// 		}
			// 	}

			// 	Assert.is_true(persistent_scenario_type != ScenarioType.NONE);
			// }

			create_player(connection_type, persistent_scenario_type);
		}
		else {
			if(Settings.FORCE_OFFLINE_MODE) {
				create_player(ConnectionType.OFFLINE, persistent_scenario_type);
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
						create_player(connection_type, persistent_scenario_type);
					}
				}

				//TODO: Confirm host list hasn't been received!!
				if(!created_player) {
					Debug.Log("Request timed out, playing offline!");
					create_player(ConnectionType.OFFLINE, persistent_scenario_type);
				}
			}
		}

		yield return null;
	}

	IEnumerator show_splash_screen() {
		if(Settings.INSTALLATION_BUILD && Settings.LAN_FORCE_CONNECTION) {
			if(!connected_to_another_player() && persistent_player_type == PlayerType.NONE) {
				splash_screen.transform.gameObject.SetActive(false);
				main_screen.transform.gameObject.SetActive(false);
				end_screen.transform.gameObject.SetActive(false);
				splash_screen_closed = false;

				if(Settings.LAN_SERVER_MACHINE) {
					Network.InitializeServer(1, Settings.LAN_SERVER_PORT, false);
				}
				else {
					Network.Connect(Settings.LAN_SERVER_IP, Settings.LAN_SERVER_PORT);
				}
				
				log_text_mesh.gameObject.SetActive(true);
				string wait_str = "ESTABLISHING LAN CONNECTION";
				int wait_index = 4;

				while(!connected_to_another_player()) {
					//TODO: Why does this crash on OSX??
#if UNITY_EDITOR || !UNITY_STANDALONE_OSX
					log_text_mesh.text += ".";

					if(wait_index > 3) {
						wait_index = 0;
						log_text_mesh.text = wait_str;
					}

					wait_index++;
#endif
					yield return Util.wait_for_500ms;
				}

				log_text_mesh.gameObject.SetActive(false);
			}
		}

		if(Settings.USE_SPLASH && !splash_screen_closed) {
			splash_screen.transform.gameObject.SetActive(true);
			main_screen.transform.gameObject.SetActive(false);
			end_screen.transform.gameObject.SetActive(false);

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
		}
		else {
			splash_screen_closed = true;
		}

		if(persistent_player_type == PlayerType.NONE) {
			if(splash_screen_closed) {
				StartCoroutine(Util.lerp_audio_volume(menu_audio_source, menu_audio_source.volume, 1.0f));
			}

			splash_screen_closed = false;

			splash_screen.transform.gameObject.SetActive(false);
			main_screen.transform.gameObject.SetActive(true);
			end_screen.transform.gameObject.SetActive(false);

			main_screen.cursor.gameObject.SetActive(false);
			main_screen.player1_helmet.material.color = Util.black_no_alpha;
			main_screen.player1_body.material.color = Util.black_no_alpha;
			main_screen.player1_text.color = Util.black_no_alpha;
			main_screen.player2_body.material.color = Util.black_no_alpha;
			main_screen.player2_text.color = Util.black_no_alpha;

			if(Settings.INSTALLATION_BUILD) {
				if(Settings.LAN_SERVER_MACHINE) {
					main_screen.player2_button.gameObject.SetActive(false);
					main_screen.player1_button.localPosition = Vector3.zero;

					main_screen.player1_head.material.color = Util.new_color(player1_text_color, 0.0f);
					yield return StartCoroutine(Util.lerp_material_color(main_screen.player1_head, main_screen.player1_head.material.color, player1_text_color));                   
				}
				else {
					main_screen.player1_button.gameObject.SetActive(false);
					main_screen.player2_button.localPosition = Vector3.zero;

					main_screen.player2_head.material.color = Util.new_color(player2_text_color, 0.0f);
					yield return StartCoroutine(Util.lerp_material_color(main_screen.player2_head, main_screen.player2_head.material.color, player2_text_color));                   
				}
			}
			else {
				if(Settings.USE_TRANSITIONS) {
					main_screen.player1_head.material.color = Util.new_color(player1_text_color, 0.0f);
					main_screen.player2_head.material.color = Util.new_color(player2_text_color, 0.0f);

					yield return StartCoroutine(Util.lerp_material_color(main_screen.player1_head, main_screen.player1_head.material.color, player1_text_color));
					yield return StartCoroutine(Util.lerp_material_color(main_screen.player2_head, main_screen.player2_head.material.color, player2_text_color));
				}
				else {
					main_screen.player1_head.material.color = player1_text_color;
					main_screen.player2_head.material.color = player2_text_color;
				}               
			}

			splash_screen_closed = true;

			Cursor.lockState = CursorLockMode.Locked;
			main_screen.cursor.gameObject.SetActive(true);
			yield return StartCoroutine(Util.lerp_material_color(main_screen.cursor.GetComponent<Renderer>(), Util.white_no_alpha, Util.white));
			Cursor.lockState = CursorLockMode.None;
		}
		else {
			splash_screen.transform.gameObject.SetActive(false);
			main_screen.transform.gameObject.SetActive(false);
			end_screen.transform.gameObject.SetActive(false);

			splash_screen_closed = true;
			StartCoroutine(start_game(persistent_player_type));
		}

		yield return null;
	}

	void Awake() {
#if !UNITY_EDITOR
		//TODO: Make sure these are always in sync!!
		if(Settings.QUALITY_LEVEL_SETTINGS) {
			int quality_level = QualitySettings.GetQualityLevel();
			switch(quality_level) {
				case 0: {
					Settings.INSTALLATION_BUILD = false;

					break;
				}

				case 1: {
					Settings.INSTALLATION_BUILD = true;
					Settings.LAN_SERVER_MACHINE = true;

					break;
				}

				case 2: {
					Settings.INSTALLATION_BUILD = true;
					Settings.LAN_SERVER_MACHINE = false;

					break;
				}
			}
		}
#endif

		network_view = GetComponent<NetworkView>();

		menu_camera = transform.Find("Camera").GetComponent<Camera>();
		pause_camera = transform.Find("PauseCamera").GetComponent<Camera>();

		menu_audio_source = GetComponent<AudioSource>();

		splash_screen = new SplashScreen();
		splash_screen.transform = transform.Find("Camera/Splash");
		splash_screen.logo = splash_screen.transform.Find("Logo").GetComponent<Renderer>();
		splash_screen.name = splash_screen.transform.Find("Name").GetComponent<TextMesh>();

		main_screen = new MainScreen();
		main_screen.transform = transform.Find("Camera/Main");
		main_screen.cursor = main_screen.transform.Find("Cursor");
		main_screen.killbox = main_screen.transform.Find("Killbox");

		main_screen.player1_button = main_screen.transform.Find("Player1Button");
		main_screen.player1_head = main_screen.player1_button.Find("Head").GetComponent<Renderer>();
		main_screen.player1_helmet = main_screen.player1_button.Find("Helmet").GetComponent<Renderer>();
		main_screen.player1_body = main_screen.player1_button.Find("Body").GetComponent<Renderer>();
		main_screen.player1_text = main_screen.player1_button.Find("Text").GetComponent<TextMesh>();
		
		main_screen.player2_button = main_screen.transform.Find("Player2Button");
		main_screen.player2_head = main_screen.player2_button.Find("Head").GetComponent<Renderer>();
		main_screen.player2_body = main_screen.player2_button.Find("Body").GetComponent<Renderer>();
		main_screen.player2_text = main_screen.player2_button.Find("Text").GetComponent<TextMesh>();

		end_screen = new EndScreen();
		end_screen.transform = transform.Find("Camera/End");
		end_screen.passage0 = end_screen.transform.Find("Passage0").GetComponent<TextMesh>();
		end_screen.passage1 = end_screen.transform.Find("Passage1").GetComponent<TextMesh>();
		end_screen.hint = end_screen.transform.Find("Hint").GetComponent<TextMesh>();
		end_screen.info = end_screen.transform.Find("Info").GetComponent<TextMesh>();
		end_screen.cursor = end_screen.transform.Find("Cursor");

		end_screen.play_button = end_screen.transform.Find("PlayButton");
		end_screen.play_collider = end_screen.play_button.GetComponent<Collider>();
		end_screen.play_circle = end_screen.play_button.Find("Circle").GetComponent<Renderer>();
		end_screen.play_text = end_screen.play_button.Find("Text").GetComponent<TextMesh>();

		end_screen.info_button = end_screen.transform.Find("InfoButton");
		end_screen.info_collider = end_screen.info_button.GetComponent<Collider>();
		end_screen.info_circle = end_screen.info_button.Find("Circle").GetComponent<Renderer>();
		end_screen.info_text = end_screen.info_button.Find("Text").GetComponent<TextMesh>();

		//TODO: Should this be here??
		end_screen.info.gameObject.SetActive(false);
		end_screen.cursor.gameObject.SetActive(false);
		end_screen.play_button.gameObject.SetActive(false);
		end_screen.info_button.gameObject.SetActive(false);

		log_text_mesh = transform.Find("Camera/Log").GetComponent<TextMesh>();

		player1_prefab = ((GameObject)Resources.Load("Player1Prefab")).transform;
		player2_prefab = ((GameObject)Resources.Load("Player2Prefab")).transform;
		missile_prefab = ((GameObject)Resources.Load("MissilePrefab")).transform;
		explosion_prefab = ((GameObject)Resources.Load("ExplosionPrefab")).transform;

		player_type = PlayerType.NONE;

		sun = GameObject.Find("Sun").GetComponent<Light>();
		time_of_day = 0.0f;

		audio = Audio.new_inst();
		env = Environment.new_inst(this, GameObject.Find("Environment").transform);

		scenarios = env.transform.Find("TargetPoints");

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

			network_scenario_type = ScenarioType.NONE;
		}

		Environment.update(this, env);

		if(Settings.USE_DAY_NIGHT_CYCLE) {
			float length_of_day_secs = 30.0f;
			time_of_day += Time.deltaTime / length_of_day_secs;
			if(time_of_day >= 1.0f) {
				time_of_day = 0.0f;
			}

			float pos_x = Mathf.Cos(time_of_day * Util.TAU);
			float pos_y = Mathf.Sin(time_of_day * Util.TAU);

			sun.transform.position = new Vector3(pos_x, pos_y, -1.0f);
			sun.transform.forward = Vector3.zero - sun.transform.position;

			bool is_day_time = time_of_day < 0.5f; 

			sun.enabled = is_day_time;
			sun.shadowStrength = is_day_time ? 1.0f : 0.0f;
			//TODO: Need a color gradient here!!
			// RenderSettings.ambientLight = is_day_time ? Util.day : Util.night;
		}

		float menu_sfx_volume = 0.0f;

		//TODO: Move this into show_splash_screen??
		if(splash_screen_closed && player_type == PlayerType.NONE) {
			float menu_sfx_max_volume = 0.5f;

			if(Settings.INSTALLATION_BUILD && Settings.LAN_FORCE_CONNECTION) {
				if(!connected_to_another_player()) {
					StartCoroutine(show_splash_screen());
				}
			}

			Ray cursor_ray = move_cursor_(main_screen.cursor);
			bool clicked = Input.GetKeyDown(KeyCode.Mouse0);

			if(main_screen.player1_button.gameObject.activeSelf)
			{
				bool ray_hit = Util.raycast_collider(main_screen.player1_button.GetComponent<Collider>(), cursor_ray);
				if(ray_hit) {
					main_screen.player1_text.color = Color.white;
					main_screen.player1_helmet.material.color = player1_text_color;
					main_screen.player1_body.material.color = player1_text_color;

					// menu_sfx_volume = menu_sfx_max_volume;

					if(clicked) {
						StartCoroutine(start_game_from_main_screen(PlayerType.PLAYER1));
					}
				}
				else {
					main_screen.player1_text.color = Util.black_no_alpha;
					main_screen.player1_helmet.material.color = Util.black_no_alpha;
					main_screen.player1_body.material.color = Util.black_no_alpha;
				}
			}

			if(main_screen.player2_button.gameObject.activeSelf)
			{
				bool ray_hit = Util.raycast_collider(main_screen.player2_button.GetComponent<Collider>(), cursor_ray);
				if(ray_hit) {
					player2_texture_flip_time += Time.deltaTime * player2_texture_flip_rate;
					if(player2_texture_flip_time > 1.0f) {
						player2_texture_id++;
						if(player2_texture_id == player2_body_textures.Length) {
							Util.shuffle_array<Texture2D>(player2_body_textures);
							player2_texture_id = 0;
						}

						player2_texture_flip_time = 0.0f;
						main_screen.player2_body.material.mainTexture = player2_body_textures[player2_texture_id];
					}

					main_screen.player2_text.color = Color.white;
					main_screen.player2_body.material.color = player2_text_color;

					menu_sfx_volume = menu_sfx_max_volume;

					if(clicked) {
						main_screen.player2_body.material.mainTexture = player2_body_texture;
						StartCoroutine(start_game_from_main_screen(PlayerType.PLAYER2));
					}
				}
				else {
					main_screen.player2_text.color = Util.black_no_alpha;
					main_screen.player2_body.material.color = Util.black_no_alpha;
				}
			}

		}

		menu_sfx_source.volume = menu_sfx_volume;

		//TODO: Test this!!
		if(created_player) {
			total_playing_time += Time.deltaTime;
		}

		// for(int btn_index = 0; btn_index < 20; btn_index++) {
		//  KeyCode key_code = (KeyCode)((int)KeyCode.JoystickButton0 + btn_index);
		//  if(Input.GetKeyDown(key_code)) {
		//      Debug.Log(key_code.ToString() + ", " + Time.time);
		//  }
		// }

		if(Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Escape)) {
			game_over(true);
		}
		else {
			// if(Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton9)) {
			if(Input.GetKeyDown(KeyCode.Escape)) {
				if(player_type != PlayerType.NONE && created_player && !showing_stats) {
					if(paused) {
						paused = false;
						pause_camera.gameObject.SetActive(false);

						Cursor.lockState = CursorLockMode.Locked;
						Cursor.visible = false;
					}
					else {
						paused = true;
						pause_camera.gameObject.SetActive(true);

						Cursor.lockState = CursorLockMode.None;
						Cursor.visible = true;
					}
				}
				else {
#if UNITY_EDITOR
					if(Cursor.lockState == CursorLockMode.None) {
						Cursor.lockState = CursorLockMode.Locked;
						Cursor.visible = false;
					}
					else {
						Cursor.lockState = CursorLockMode.None;
						Cursor.visible = true;
					}
#endif
					if(!Settings.INSTALLATION_BUILD) {
						Application.Quit();
					}
				}
			}
		}

		if(paused) {
			Ray mouse_ray = pause_camera.ScreenPointToRay(Input.mousePosition);

			Transform btn_cont = pause_camera.transform.Find("Cont");
			Transform btn_menu = pause_camera.transform.Find("Menu");
			Transform btn_exit = pause_camera.transform.Find("Exit");

			Color text_color = Util.new_color(Util.white, 0.5f);
			Color cont_text_color = text_color;
			Color menu_text_color = text_color;
			Color exit_text_color = text_color;

			RaycastHit hit_info;
			if(Physics.Raycast(mouse_ray, out hit_info, 2.0f)) {
				Transform button = hit_info.collider.transform.parent;
				if(button == btn_cont) {
					cont_text_color = Color.white;

					if(Input.GetKeyDown(KeyCode.Mouse0)) {
						paused = false;
						pause_camera.gameObject.SetActive(false);

						Cursor.lockState = CursorLockMode.Locked;
						Cursor.visible = false;
					}
				}
				else if(button == btn_menu) {
					menu_text_color = Color.white;

					if(Input.GetKeyDown(KeyCode.Mouse0)) {
						game_over(true);
					}
				}
				else if(button == btn_exit) {
					if(!Settings.INSTALLATION_BUILD) {
						exit_text_color = Color.white;

						if(Input.GetKey(KeyCode.Mouse0)) {
							Application.Quit();
						}
					}
				}
			}

			if(Settings.INSTALLATION_BUILD) {
				exit_text_color = Util.new_color(Util.white, 0.25f);
			}

			btn_cont.GetComponent<TextMesh>().color = cont_text_color;
			btn_menu.GetComponent<TextMesh>().color = menu_text_color;
			btn_exit.GetComponent<TextMesh>().color = exit_text_color;
		}
	}

	void OnRenderObject() {
		if(Settings.USE_KILLBOX_ANIMATION) {
			if(splash_screen_closed && player_type == PlayerType.NONE) {
				main_screen.killbox.rotation = Quaternion.Inverse(Quaternion.Euler(35.26439f, -45.0f, 0.0f));
				Killbox.gl_render_(main_screen.killbox, env.killbox.line_material, Time.time * 0.5f);
			}
		}

		Killbox.gl_render(env.killbox);
	}
}
