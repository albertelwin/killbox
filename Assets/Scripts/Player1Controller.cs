﻿
using UnityEngine;
using System.Collections;

//NOTE: Disables RPC warning, temp!!
#pragma warning disable 0618
#pragma warning disable 0162

public static class Player1Util {
	public static Transform new_quad(Transform parent, string layer, Vector3 pos, Vector3 scale, Material material) {
		Transform transform = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
		transform.gameObject.layer = LayerMask.NameToLayer(layer);
		transform.parent = parent;
		transform.localPosition = pos;
		transform.localScale = scale;
		transform.localRotation = Quaternion.identity;

		Renderer renderer = transform.GetComponent<Renderer>();
		renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		renderer.useLightProbes = false;
		renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
		renderer.material = material;

		return transform;
	}
}

public class Player1Console {
	public enum ItemType {
		TEXT_MESH,
		LOADING_BAR,
	};

	public class Item {
		public Transform transform;
		public float height;

		public ItemType type;

		//NOTE: TEXT_MESH
		public TextMesh text_mesh;

		//NOTE: LOAGING_BAR
		public Transform bar;
	}

	public enum CmdType {
		COMMIT_STR,
		PRINT_STR,
		INPUT_STR,

		WAIT,
		WAIT_KEY,
		TUTORIAL_KEY,

		LOG_IN,
		LOG_OFF,

		SWITCH_ON_DISPLAY,
		SWITCH_OFF_DISPLAY,

		ENABLE_CONTROLS,

		ACQUIRE_TARGET,
		LOCK_TARGET,

		LAUNCH_MISSILE,
		FIRE_MISSILE,

		CONFIRM_DEATHS,
	}

	//TODO: This is getting a bit ridiculous, use a base class instead??
	public class Cmd {
		public CmdType type;
		public bool done;

		public bool cursor_on;
		public bool play_audio;

		public float time;
		public float duration;

		public KeyCode key;
		public Player1Controller.ControlType control_type;

		public int index;

		public string str;
		public int str_it;
		public int max_str_len;
		public bool hide_str;

		public Cmd print_cmd;

		public bool use_alt_next_cmd;
		public int next_cmd_index;

		public Item item;
	}

	public class KeyValue {
		public KeyCode key;
		public int val;

		public static KeyValue new_inst(KeyCode key, int val) {
			KeyValue key_val = new KeyValue();
			key_val.key = key;
			key_val.val = val;
			return key_val;
		}
	}

	public class RichTextTag {
		public string match_str;
		public bool is_glyph;

		public static RichTextTag new_inst(string match_str, bool is_glyph) {
			RichTextTag inst = new RichTextTag();
			inst.match_str = match_str;
			inst.is_glyph = is_glyph;
			return inst;
		}
	}

	public static RichTextTag[] RICH_TEXT_TAGS = new RichTextTag[] {
		RichTextTag.new_inst("<quad", true),
		RichTextTag.new_inst("<color", false),

		RichTextTag.new_inst("</", false),
	};

	public static string CURSOR_STR = "\u2588";

	public static float CHARS_PER_SEC = 0.03f;

	public static float DISPLAY_FADE_IN_DURATION = 0.25f;
	public static float DISPLAY_FADE_OUT_DURATION = 1.0f;

	public Transform transform;
	public Renderer renderer;
	public bool enabled;

	public string working_text_buffer;

	public Transform text_mesh_prefab;

	public static float LOADING_BAR_WIDTH = 0.4f;
	public static float LOADING_BAR_HEIGHT = 0.04f;

	public Item[] item_queue;
	public int item_count;
	public int item_head_index;

	public float cursor_time;
	public float last_cursor_time;

	public int command_buffer_capacity;
	public int command_buffer_length;
	public Cmd[] command_buffer;

	public int current_cmd_index;
	public bool first_pass;

	public bool logged_user_details;
	public Cmd username_input_str_cmd;
	public Cmd password_input_str_cmd;

	public KeyValue[] number_key_values = new KeyValue[] {
		KeyValue.new_inst(KeyCode.Keypad0, 0),
		KeyValue.new_inst(KeyCode.Keypad1, 1),
		KeyValue.new_inst(KeyCode.Keypad2, 2),
		KeyValue.new_inst(KeyCode.Keypad3, 3),
		KeyValue.new_inst(KeyCode.Keypad4, 4),
		KeyValue.new_inst(KeyCode.Keypad5, 5),
		KeyValue.new_inst(KeyCode.Keypad6, 6),
		KeyValue.new_inst(KeyCode.Keypad7, 7),
		KeyValue.new_inst(KeyCode.Keypad8, 8),
		KeyValue.new_inst(KeyCode.Keypad9, 9),

		KeyValue.new_inst(KeyCode.Alpha0, 0),
		KeyValue.new_inst(KeyCode.Alpha1, 1),
		KeyValue.new_inst(KeyCode.Alpha2, 2),
		KeyValue.new_inst(KeyCode.Alpha3, 3),
		KeyValue.new_inst(KeyCode.Alpha4, 4),
		KeyValue.new_inst(KeyCode.Alpha5, 5),
		KeyValue.new_inst(KeyCode.Alpha6, 6),
		KeyValue.new_inst(KeyCode.Alpha7, 7),
		KeyValue.new_inst(KeyCode.Alpha8, 8),
		KeyValue.new_inst(KeyCode.Alpha9, 9),
	};

	public static Cmd push_cmd(Player1Console inst, CmdType cmd_type) {
		Assert.is_true(inst.command_buffer_length < inst.command_buffer_capacity);

		Cmd cmd = new Cmd();
		cmd.type = cmd_type;
		cmd.done = false;

		cmd.cursor_on = false;
		cmd.play_audio = false;

		cmd.time = 0.0f;
		cmd.duration = 0.0f;

		cmd.key = KeyCode.None;
		cmd.control_type = Player1Controller.ControlType.COUNT;

		cmd.index = -1;
		
		cmd.str = "";
		cmd.str_it = 0;

		cmd.print_cmd = null;

		cmd.use_alt_next_cmd = false;
		cmd.next_cmd_index = -1;

		inst.command_buffer[inst.command_buffer_length++] = cmd;
		return cmd;
	}

	public static Cmd push_commit_str_cmd(Player1Console inst, string str) {
		Cmd cmd = push_cmd(inst, CmdType.COMMIT_STR);
		cmd.str = str;
		return cmd;
	}

	public static Cmd push_print_str_cmd(Player1Console inst, string str) {
		//TODO: Extract html tags, we don't want to print them!!
		Assert.is_true(str.Length > 0);

		Cmd cmd = push_cmd(inst, CmdType.PRINT_STR);
		cmd.str = str;
		return cmd;
	}

	public static void push_rich_text_str_cmd(Player1Console inst, string str_) {
		Assert.is_true(str_.Length > 0);

		int str_it = 0;
		string str = "";
		while(str_it < str_.Length) {
			str += str_[str_it++];

			for(int i = 0; i < RICH_TEXT_TAGS.Length; i++) {
				RichTextTag tag = RICH_TEXT_TAGS[i];

				if(str.Length >= tag.match_str.Length) {
					string substr = str.Substring(str.Length - tag.match_str.Length, tag.match_str.Length);
					if(substr == tag.match_str) {
						str = str.Substring(0, str.Length - tag.match_str.Length);
						if(str.Length > 0) {
							push_print_str_cmd(inst, str);
							str = "";
						}

						while(true) {
							char char_ = str_[str_it++];
							substr += char_;

							if(char_ == '>') {
								break;
							}
						}

						// for(int ii = 0; ii < tag.extra_len; ii++) {
						// 	substr += str_[str_it++];
						// }

						push_commit_str_cmd(inst, substr);
						if(tag.is_glyph) {
							push_wait_cmd(inst, CHARS_PER_SEC);
						}
					}
				}
			}
		}

		if(str.Length > 0) {
			push_print_str_cmd(inst, str);
		}
	}

	public static Cmd push_input_str_cmd(Player1Console inst, int max_str_len, bool hide_str = false) {
		Assert.is_true(max_str_len > 0);

		Cmd cmd = push_cmd(inst, CmdType.INPUT_STR);
		cmd.max_str_len = max_str_len;
		cmd.hide_str = hide_str;
		return cmd;
	}

	public static void push_wait_cmd(Player1Console inst, float time, bool cursor_on = false) {
		Cmd cmd = push_cmd(inst, CmdType.WAIT);
		cmd.cursor_on = cursor_on;
		cmd.duration = time;
	}

	public static void push_wait_key_cmd(Player1Console inst, KeyCode key) {
		Cmd cmd = push_cmd(inst, CmdType.WAIT_KEY);
		cmd.key = key;
	}

	public static void push_delay_cmd(Player1Console inst, string str = ".\n") {
		float duration = 0.5f;

		push_wait_cmd(inst, duration, true);
		push_commit_str_cmd(inst, str).play_audio = true;
		push_wait_cmd(inst, duration, true);
		push_commit_str_cmd(inst, str).play_audio = true;
		push_wait_cmd(inst, duration, true);
		push_commit_str_cmd(inst, str).play_audio = true;
		push_wait_cmd(inst, duration, true);
	}

	public static void push_tutorial_key_cmd(Player1Console inst, string str, Player1Controller.ControlType control_type, float duration, int index) {
		push_print_str_cmd(inst, str);

		Cmd cmd = push_cmd(inst, CmdType.TUTORIAL_KEY);
		cmd.control_type = control_type;
		cmd.duration = duration;
		cmd.index = index;

		push_delay_cmd(inst, "\n");
	}

	public static void push_fire_missile_cmd(Player1Console inst, int index) {
		push_print_str_cmd(inst, "\nREADYING MISSILE...\n");
		push_delay_cmd(inst);
		push_print_str_cmd(inst, "\nMISSILE LAUNCHED\n\n");

		Cmd cmd = push_cmd(inst, CmdType.FIRE_MISSILE);
		cmd.index = index;

		//TODO: Temp!!
		push_wait_cmd(inst, 11.0f);
	}

	public static void push_confirm_deaths_cmd(Player1Console inst) {
		Cmd cmd = push_cmd(inst, CmdType.CONFIRM_DEATHS);
		cmd.duration = 10.0f;

		push_delay_cmd(inst);
		push_print_str_cmd(inst, "\n");
		cmd.print_cmd = push_print_str_cmd(inst, "0");
		push_print_str_cmd(inst, " DEATHS CONFIRMED\n\n");
	}

	public static Item get_item(Player1Console inst, int it) {
		int index = (inst.item_head_index + it) % inst.item_queue.Length;

		Item item = inst.item_queue[index];
		Assert.is_true(item != null, index.ToString());
		return item;
	}

	public static Item get_tail_item(Player1Console inst) {
		Assert.is_true(inst.item_count > 0);
		return get_item(inst, inst.item_count - 1);
	}

	public static Item pop_front_item(Player1Console inst) {
		Assert.is_true(inst.item_count > 0);

		Item item = inst.item_queue[inst.item_head_index];
		inst.item_queue[inst.item_head_index] = null;

		inst.item_count--;
		inst.item_head_index++;
		if(inst.item_head_index >= inst.item_queue.Length) {
			inst.item_head_index = 0;
		}

		return item;
	}

	public static void push_back_item(Player1Console inst, Item item) {
		Assert.is_true(inst.item_count < inst.item_queue.Length);

		float bounds_height = 1.0f;
		for(int i = 0; i < inst.item_count; i++) {
			Transform transform = get_item(inst, i).transform;
			transform.localPosition += Vector3.up * item.height;

			if(transform.localPosition.y >= bounds_height) {
				Assert.is_true(i == 0);
				GameObject.Destroy(transform.gameObject);
				pop_front_item(inst);
				i--;
			}
		}

		int index = (inst.item_head_index + inst.item_count++) % inst.item_queue.Length;
		inst.item_queue[index] = item;
	}

	public static Item push_text_mesh(Player1Console inst, string str) {
		Transform transform = (Transform)Object.Instantiate(inst.text_mesh_prefab, inst.transform.position, Quaternion.identity);
		transform.name = "TextMesh";
		transform.parent = inst.transform;
		transform.localScale = Vector3.one;
		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.identity;

		TextMesh text_mesh = transform.GetComponent<TextMesh>();
		text_mesh.text = str;
		text_mesh.richText = true;

		Renderer renderer = transform.GetComponent<Renderer>();
		float height = renderer.bounds.size.y;

		Item item = new Item();
		item.transform = transform;
		item.height = height;
		item.type = ItemType.TEXT_MESH;
		item.text_mesh = text_mesh;
		push_back_item(inst, item);
		return item;
	}

	public static Item push_loading_bar(Player1Console inst) {
		Transform transform = Util.new_transform(inst.transform, "LoadingBar");

		Material material = (Material)Resources.Load("hud_mat");

		Transform offset = Util.new_transform(transform, "Offset", new Vector3(LOADING_BAR_WIDTH * 0.5f, LOADING_BAR_HEIGHT * 0.5f, 0.0f));
		Transform bar = Player1Util.new_quad(offset, "UI", Vector3.zero, new Vector3(0.0f, LOADING_BAR_HEIGHT, 1.0f), material);

		float thickness = 0.002f;
		Player1Util.new_quad(offset, "UI", new Vector3((-LOADING_BAR_WIDTH + thickness) * 0.5f, 0.0f, 0.0f), new Vector3(thickness, LOADING_BAR_HEIGHT, 0.0f), material);
		Player1Util.new_quad(offset, "UI", new Vector3((LOADING_BAR_WIDTH - thickness) * 0.5f, 0.0f, 0.0f), new Vector3(thickness, LOADING_BAR_HEIGHT, 0.0f), material);
		Player1Util.new_quad(offset, "UI", new Vector3(0.0f, (LOADING_BAR_HEIGHT - thickness) * 0.5f, 0.0f), new Vector3(LOADING_BAR_WIDTH, thickness, 0.0f), material);
		Player1Util.new_quad(offset, "UI", new Vector3(0.0f, (-LOADING_BAR_HEIGHT + thickness) * 0.5f, 0.0f), new Vector3(LOADING_BAR_WIDTH, thickness, 0.0f), material);

		Item item = new Item();
		item.transform = transform;
		item.height = LOADING_BAR_HEIGHT;
		item.type = ItemType.LOADING_BAR;
		item.bar = bar;
		push_back_item(inst, item);
		return item;
	}

	public static void set_loading_bar_progress(Item item, float progress) {
		Assert.is_true(item != null);
		Assert.is_true(item.type == ItemType.LOADING_BAR);

		Transform bar = item.bar;
		bar.localScale = new Vector3(Mathf.Clamp01(progress) * LOADING_BAR_WIDTH, LOADING_BAR_HEIGHT, 1.0f);
		bar.localPosition = new Vector3((-LOADING_BAR_WIDTH + bar.localScale.x) * 0.5f, 0.0f, 0.0f);
	}

	public static Player1Console new_inst(Transform transform) {
		Player1Console inst = new Player1Console();

		inst.transform = transform;
		inst.renderer = transform.GetComponent<Renderer>();
		inst.enabled = true;

		inst.working_text_buffer = "";

		inst.text_mesh_prefab = ((GameObject)Resources.Load("TextMeshPrefab")).transform;

		inst.item_queue = new Item[64];
		inst.item_count = 0;
		inst.item_head_index = 0;

		push_text_mesh(inst, inst.working_text_buffer);

		inst.cursor_time = 0.0f;
		inst.last_cursor_time = -0.5f;

		inst.command_buffer_capacity = 512;
		inst.command_buffer_length = 0;
		inst.command_buffer = new Cmd[inst.command_buffer_capacity];

		inst.current_cmd_index = 0;
		inst.first_pass = true;

		inst.logged_user_details = false;

		// push_commit_str_cmd(inst, "<quad material=1 width=0.001 height=0.001/>\n");

		// int sprite_sheet_size_pixels = 390;

		// float sprite_size = 51.0f / (float)sprite_sheet_size_pixels;
		// float sprite_size_with_padding = 65.0f / (float)sprite_sheet_size_pixels;

		// for(int y = 0; y < 6; y++) {
		// 	for(int x = 0; x < 6; x++) {
		// 		float pos_x = x * sprite_size_with_padding;
		// 		float pos_y = 1.0f - (y + 1) * sprite_size_with_padding;

		// 		string rich_text = string.Format("<quad material=1 x={0} y={1} width={2} height={2}/>", pos_x, pos_y, sprite_size);
		// 		push_rich_text_str_cmd(inst, rich_text);
		// 	}

		// 	push_print_str_cmd(inst, "\n");
		// }

		push_cmd(inst, CmdType.SWITCH_ON_DISPLAY);
		push_cmd(inst, CmdType.LOG_IN);
		push_cmd(inst, CmdType.ENABLE_CONTROLS);
		push_cmd(inst, CmdType.ACQUIRE_TARGET);

		push_tutorial_key_cmd(inst, "HOLD \"A\" TO LOOK LEFT\n", Player1Controller.ControlType.LOOK_LEFT, 1.8f, 0);
		push_tutorial_key_cmd(inst, "HOLD \"D\" TO LOOK RIGHT\n", Player1Controller.ControlType.LOOK_RIGHT, 1.8f, 1);

		push_wait_cmd(inst, Mathf.Infinity);

		// push_print_str_cmd(inst, "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce luctus, lectus ultrices vulputate scelerisque, enim ante mollis libero, at lobortis nisi massa id orci. Quisque ac diam nisl. In hac habitasse platea dictumst. Etiam ligula neque, euismod id ornare sit amet, posuere et ante\n");
		// push_wait_cmd(inst, Mathf.Infinity);

		if(Settings.USE_TRANSITIONS) {
			push_wait_cmd(inst, 4.0f);
		}

		push_print_str_cmd(inst, "USERNAME: ");
		inst.username_input_str_cmd = push_input_str_cmd(inst, 16);

		push_print_str_cmd(inst, "PASSWORD: ");
		inst.password_input_str_cmd = push_input_str_cmd(inst, 16, true);

		push_delay_cmd(inst);
		push_cmd(inst, CmdType.LOG_IN);

		push_print_str_cmd(inst, "\nCREECH AIRBASE 432D\n");
		push_wait_cmd(inst, 2.0f);

		push_print_str_cmd(inst, "\nN 36°35′32″\n");
		push_print_str_cmd(inst, "W 115°40′00″\n\n");

		push_print_str_cmd(inst, "2512155\n\n");
		push_print_str_cmd(inst, "2443872\n\n");

		push_wait_cmd(inst, 3.0f);

		push_print_str_cmd(inst, "\nJ09NV0399\n");
		//TODO: Don't end block here, end if after next wait cmd!!
		push_delay_cmd(inst);

		push_print_str_cmd(inst, "\n");
		push_wait_cmd(inst, 4.5f);

		push_print_str_cmd(inst, "PRESS \"U\" TO LOCATE UAV\n");
		push_wait_key_cmd(inst, KeyCode.U);
		push_print_str_cmd(inst, "\nSEARCHING...\n");
		push_delay_cmd(inst);
		push_print_str_cmd(inst, "\nUAV LOCATED OVER WAZIRISTAN, PAKISTAN\n");
		push_print_str_cmd(inst, "\nCONNECTING VIA REMOTE SYSTEM...\n");
		push_delay_cmd(inst);
		push_print_str_cmd(inst, "\nCONNECTED TO UAV PREDATOR DRONE\n\n");

		push_cmd(inst, CmdType.SWITCH_ON_DISPLAY);

		// if(Settings.USE_TRANSITIONS) {
		if(true) {
			push_print_str_cmd(inst, "RUNNING SYSTEMS CHECK...\n");
			push_delay_cmd(inst);
			push_print_str_cmd(inst, "\n\n");

			push_tutorial_key_cmd(inst, "HOLD \"A\" TO LOOK LEFT\n", Player1Controller.ControlType.LOOK_LEFT, 1.8f, 0);
			push_tutorial_key_cmd(inst, "HOLD \"D\" TO LOOK RIGHT\n", Player1Controller.ControlType.LOOK_RIGHT, 1.8f, 1);
			push_tutorial_key_cmd(inst, "HOLD \"W\" TO LOOK UP\n", Player1Controller.ControlType.LOOK_UP, 1.8f, 2);
			push_tutorial_key_cmd(inst, "HOLD \"S\" TO LOOK DOWN\n", Player1Controller.ControlType.LOOK_DOWN, 1.8f, 3);
			push_tutorial_key_cmd(inst, "HOLD \"Q\" TO ZOOM IN\n", Player1Controller.ControlType.ZOOM_IN, 1.2f, 4);
			push_tutorial_key_cmd(inst, "HOLD \"E\" TO ZOOM OUT\n", Player1Controller.ControlType.ZOOM_OUT, 1.2f, 5);

			push_cmd(inst, CmdType.ENABLE_CONTROLS);
			push_print_str_cmd(inst, "ALL SYSTEMS OPERATIONAL\n\n");
		}
		else {
			push_cmd(inst, CmdType.ENABLE_CONTROLS);
		}

		if(Settings.USE_TRANSITIONS) {
			push_wait_cmd(inst, 10.0f);
		}

		push_cmd(inst, CmdType.ACQUIRE_TARGET);
		push_print_str_cmd(inst, "TARGET ACQUIRED\n\n");

		push_print_str_cmd(inst, "PRESS \"T\" TO CONFIRM TARGET\n");
		push_wait_key_cmd(inst, KeyCode.T);
		push_print_str_cmd(inst, "\nLOCKING ON...\n");
		push_delay_cmd(inst);

		push_print_str_cmd(inst, "\nTARGET LOCKED\n\n");
		push_cmd(inst, CmdType.LOCK_TARGET);

		push_print_str_cmd(inst, "PRESS \"M\" TO LAUNCH PRIMARY MISSILE\n");
		push_wait_key_cmd(inst, KeyCode.M);
		push_fire_missile_cmd(inst, 0);

		push_print_str_cmd(inst, "PRESS \"M\" TO LAUNCH SECONDARY MISSILE\n");
		{
			Cmd cmd = push_cmd(inst, CmdType.LAUNCH_MISSILE);
			cmd.key = KeyCode.M;
			cmd.duration = 15.0f;

			push_print_str_cmd(inst, "\nSWITCHING TO AUTO-PILOT...\n");
			push_delay_cmd(inst);

			cmd.next_cmd_index = inst.command_buffer_length;
			push_fire_missile_cmd(inst, 1);
		}

		if(!Settings.USE_DEATH_CONFIRM) {
			push_wait_cmd(inst, 10.0f);
		}
		else {
			push_wait_cmd(inst, 5.0f);
			push_print_str_cmd(inst, "\nENTER NUMBER OF TARGETS NEUTRALISED: ");
			push_confirm_deaths_cmd(inst);
			push_wait_cmd(inst, 5.0f);
		}

		push_cmd(inst, CmdType.SWITCH_OFF_DISPLAY);
		push_wait_cmd(inst, 2.0f);
		push_cmd(inst, CmdType.LOG_OFF);
		push_wait_cmd(inst, 1.0f);

		return inst;
	}

	public static bool is_new_line(char char_) {
		return char_ == '\n' || char_ == '\r';
	}

	public static bool is_cursor_on(float cursor_time) {
		return ((int)(cursor_time * 2.0f)) % 2 == 0;
	}

	public static void update(Player1Console inst, Player1Controller player1) {
		if(!inst.logged_user_details) {
			if(inst.username_input_str_cmd.done && inst.password_input_str_cmd.done) {
				string details_str = "username: " + inst.username_input_str_cmd.str + ", password: " + inst.password_input_str_cmd.str + "\n";
				Debug.Log(details_str);
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
				System.IO.File.AppendAllText("killbox_Data/passwords.txt", details_str);
#endif
				inst.logged_user_details = true;
			}
		}

		if(inst.enabled) {
			GameManager game_manager = player1.game_manager;

			float time_left = Time.deltaTime;
			inst.cursor_time += Time.deltaTime;

			while(time_left > 0.0f && inst.current_cmd_index < inst.command_buffer_length) {
				Cmd cmd = inst.command_buffer[inst.current_cmd_index];

				switch(cmd.type) {
					case CmdType.COMMIT_STR: {
						inst.working_text_buffer += cmd.str;

						if(cmd.play_audio) {
							Audio.play(game_manager.audio, Audio.Clip.CONSOLE_TYPING);
						}

						cmd.done = true;
						break;
					}

					case CmdType.PRINT_STR: {
						cmd.time += time_left;
						int chars_left = cmd.str.Length - cmd.str_it;
						int chars_to_print = Mathf.Min((int)(cmd.time / CHARS_PER_SEC), chars_left);
						cmd.time -= chars_to_print * CHARS_PER_SEC;

						for(int i = 0; i < chars_to_print; i++) {
							inst.working_text_buffer += cmd.str[cmd.str_it++];
						}

						if(chars_to_print > 0) {
							Audio.play(game_manager.audio, Audio.Clip.CONSOLE_TYPING);
						}

						if(cmd.str_it >= cmd.str.Length) {
							time_left = cmd.time;
							cmd.done = true;
						}

						inst.cursor_time = inst.last_cursor_time = 0.0f;
						break;
					}

					case CmdType.INPUT_STR: {
						string str = cmd.str;

						string input_str = game_manager.get_input_str();
						if(input_str.Length > 0) {
							for(int i = 0; i < input_str.Length; i++) {
								char input_char = input_str[i];
								if(input_char == '\b') {
									if(str.Length > 0) {
										str = str.Substring(0, str.Length - 1);
										inst.working_text_buffer = inst.working_text_buffer.Substring(0, inst.working_text_buffer.Length - 1);

										Audio.play(game_manager.audio, Audio.Clip.CONSOLE_TYPING);
									}
								}
								else {
									if(is_new_line(input_char)) {
										if(str.Length > 0) {
											inst.working_text_buffer += "\n";

											cmd.done = true;
											break;
											// Debug.Log(str);
										}
									}
									else {
										if(str.Length < cmd.max_str_len) {
											str += input_char;
											if(cmd.hide_str) {
												//TODO: Replace this with a better character??
												inst.working_text_buffer += "*";
											}
											else {
												inst.working_text_buffer += input_char;
											}

											Audio.play(game_manager.audio, Audio.Clip.CONSOLE_TYPING);
										}
									}
								}
							}

							cmd.str = str;
							inst.cursor_time = inst.last_cursor_time = 0.0f;
						}

						break;
					}

					case CmdType.WAIT: {
						cmd.time += time_left;
						if(cmd.time >= cmd.duration) {
							time_left = cmd.time - cmd.duration;
							cmd.done = true;
						}

						if(cmd.cursor_on) {
							inst.cursor_time = inst.last_cursor_time = 0.0f;
						}

						break;
					}

					case CmdType.WAIT_KEY: {
						if(game_manager.get_key_down(cmd.key)) {
							cmd.done = true;
						}

						break;
					}

					case CmdType.LOG_IN: {
						// player1.audio_sources[0].Stop();
						if(Settings.USE_MUSIC) {
							player1.audio_sources[1].Play();
						}

						cmd.done = true;
						break;
					}

					case CmdType.LOG_OFF: {
						player1.StartCoroutine(player1.log_off());

						cmd.done = true;
						break;
					}

					case CmdType.SWITCH_ON_DISPLAY: {
						player1.StartCoroutine(FadeImageEffect.lerp_alpha(player1.camera_fade, 0.0f, DISPLAY_FADE_IN_DURATION));

						cmd.done = true;
						break;
					}

					case CmdType.SWITCH_OFF_DISPLAY: {
						player1.StartCoroutine(FadeImageEffect.lerp_alpha(player1.camera_fade, 1.0f, DISPLAY_FADE_OUT_DURATION));

						cmd.done = true;
						break;
					}

					case CmdType.TUTORIAL_KEY: {
						Player1Controller.Control control = player1.controls[(int)cmd.control_type];
						control.enabled = true;

						if(cmd.item == null) {
							//TODO: Actually parse the working buffer and push as many text meshes as needed!!
							Item tail = get_tail_item(inst);
							if(tail.type == ItemType.TEXT_MESH) {
								tail.text_mesh.text = "";
							}

							cmd.item = push_loading_bar(inst);
							push_text_mesh(inst, "");
						}

						if(game_manager.get_key(control.key)) {
							cmd.time += time_left;

							set_loading_bar_progress(cmd.item, cmd.time / cmd.duration);

							if(cmd.time >= cmd.duration) {
								time_left = cmd.time - cmd.duration;
								cmd.done = true;

								for(int i = 0; i < player1.controls.Length; i++) {
									player1.controls[i].enabled = false;
								}
							}

							inst.cursor_time = inst.last_cursor_time = 0.0f;
						}

						break;
					}

					case CmdType.ENABLE_CONTROLS: {
						for(int i = 0; i < player1.controls.Length; i++) {
							player1.controls[i].enabled = true;
						}

						cmd.done = true;
						break;
					}

					case CmdType.ACQUIRE_TARGET: {
						player1.marker_renderer.material.color = Util.white;
						player1.marker_renderer.material.SetColor("_Temperature", Util.green);
						player1.locked_target = game_manager.scenario.high_value_target;

						cmd.done = true;
						break;
					}

					case CmdType.LOCK_TARGET: {
						player1.marker_renderer.material.color = Util.red;
						// player1.marker_renderer.material.SetColor("_Temperature", Util.red);

						cmd.done = true;
						break;
					}

					case CmdType.LAUNCH_MISSILE: {
						cmd.time += time_left;
						if(cmd.time >= cmd.duration) {
							time_left = cmd.time - cmd.duration;
							cmd.done = true;
						}
						else {
							if(game_manager.get_key_down(cmd.key)) {
								cmd.done = true;

								cmd.use_alt_next_cmd = true;
							}
						}

						break;
					}

					case CmdType.FIRE_MISSILE: {
						player1.StartCoroutine(player1.fire_missile_(cmd.index));

						cmd.done = true;
						break;
					}

					case CmdType.CONFIRM_DEATHS: {
						cmd.time += time_left;
						if(cmd.time >= cmd.duration) {
							time_left = cmd.time - cmd.duration;
							cmd.done = true;

							inst.working_text_buffer += "\n";
						}
						else {
							//TODO: Support multiple digit numbers!!
							for(int i = 0; i < inst.number_key_values.Length; i++) {
								KeyValue key_val = inst.number_key_values[i];
								if(game_manager.get_key(key_val.key)) {
									cmd.str += key_val.val.ToString();
									break;
								}
							}

							//TODO: Hit enter to confirm!!
							if(cmd.str.Length > 0) {
								cmd.done = true;

								inst.working_text_buffer += cmd.str + "\n";
								cmd.print_cmd.str = cmd.str;
							}							
						}

						break;
					}

					default: {
						Debug.Log("LOG: Cmd_" + cmd.type.ToString() + " not implemented!!");

						cmd.done = true;
						break;
					}
				}

				if(cmd.done) {
					if(cmd.use_alt_next_cmd) {
						inst.current_cmd_index = cmd.next_cmd_index;
					}
					else {
						inst.current_cmd_index++;
					}

					inst.first_pass = true;
				}
				else {
					//NOTE: If a cmd isn't done it must have consumed all of the step time!!
					time_left = 0.0f;
					inst.first_pass = false;
				}

				Item tail_item = get_tail_item(inst);
				
				string working_buffer = inst.working_text_buffer;
				for(int i = 0; i < working_buffer.Length; i++) {
					char char_ = working_buffer[i];

					if(is_new_line(char_)) {
						tail_item.text_mesh.text = working_buffer.Substring(0, i);
						tail_item = push_text_mesh(inst, "");

						working_buffer = working_buffer.Substring(i + 1);
					}
					else {
						tail_item.text_mesh.text = working_buffer.Substring(0, i);

						Renderer renderer = tail_item.text_mesh.GetComponent<Renderer>();
						float width = renderer.bounds.size.x;

						if(width >= 0.5f) {
							int word_start_index = 0;
							for(int j = i - 1; j >= 0; j--) {
								if(working_buffer[j] == ' ') {
									word_start_index = j;
									break;
								}
							}

							if(word_start_index != 0) {
								tail_item.text_mesh.text = working_buffer.Substring(0, word_start_index);
								tail_item = push_text_mesh(inst, "");

								working_buffer = working_buffer.Substring(word_start_index + 1);
							}
						}
					}
				}

				inst.working_text_buffer = working_buffer;
				tail_item.text_mesh.text = inst.working_text_buffer;

				if(is_cursor_on(inst.cursor_time)) {
					tail_item.text_mesh.text += CURSOR_STR;

					if(!is_cursor_on(inst.last_cursor_time)) {
						Audio.play(game_manager.audio, Audio.Clip.CONSOLE_CURSOR_FLASH);
					}
				}

				inst.last_cursor_time = inst.cursor_time;
			}			
		}
	}
}

public class Player1Controller : MonoBehaviour {
	public struct UiIndicator {
		public Transform transform;
		public Renderer[] lines;
		public Renderer fill;
	}

	public enum ControlType {
		LOOK_LEFT,
		LOOK_RIGHT,
		LOOK_UP,
		LOOK_DOWN,

		ZOOM_IN,
		ZOOM_OUT,

		TOGGLE_INFRARED,

		COUNT,
	}

	public class Control {
		public KeyCode key;
		public bool enabled;

		public static Control new_inst(KeyCode key, bool enabled = false) {
			Control control = new Control();
			control.key = key;
			control.enabled = enabled;
			return control;
		}
	}

	public class Meter {
		public Transform transform;
		public Transform line;
		public Transform[] bounds;
		public Transform[] markers;

		public static float LINE_THICKNESS = 0.002f;
		public static float METER_LENGTH = 0.5f;
		// public static float MARKER_SPACING = METER_LENGTH / 6.0f;

		public float marker_spacing;

		public static Meter new_inst(Transform parent, Vector3 pos, Quaternion rotation, float medium_length, float small_length, int resolution) {
			Meter meter = new Meter();

			Material hud_material = (Material)Resources.Load("hud_mat");

			float medium_x = -(LINE_THICKNESS * 0.5f + medium_length * 0.5f);
			Vector3 medium_scale = new Vector3(medium_length, LINE_THICKNESS, 1.0f);

			float small_x = -(LINE_THICKNESS * 0.5f + small_length * 0.5f);
			Vector3 small_scale = new Vector3(small_length, LINE_THICKNESS, 1.0f);


			meter.transform = Util.new_transform(parent, "Meter", pos, Vector3.one, rotation);

			meter.line = Player1Util.new_quad(meter.transform, "HUD", Vector3.zero, new Vector3(LINE_THICKNESS, 0.5f + LINE_THICKNESS, 1.0f), hud_material);

			meter.bounds = new Transform[2];
			meter.bounds[0] = Player1Util.new_quad(meter.transform, "HUD", new Vector3(medium_x, METER_LENGTH * 0.5f, 0.0f), medium_scale, hud_material);
			meter.bounds[1] = Player1Util.new_quad(meter.transform, "HUD", new Vector3(medium_x, -METER_LENGTH * 0.5f, 0.0f), medium_scale, hud_material);

			int marker_count = 3 * resolution;
			meter.marker_spacing = METER_LENGTH / (float)marker_count;

			meter.markers = new Transform[marker_count];
			for(int i = 0; i < meter.markers.Length; i++) {
				float x = small_x;
				Vector3 scale = small_scale;
				if(i % 3 == 0) {
					x = medium_x;
					scale = medium_scale;
				}

				meter.markers[i] = Player1Util.new_quad(meter.transform, "HUD", new Vector3(x, METER_LENGTH * 0.5f - meter.marker_spacing * i, 0.0f), scale, hud_material);
			}

			return meter;
		}

		public static void set_pos(Meter meter, float offset) {
			for(int i = 0; i < meter.markers.Length; i++) {
				Transform marker = meter.markers[i];

				float y = (METER_LENGTH * 0.5f - meter.marker_spacing * i) + offset;

				float dir = y > 0.0f ? 1.0f : -1.0f;
				int adjustment = (int)((y + (METER_LENGTH * 0.5f * dir)) / METER_LENGTH);
				y -= METER_LENGTH * adjustment;

				Vector3 pos = marker.localPosition;
				pos.y = y;
				marker.localPosition = pos;
			}
		}
	}

	[System.NonSerialized] public GameManager game_manager = null;

	[System.NonSerialized] public NetworkView network_view;
	[System.NonSerialized] public float join_time_stamp;

	[System.NonSerialized] public Transform locked_target = null;
	[System.NonSerialized] public bool firing_missile = false;

	[System.NonSerialized] public Control[] controls;
	
	[System.NonSerialized] public Transform camera_ref;
	[System.NonSerialized] public Camera main_camera;

	[System.NonSerialized] public Quaternion camera_ref_zero_rot;
	[System.NonSerialized] public Quaternion camera_zero_rot;
	[System.NonSerialized] public Vector2 camera_xy;
	[System.NonSerialized] public float camera_zoom;

	[System.NonSerialized] public FadeImageEffect camera_fade;
	[System.NonSerialized] public UnityStandardAssets.ImageEffects.Antialiasing camera_aa;

	float angular_pos = Mathf.PI;

	float zero_fov = 40.0f;
	float min_fov = 10.0f;
	float max_fov = 60.0f;
	float zoom_speed = 12.0f;

	MissileController missile_controller;

	Camera hud_camera = null;
	TextMesh hud_acft_text = null;

	[System.NonSerialized] public Renderer marker_renderer;
	UiIndicator[] ui_indicators;
	float ui_indicator_alpha;

	[System.NonSerialized] public Meter meter_x;
	[System.NonSerialized] public Meter meter_y;

	[System.NonSerialized] public bool infrared_mode;
	[System.NonSerialized] public TextMesh[] ui_text_meshes;

	[System.NonSerialized] public Camera ui_camera = null;
	Transform console_transform = null;
	Vector3 console_local_position = Vector3.zero;
	Vector3 console_local_scale = Vector3.one;

	[System.NonSerialized] public Player1Console console_;

	[System.NonSerialized] public AudioSource[] audio_sources;

	void Awake() {
		game_manager = GameObject.Find("GameManager").GetComponent<GameManager>();

		camera_ref = transform.Find("CameraRef");
		main_camera = camera_ref.Find("Camera").GetComponent<Camera>();
		main_camera.fieldOfView = zero_fov;

		camera_ref_zero_rot = camera_ref.transform.localRotation;
		camera_zero_rot = main_camera.transform.localRotation;
		camera_xy = Vector2.zero;
		camera_zoom = 0.0f;

		network_view = GetComponent<NetworkView>();
		bool local_inst = network_view.isMine || (game_manager.connection_type == ConnectionType.OFFLINE);
		if(local_inst) {
			name = "Player1";
			game_manager.player1_inst = this;
		}
		else {
			name = "NetworkPlayer1";
			game_manager.network_player1_inst = this;

			main_camera.gameObject.SetActive(false);
			this.enabled = false;
		}
		
		join_time_stamp = Time.time;

		controls = new Control[(int)ControlType.COUNT];
		controls[(int)ControlType.LOOK_LEFT] = Control.new_inst(KeyCode.A);
		controls[(int)ControlType.LOOK_RIGHT] = Control.new_inst(KeyCode.D);
		controls[(int)ControlType.LOOK_UP] = Control.new_inst(KeyCode.W);
		controls[(int)ControlType.LOOK_DOWN] = Control.new_inst(KeyCode.S);
		controls[(int)ControlType.ZOOM_IN] = Control.new_inst(KeyCode.Q);
		controls[(int)ControlType.ZOOM_OUT] = Control.new_inst(KeyCode.E);
		controls[(int)ControlType.TOGGLE_INFRARED] = Control.new_inst(KeyCode.I);
	}

	public static void destroy(Player1Controller player) {
		if(player.missile_controller.transform.parent == null) {
			Destroy(player.missile_controller.gameObject);
		}

		if(player.ui_camera.transform.parent == null) {
			Destroy(player.ui_camera.gameObject);
		}

		Network.Destroy(player.gameObject);
	}

	void Start() {
		camera_aa = main_camera.GetComponent<UnityStandardAssets.ImageEffects.Antialiasing>();
		camera_aa.enabled = true;

		hud_camera = main_camera.transform.Find("HudCamera").GetComponent<Camera>();
		hud_acft_text = hud_camera.transform.Find("Hud/ACFT").GetComponent<TextMesh>();
		camera_fade = hud_camera.GetComponent<FadeImageEffect>();

		marker_renderer = hud_camera.transform.Find("Hud/Marker").GetComponent<Renderer>();

		infrared_mode = false;
		ui_text_meshes = hud_camera.GetComponentsInChildren<TextMesh>();

		ui_indicator_alpha = 0.5f;
		ui_indicators = new UiIndicator[2];
		for(int i = 0; i < ui_indicators.Length; i++) {
			UiIndicator indicator = new UiIndicator();
			indicator.transform = hud_camera.transform.Find("Hud/Indicator" + i);

			indicator.lines = new Renderer[4];
			for(int ii = 0; ii < indicator.lines.Length; ii++) {
				indicator.lines[ii] = indicator.transform.GetChild(ii).GetComponent<Renderer>();
				indicator.lines[ii].material.color = Util.white;
			}

			indicator.fill = indicator.transform.GetChild(indicator.lines.Length).GetComponent<Renderer>();
			indicator.fill.material.color = Util.white_no_alpha;

			ui_indicators[i] = indicator;
		}

		Transform hud_transform = hud_camera.transform.Find("Hud");
		meter_x = Meter.new_inst(hud_transform, new Vector3(0.0f, 0.425f, 0.0f), Quaternion.Euler(0.0f, 0.0f, -90.0f), 0.04f, 0.02f, 3);
		meter_y = Meter.new_inst(hud_transform, new Vector3(-0.525f, 0.0f, 0.0f), Quaternion.identity, 0.025f, 0.015f, 2);

		missile_controller = main_camera.transform.Find("Missile").GetComponent<MissileController>();
		missile_controller.player1 = this;

		ui_camera = main_camera.transform.Find("UiCamera").GetComponent<Camera>();
		ui_camera.transform.parent = null;
		ui_camera.transform.position = Vector3.zero;
		ui_camera.transform.rotation = Quaternion.identity;
		console_transform = ui_camera.transform.Find("Console");
		console_local_position = console_transform.localPosition;
		console_local_scale = console_transform.localScale;

		{
			float aspect_ratio_x = 16.0f / 9.0f;
			float aspect_ratio_y = 9.0f / 16.0f;

			float screen_width = (float)Screen.width;
			float screen_height = (float)Screen.height;
			float screen_aspect_ratio_x = screen_width / screen_height;

			float adjusted_aspect_ratio_y = aspect_ratio_y * screen_aspect_ratio_x;

			float viewport_padding_x = 0.01f;
			float viewport_padding_y = (viewport_padding_x * aspect_ratio_x) * adjusted_aspect_ratio_y;

			float main_camera_width = 0.75f - viewport_padding_x;
			float main_camera_height = adjusted_aspect_ratio_y - viewport_padding_y;
			float main_camera_height_offset = (1.0f - main_camera_height) * 0.5f;
			
			main_camera.rect = new Rect(viewport_padding_x * 0.5f, main_camera_height_offset, main_camera_width, main_camera_height);
			hud_camera.rect = main_camera.rect;

			float missile_camera_width = 0.25f - viewport_padding_x * 0.5f;
			float missile_camera_height = (missile_camera_width * aspect_ratio_x) * adjusted_aspect_ratio_y;
			
			missile_controller.camera_.rect = new Rect((1.0f - missile_camera_width) - viewport_padding_x * 0.5f, (main_camera_height_offset + main_camera_height) - missile_camera_height, missile_camera_width, missile_camera_height);

			console_transform.localPosition = new Vector3(console_local_position.x * adjusted_aspect_ratio_y, console_local_position.y * adjusted_aspect_ratio_y, console_local_position.z);
			console_transform.localScale = console_local_scale * adjusted_aspect_ratio_y;

			float occluder_pos_y = 0.5265f;
			Transform occluder = ui_camera.transform.Find("Occluder");
			occluder.localPosition = new Vector3(0.0f, occluder_pos_y * adjusted_aspect_ratio_y, 0.5f);
			occluder.localScale = new Vector3(2.0f, 1.0f, 1.0f) * adjusted_aspect_ratio_y;
		}

		audio_sources = new AudioSource[2];
		for(int i = 0; i < audio_sources.Length; i++) {
			AudioSource source = Util.new_audio_source(transform, "AudioSource" + i);
			source.clip = (AudioClip)Resources.Load("player1_track" + i);
			source.loop = true;

			audio_sources[i] = source;
		}

		audio_sources[0].Play();

		if(game_manager.network_player2_inst != null) {
			game_manager.network_player2_inst.renderer_.enabled = true;
			game_manager.network_player2_inst.collider_.enabled = true;

			Debug.Log("LOG: Showing network player2");
		}

		console_ = Player1Console.new_inst(console_transform);
		console_.enabled = false;
		StartCoroutine(start_console_delayed());
	}

	public IEnumerator fire_missile_(int index) {
		firing_missile = true;
		StartCoroutine(fire_missile());

		UiIndicator indicator = ui_indicators[index];
		for(int i = 0; i < indicator.lines.Length; i++) {
			indicator.lines[i].material.color = Color.red;
			// indicator.lines[i].material.SetColor("_Temperature", Color.red);
		}

		float flash_duration = 0.25f;
		float inv_flash_duration = 1.0f / flash_duration;

		float total_time = Mathf.Infinity;

		float t = 0.0f;
		while(t < total_time) {
			bool flash_off = ((int)(t * inv_flash_duration)) % 2 == 0;
			marker_renderer.material.color = flash_off ? Util.white_no_alpha : Color.red;
			indicator.fill.material.color = flash_off ? Util.white_no_alpha : Util.new_color(Color.red, ui_indicator_alpha);
			// indicator.fill.material.SetColor("_Temperature", indicator.fill.material.color);
			indicator.fill.material.SetColor("_Temperature", flash_off ? Util.white_no_alpha : Util.new_color(Color.green, ui_indicator_alpha));

			t += Time.deltaTime;
			yield return Util.wait_for_frame;

			if(!firing_missile && total_time == Mathf.Infinity) {
				int flash_count = Mathf.CeilToInt(t / flash_duration) | 1;
				total_time = flash_duration * flash_count;
			}
		}

		indicator.fill.material.color = Util.white_no_alpha;
		for(int i = 0; i < indicator.lines.Length; i++) {
			float alpha = ui_indicator_alpha * 0.25f;
			indicator.lines[i].material.color = Util.new_color(Util.white, alpha);
			indicator.lines[i].material.SetColor("_Temperature", Util.new_color(Util.green, alpha));
		}

		yield return null;
	}

	IEnumerator start_console_delayed() {
		camera_fade.alpha = 1.0f;
		if(Settings.USE_TRANSITIONS) {
			yield return new WaitForSeconds(4.9f);
		}

		console_.enabled = true;
	}

	public IEnumerator log_off() {
		// yield return StartCoroutine(Util.lerp_text_color(console_.text_mesh, Color.white, Util.white_no_alpha));

		FadeImageEffect ui_camera_fade = ui_camera.GetComponent<FadeImageEffect>();
		ui_camera_fade.alpha = 0.0f;
		yield return StartCoroutine(FadeImageEffect.lerp_alpha(ui_camera_fade, 1.0f));

		StartCoroutine(Util.lerp_audio_volume(audio_sources[0], 1.0f, 0.0f, 2.0f));
		yield return StartCoroutine(Util.lerp_audio_volume(audio_sources[1], 1.0f, 0.0f, 2.0f));
		for(int i = 0; i < audio_sources.Length; i++) {
			audio_sources[i].Stop();
		}

		game_manager.show_stats(main_camera);
		yield return null;
	}
	
	void Update() {
		Player1Console.update(console_, this);

		bool camera_moved = false;
		float camera_delta = Time.deltaTime * MathExt.TAU * Mathf.Rad2Deg * 0.02f;

		Control look_left = controls[(int)ControlType.LOOK_LEFT];
		if(look_left.enabled && game_manager.get_key(look_left.key)) {
			camera_moved = true;
			camera_xy.x += camera_delta;
		}

		Control look_right = controls[(int)ControlType.LOOK_RIGHT];
		if(look_right.enabled && game_manager.get_key(look_right.key)) {
			camera_moved = true;
			camera_xy.x -= camera_delta; 
		}

		Control look_up = controls[(int)ControlType.LOOK_UP];
		if(look_up.enabled && game_manager.get_key(look_up.key)) {
			camera_moved = true;
			camera_xy.y -= camera_delta;
		}

		Control look_down = controls[(int)ControlType.LOOK_DOWN];
		if(look_down.enabled && game_manager.get_key(look_down.key)) {
			camera_moved = true;
			camera_xy.y += camera_delta;
		}
		
		Control zoom_in = controls[(int)ControlType.ZOOM_IN];
		if(zoom_in.enabled && game_manager.get_key(zoom_in.key)) {
			camera_zoom -= Time.deltaTime * zoom_speed;
		}

		Control zoom_out = controls[(int)ControlType.ZOOM_OUT];
		if(zoom_out.enabled && game_manager.get_key(zoom_out.key)) {
			camera_zoom += Time.deltaTime * zoom_speed;
		}

		Control toggle_infrared = controls[(int)ControlType.TOGGLE_INFRARED];
		if(toggle_infrared.enabled && game_manager.get_key_down(toggle_infrared.key)) {
			infrared_mode = !infrared_mode;
			GameManager.set_infrared_mode(infrared_mode);

			Color ui_color = infrared_mode ? Util.green : Util.white;
			for(int i = 0; i < ui_text_meshes.Length; i++) {
				ui_text_meshes[i].color = ui_color;
			}
		}

		camera_xy.y = Mathf.Clamp(camera_xy.y, -60.0f, 20.0f);

		float drift_amount = Time.deltaTime * 1.2f;
		camera_xy.x += (Mathf.PerlinNoise(Time.time, 0.0f) * 2.0f - 1.0f) * drift_amount;
		camera_xy.y += (Mathf.PerlinNoise(Time.time, 1.0f) * 2.0f - 1.0f) * drift_amount;

		float camera_x_modifier = 1.75f;
		float camera_x = camera_xy.x * camera_x_modifier;
		if(camera_x > 180.0f) {
			camera_x -= 360.0f;
		}
		else if(camera_x < -180.0f) {
			camera_x += 360.0f;
		}

		camera_xy.x = camera_x / camera_x_modifier;

		if(!camera_moved) {
			camera_xy = Vector2.Lerp(camera_xy, Vector2.zero, Time.deltaTime * 0.04f);
		}

		Quaternion camera_ref_rot = camera_ref_zero_rot * Quaternion.Euler(0.0f, 0.0f, camera_xy.x * camera_x_modifier);
		Quaternion camera_rot = camera_zero_rot * Quaternion.Euler(camera_xy.y, 0.0f, 0.0f);

		//TODO: Does this need to use delta time??
		// float damp = Time.deltaTime * 6.0f;
		float damp = 0.2f;
		main_camera.transform.localRotation = Quaternion.Slerp(main_camera.transform.localRotation, camera_rot, damp);
		camera_ref.localRotation = Quaternion.Slerp(camera_ref.localRotation, camera_ref_rot, damp);
		
		angular_pos += Time.deltaTime * -0.024f;
		float x = Mathf.Sin(angular_pos) * GameManager.drone_radius;
		float z = Mathf.Cos(angular_pos) * GameManager.drone_radius;

		transform.position = game_manager.scenario.pos + new Vector3(x, GameManager.drone_height, z);
		transform.rotation = Quaternion.Euler(0.0f, -90.0f + angular_pos * Mathf.Rad2Deg, 0.0f);

		camera_zoom = Mathf.Clamp(camera_zoom, min_fov - zero_fov, max_fov - zero_fov);
		main_camera.fieldOfView = Mathf.Lerp(main_camera.fieldOfView, zero_fov + camera_zoom, Time.smoothDeltaTime * 8.0f);

		{
			float pos_x = transform.position.x + 40.0f;
			float pos_z = transform.position.z + 40.0f;

			float rot_x = (main_camera.transform.forward.x * 0.5f + 0.5f) * 80.0f;
			float rot_y = (main_camera.transform.up.x * 0.5f + 0.5f) * 80.0f;
			float rot_z = (main_camera.transform.forward.z * 0.5f + 0.5f) * 80.0f;

			int z_x = (int)((main_camera.fieldOfView + 4.74f) * 9.773f) + 7337;

			string n_x = ((int)pos_x).ToString("00");
			string n_z = ((int)pos_z).ToString("00");

			string e_x = ((int)rot_x).ToString("00");
			string e_y = ((int)rot_y).ToString("00");
			string e_z = ((int)rot_z).ToString("00");

			string h_x = z_x.ToString("0,000");

			hud_acft_text.text = "    ACFT\nN " + n_x + "°39'" + n_z + "\"\nE " + e_x + "°" + e_y + "'" + e_z+ "\"\n   " + h_x + " HAT";
		}
	}

	void LateUpdate() {
		bool marker_active = false;
		
		if(locked_target != null) {
			Vector3 target_screen_pos = main_camera.WorldToScreenPoint(locked_target.position);
			Ray ray_to_target = hud_camera.ScreenPointToRay(target_screen_pos);

			Plane hud_plane = new Plane(-main_camera.transform.forward, main_camera.transform.position + main_camera.transform.forward);

			float hit_dist = Mathf.Infinity;
			if(hud_plane.Raycast(ray_to_target, out hit_dist)) {
				marker_renderer.transform.position = ray_to_target.GetPoint(hit_dist);
				marker_active = true;
			}
		}

		if(marker_renderer.gameObject.activeSelf != marker_active) {
			marker_renderer.gameObject.SetActive(marker_active);
		}

		Meter.set_pos(meter_x, camera_ref.transform.localEulerAngles.y * Mathf.Deg2Rad * -2.0f);
		Meter.set_pos(meter_y, main_camera.transform.localEulerAngles.x * Mathf.Deg2Rad * 2.0f);
	}

	public IEnumerator fire_missile() {
		firing_missile = true;

		yield return Util.wait_for_2000ms;

		Vector3 missile_direction = main_camera.transform.forward;
		Vector3 missile_position = main_camera.transform.position - missile_direction * 4000.0f;
		float missile_speed = Settings.USE_TRANSITIONS ? 100.0f : 1000.0f;
		float missile_time = Mathf.Sqrt((2.0f * Vector3.Distance(missile_position, game_manager.scenario.pos)) / missile_speed);
		// Debug.Log(missile_time.ToString());

		if(!Settings.INSTALLATION_BUILD) {
			if(!game_manager.connected_to_another_player()) {
				game_manager.network_disconnect();
			}			
		} 

		if(game_manager.network_player2_inst != null && game_manager.connection_type != ConnectionType.OFFLINE) {
			if(game_manager.connected_to_another_player()) {
				game_manager.network_player2_inst.network_view.RPC("missile_fired", RPCMode.Others, missile_position, missile_direction, missile_time);
			}
			else {
				Debug.Log("LOG: Failed to send RPC, no network connection.");
			}
		}

		Transform missile = missile_controller.transform;
		missile.gameObject.SetActive(true);
		missile.parent = null;

		missile_controller.camera_.enabled = true;

		missile.position = missile_position;
		missile.forward = missile_direction;

		{
			Vector3 acceleration = missile_direction * missile_speed;
			Vector3 velocity = Vector3.zero;

			Vector3 start_pos = missile_position;
			float dist_to_target = Vector3.Distance(start_pos, game_manager.scenario.pos);

			bool hit_target = false;
			while(hit_target == false) {
				velocity += acceleration * Time.deltaTime;
				missile_position += velocity * Time.deltaTime;

				float dist_to_missile = Vector3.Distance(start_pos, missile_position);
				if(dist_to_missile >= dist_to_target) {
					missile_position = game_manager.scenario.pos;
					hit_target = true;
				}
				else {
					missile.position = missile_position;

					yield return Util.wait_for_frame;	
				}
			}
		}

		missile_controller.camera_.enabled = false;
		missile.parent = main_camera.transform;

		Environment.play_explosion(game_manager, this, game_manager.env, missile_position);
		firing_missile = false;
	}
}
