
using UnityEngine;
using System.Collections;
using System.Text;

public static class Player1Util {
	public class Parser {
		public string str;
		public int at;
		public int len;
	}

	public enum ParsedCmdInputType {
		NUM,
		STR,
	}

	public class ParsedCmdInput {
		public ParsedCmdInputType type;

		public float num;
		public string str;
	}

	public class ParsedCmd {
		public ParsedCmdInput[] inputs;
		public int input_count;
	}

	public static string CMD_START_TAG = "KIL(";
	public static string CMD_END_TAG = ")";

	public static bool find_match(Parser parser, string match) {
		bool found = false;

		int str_len = parser.len - parser.at;
		if(str_len >= match.Length) {
			int match_len = str_len - (match.Length - 1);

			for(int i = 0; i < match_len; i++) {
				bool matched = true;

				for(int j = 0; j < match.Length; j++) {
					if(parser.str[parser.at + i + j] != match[j]) {
						matched = false;
						break;
					}
				}

				if(matched) {
					parser.at += (i + match.Length);
					found = true;
					break;
				}
			}
		}

		return found;
	}

	public static int require_match(Parser parser, string match) {
		if(!find_match(parser, match)) {
			Assert.invalid_path();
		}

		return parser.at;
	}

	public static string extract_str_match(Parser parser, string start_match, string end_match) {
		string str = "";

		if(find_match(parser, start_match)) {
			int str_at = parser.at;

			require_match(parser, end_match);

			int str_len = parser.at - (str_at + end_match.Length);
			str = parser.str.Substring(str_at, str_len);
		}

		return str;
	}

	public static bool is_new_line(char char_) {
		return char_ == '\n' || char_ == '\r';
	}

	public static bool is_whitespace(char char_) {
		return char_ == ' ' || char_ == '\t' || char_ == '\v' || char_ == '\f' || is_new_line(char_);
	}

	public static ParsedCmd parse_cmd(string str) {
		ParsedCmd cmd = new ParsedCmd();
		cmd.inputs = new ParsedCmdInput[8];
		cmd.input_count = 0;

		int at = 0;
		while(at < str.Length) {
			while(at < str.Length && is_whitespace(str[at])) {
				at++;
			}

			int start = at;
			while(at < str.Length && str[at] != ',') {
				at++;
			}

			int len = at - start;
			Assert.is_true(len > 0);

			Assert.is_true(cmd.input_count < cmd.inputs.Length);

			ParsedCmdInput input = new ParsedCmdInput();
			input.str = str.Substring(start, len);

			if(float.TryParse(input.str, out input.num)) {
				input.type = ParsedCmdInputType.NUM;
			}
			else {
				input.type = ParsedCmdInputType.STR;
				input.num = 0.0f;
			}

			cmd.inputs[cmd.input_count++] = input;

			if(at < str.Length) {
				at++;
			}
		}

		Assert.is_true(cmd.input_count > 0);

		return cmd;
	}

	public static int find_branch_cmd_index(Player1Console.CmdBuf cmd_buf, string str) {
		Assert.is_true(str != null && str != "");

		int index = -1;

		for(int i = 0; i < cmd_buf.elem_count; i++) {
			Player1Console.Cmd cmd = cmd_buf.elems[i];
			if(cmd.type == Player1Console.CmdType.NOOP && cmd.str == str) {
				index = i;
				break;
			}
		}

		return index;
	}

	public static KeyCode get_cmd_input_key(ParsedCmdInput input) {
		Assert.is_true(input.str.Length == 1);

		KeyCode key = Util.char_to_key_code(input.str[0]);
		Assert.is_true(key != KeyCode.None);

		return key;
	}

	public static string get_cmd_input_branch(ParsedCmdInput input) {
		Assert.is_true(input.type == ParsedCmdInputType.STR);
		return input.str.Substring(2, input.str.Length - 4);
	}

	public static void parser_assert(Parser parser, int at, string name, bool expr) {
		if(!expr) {
			Debug.LogError("ERROR[" + name + "]: KIL(" + parser.str.Substring(at));
		}
	}

	public static int parse_script(Player1Console.CmdBuf cmd_buf, string script_path) {
		TextAsset script_asset = (TextAsset)Resources.Load(script_path);

		Parser parser = new Parser();
		parser.str = Util.to_unix_str(script_asset.text);
		parser.at = 0;
		parser.len = parser.str.Length;

		require_match(parser, "<tw-storydata");
		int start_passage = int.Parse(extract_str_match(parser, "startnode=\"", "\""));

		while(find_match(parser, "<tw-passagedata")) {
			string passage_id = extract_str_match(parser, "pid=\"", "\"");
			string passage_name = extract_str_match(parser, "name=\"", "\"");

			require_match(parser, ">");

			Player1Console.Cmd passage_cmd = Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.NOOP);
			passage_cmd.str = passage_name;
			passage_cmd.num = int.Parse(passage_id);

			Parser passage_parser = new Parser();
			passage_parser.str = parser.str;
			passage_parser.at = parser.at;

			string passage_end_tag = "</tw-passagedata>";
			require_match(parser, passage_end_tag);

			passage_parser.len = parser.at - passage_end_tag.Length;

			int print_start = passage_parser.at;
			int print_len = 0;

			while(find_match(passage_parser, CMD_START_TAG)) {
				int cmd_start = passage_parser.at;

				print_len = passage_parser.at - (print_start + CMD_START_TAG.Length);
				if(print_len > 0) {
					Player1Console.push_print_str_cmd(cmd_buf, passage_parser.str.Substring(print_start, print_len));
				}

				require_match(passage_parser, CMD_END_TAG);

				int cmd_len = passage_parser.at - (cmd_start + CMD_END_TAG.Length);
				if(cmd_len > 0) {
					string cmd_str = passage_parser.str.Substring(cmd_start, cmd_len);
					ParsedCmd cmd = parse_cmd(cmd_str);

					switch(cmd.inputs[0].str) {
						case "noop": {
							break;
						}

						case "go_to": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 2);
							parser_assert(passage_parser, cmd_start, passage_name, cmd.inputs[1].type == ParsedCmdInputType.STR);

							Player1Console.push_go_to_cmd(cmd_buf, get_cmd_input_branch(cmd.inputs[1]));

							break;
						}

						case "cls": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 1);

							Player1Console.push_cls_cmd(cmd_buf);

							break;
						}

						case "wait": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 2);
							parser_assert(passage_parser, cmd_start, passage_name, cmd.inputs[1].type == ParsedCmdInputType.NUM);

							float wait_time = cmd.inputs[1].num;
							if(wait_time > 0.0f) {
								Player1Console.push_wait_cmd(cmd_buf, wait_time);
							}

							break;
						}

						case "delay": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 1);
							Player1Console.push_delay_cmd(cmd_buf);
							break;
						}

						case "wait_key": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count >= 2 && cmd.input_count <= 4);

							KeyCode key = get_cmd_input_key(cmd.inputs[1]);
							string branch = cmd.input_count >= 3 ? get_cmd_input_branch(cmd.inputs[2]) : "";
							float timeout = cmd.input_count >= 4 ? cmd.inputs[3].num : Mathf.Infinity;

							Player1Console.Cmd pushed_cmd = Player1Console.push_wait_key_cmd(cmd_buf, key, -1, timeout);
							pushed_cmd.str = branch;

							break;
						}

						case "log_in": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 1);

							Player1Console.push_print_str_cmd(cmd_buf, "USERNAME: ");
							Player1Console.push_user_str_cmd(cmd_buf, Player1Console.UserStrId.USERNAME, 16);
							Player1Console.push_print_str_cmd(cmd_buf, "PASSWORD: ");
							Player1Console.push_user_str_cmd(cmd_buf, Player1Console.UserStrId.PASSWORD, 16, false, true);

							Player1Console.push_delay_cmd(cmd_buf);

							break;
						}

						case "chatter": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 1);

							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.CHATTER);

							break;
						}

						case "log_off": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 1);
							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.LOG_OFF);
							break;
						}

						case "display_on": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 1);
							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.SWITCH_ON_DISPLAY);
							break;
						}

						case "display_off": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 1);
							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.SWITCH_OFF_DISPLAY);
							break;
						}

						case "tutorial": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 1);

							float look_time = 2.5f;
							float zoom_time = 2.0f;

							Player1Console.push_tutorial_key_cmd(cmd_buf, "CAM ROTATE LEFT: HOLD [ A ]", Player1Controller.ControlType.LOOK_LEFT, look_time);
							Player1Console.push_tutorial_key_cmd(cmd_buf, "CAM ROTATE RIGHT: HOLD [ D ]", Player1Controller.ControlType.LOOK_RIGHT, look_time);
							Player1Console.push_tutorial_key_cmd(cmd_buf, "CAM ROTATE UP: HOLD [ W ]", Player1Controller.ControlType.LOOK_UP, look_time);
							Player1Console.push_tutorial_key_cmd(cmd_buf, "CAM ROTATE DOWN: HOLD [ S ]", Player1Controller.ControlType.LOOK_DOWN, look_time);
							Player1Console.push_tutorial_key_cmd(cmd_buf, "CAM ZOOM IN: HOLD [ J ]", Player1Controller.ControlType.ZOOM_IN, zoom_time);
							Player1Console.push_tutorial_key_cmd(cmd_buf, "CAM ZOOM OUT: HOLD [ K ]", Player1Controller.ControlType.ZOOM_OUT, zoom_time);

							Player1Console.push_tutorial_key_cmd(cmd_buf, "IR MODE ON: PRESS [ R ]", Player1Controller.ControlType.TOGGLE_INFRARED, 0.0f, true);
							Player1Console.push_tutorial_key_cmd(cmd_buf, "IR MODE OFF: PRESS [ R ]", Player1Controller.ControlType.TOGGLE_INFRARED, 0.0f, false);

							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.ENABLE_CONTROLS);

							break;
						}

						case "crosshair_style": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 2);
							parser_assert(passage_parser, cmd_start, passage_name, cmd.inputs[1].type == ParsedCmdInputType.NUM);

							Player1Console.Cmd pushed_cmd = Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.CROSSHAIR_STYLE);
							pushed_cmd.num = (int)cmd.inputs[1].num;

							break;
						}

						case "target_style": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 2);
							parser_assert(passage_parser, cmd_start, passage_name, cmd.inputs[1].type == ParsedCmdInputType.NUM);

							Player1Console.Cmd pushed_cmd = Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.TARGET_STYLE);
							pushed_cmd.num = (int)cmd.inputs[1].num;

							break;
						}

						case "fire": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 1);
							Player1Console.push_fire_missile_cmd(cmd_buf);
							break;
						}

						case "death_count": {
							parser_assert(passage_parser, cmd_start, passage_name, cmd.input_count == 1);

							Player1Console.push_print_str_cmd(cmd_buf, "\nENTER NUMBER OF TARGETS NEUTRALISED: ");
							Player1Console.push_confirm_deaths_cmd(cmd_buf);

							break;
						}

						default: {
							Debug.Log("ERROR: Invalid cmd: " + CMD_START_TAG + cmd_str + CMD_END_TAG);
							break;
						}
					}
				}

				while(passage_parser.at < passage_parser.len && is_whitespace(passage_parser.str[passage_parser.at])) {
					if(is_new_line(passage_parser.str[passage_parser.at])) {
						break;
					}
					else {
						passage_parser.at++;
					}
				}

				if(passage_parser.at < passage_parser.len) {
					parser_assert(passage_parser, cmd_start, passage_name, is_new_line(passage_parser.str[passage_parser.at]));
					passage_parser.at++;
				}

				print_start = passage_parser.at;
			}

			print_len = passage_parser.len - print_start;
			if(print_len > 0) {
				Player1Console.push_print_str_cmd(cmd_buf, parser.str.Substring(print_start, print_len));
			}

			//NOTE: We should never reach this!!
			Player1Console.push_wait_cmd(cmd_buf, Mathf.Infinity);
		}

		//TODO: Use a separate table for this to avoid looping over the entire cmd buf!!
		for(int i = 0; i < cmd_buf.elem_count; i++) {
			Player1Console.Cmd cmd = cmd_buf.elems[i];

			switch(cmd.type) {
				case Player1Console.CmdType.GO_TO: {
					if(cmd.next_index < 0) {
						cmd.next_index = find_branch_cmd_index(cmd_buf, cmd.str);
						Assert.is_true(cmd.next_index > -1);
					}

					break;
				}

				case Player1Console.CmdType.WAIT_KEY: {
					if(cmd.str != "") {
						cmd.next_index = find_branch_cmd_index(cmd_buf, cmd.str);
					}

					break;
				}
			}
		}

		int start_index = 0;
		for(int i = 0; i < cmd_buf.elem_count; i++) {
			Player1Console.Cmd cmd = cmd_buf.elems[i];
			if(cmd.type == Player1Console.CmdType.NOOP && cmd.num == start_passage) {
				start_index = cmd.index;
				break;
			}
		}

		return start_index;
	}

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

		Object.Destroy(transform.gameObject.GetComponent<Collider>());

		return transform;
	}

	public static bool flash(float t, float hz = 1.0f) {
		return ((int)(t * hz * 2.0f)) % 2 == 0;
	}
}

public class Player1Console {
	public enum ItemType {
		EMPTY_SPACE,
		TEXT_MESH,
		LOADING_BAR,
	};

	public class Item {
		public Transform transform;
		public float height;

		public ItemType type;

		//NOTE: TEXT_MESH
		public TextMesh text_mesh;
		public Renderer text_mesh_renderer;

		//NOTE: LOAGING_BAR
		public Transform bar;
	}

	public enum CmdType {
		NOOP,

		GO_TO,

		COMMIT_STR,
		PRINT_STR,
		USER_STR,

		WAIT,
		WAIT_KEY,

		TUTORIAL_KEY,

		CHATTER,
		LOG_OFF,

		SWITCH_ON_DISPLAY,
		SWITCH_OFF_DISPLAY,

		ENABLE_CONTROLS,

		CROSSHAIR_STYLE,
		TARGET_STYLE,

		FIRE_MISSILE,
	}

	//TODO: User str table!!
	public enum UserStrId {
		NONE,

		USERNAME,
		PASSWORD,
		DEATH_COUNT,

		COUNT,
	}

	//TODO: This is getting a bit ridiculous, use a base class instead??
	public class Cmd {
		public CmdType type;
		public int index;
		public int next_index;

		public bool cursor_on;
		public bool play_audio;

		public float duration;

		public KeyCode key;
		public Player1Controller.ControlType control_type;

		public int num;

		public string str;
		public UserStrId str_id;
		public int max_str_len;
		public bool numeric_only;
		public bool hide_str;
	}

	public class CmdBuf {
		public Cmd[] elems;
		public int elem_count;
	}

	public static string CURSOR_SYM = "\u2588";
	public static string PROMPT_SYM = "_";

	public static float CHARS_PER_SEC = 50.0f;
	// public static float SECS_PER_CHAR = 0.03f;
	public static float SECS_PER_CHAR = 1.0f / CHARS_PER_SEC;

	public static float DISPLAY_FADE_IN_DURATION = 0.25f;
	public static float DISPLAY_FADE_OUT_DURATION = 1.0f;

	public Transform transform;
	public Renderer renderer;
	public bool enabled;

	public float width;

	public StringBuilder str_builder;

	public Transform text_mesh_prefab;

	public static float LINE_HEIGHT = 0.005f;

	public static float LOADING_BAR_WIDTH = 0.4f;
	public static float LOADING_BAR_HEIGHT = 0.04f;

	public Item[] item_queue;
	public int item_count;
	public int item_head_index;

	public CmdBuf cmd_buf;

	public float cursor_time;
	public float last_cursor_time;
	public bool prompt;
	public int prompt_length;

	public AudioSource print_source;
	public AudioSource prompt_source;
	public AudioSource laser_source;

	public int current_cmd_index;
	public int next_cmd_index;
	public float current_cmd_time;
	public string current_cmd_str;
	public int current_cmd_str_it;
	public bool current_cmd_first_pass;
	public Item current_cmd_item;

	public string[] user_str_table;
	public bool logged_user_details;

	public static int MAX_CMD_COUNT = 1024;

	public static CmdBuf new_cmd_buf(int capacity) {
		CmdBuf buf = new CmdBuf();
		buf.elems = new Cmd[capacity];
		buf.elem_count = 0;
		return buf;
	}

	public static Cmd push_cmd(CmdBuf cmd_buf, CmdType cmd_type) {
		Assert.is_true(cmd_buf.elem_count < cmd_buf.elems.Length);

		Cmd cmd = new Cmd();
		cmd.type = cmd_type;
		cmd.index = cmd_buf.elem_count;
		cmd.next_index = -1;

		cmd.cursor_on = false;
		cmd.play_audio = false;

		cmd.duration = 0.0f;

		cmd.key = KeyCode.None;
		cmd.control_type = Player1Controller.ControlType.COUNT;

		cmd.num = 0;

		cmd.str = "";
		cmd.str_id = UserStrId.NONE;

		cmd_buf.elems[cmd_buf.elem_count++] = cmd;
		return cmd;
	}

	public static Cmd push_go_to_cmd(CmdBuf cmd_buf, int index) {
		Cmd cmd = push_cmd(cmd_buf, CmdType.GO_TO);
		cmd.index = index;
		return cmd;
	}

	public static Cmd push_go_to_cmd(CmdBuf cmd_buf, string name) {
		Cmd cmd = push_cmd(cmd_buf, CmdType.GO_TO);
		cmd.str = name;
		return cmd;
	}

	public static Cmd push_commit_str_cmd(CmdBuf cmd_buf, string str) {
		Cmd cmd = push_cmd(cmd_buf, CmdType.COMMIT_STR);
		cmd.str = str;
		return cmd;
	}

	public static Cmd push_print_str_cmd(CmdBuf cmd_buf, string str) {
		Assert.is_true(str.Length > 0);

		Cmd cmd = push_cmd(cmd_buf, CmdType.PRINT_STR);
		cmd.str = str;
		return cmd;
	}

	public static Cmd push_print_str_cmd(CmdBuf cmd_buf, UserStrId str_id) {
		Assert.is_true(str_id != UserStrId.NONE);

		Cmd cmd = push_cmd(cmd_buf, CmdType.PRINT_STR);
		cmd.str_id = str_id;
		return cmd;
	}

	public static Cmd push_cls_cmd(CmdBuf cmd_buf) {
		return Player1Console.push_print_str_cmd(cmd_buf, "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n");
	}

	public static Cmd push_user_str_cmd(CmdBuf cmd_buf, UserStrId str_id, int max_str_len, bool numeric_only = false, bool hide_str = false, float timeout = Mathf.Infinity) {
		Assert.is_true(max_str_len > 0);
		Assert.is_true(str_id != UserStrId.NONE);

		Cmd cmd = push_cmd(cmd_buf, CmdType.USER_STR);
		cmd.str_id = str_id;
		cmd.max_str_len = max_str_len;
		cmd.numeric_only = numeric_only;
		cmd.hide_str = hide_str;
		cmd.duration = timeout;
		return cmd;
	}

	public static void push_wait_cmd(CmdBuf cmd_buf, float time, bool cursor_on = false) {
		Cmd cmd = push_cmd(cmd_buf, CmdType.WAIT);
		cmd.cursor_on = cursor_on;
		cmd.duration = time;
	}

	public static Cmd push_wait_key_cmd(CmdBuf cmd_buf, KeyCode key, int index = -1, float timeout = Mathf.Infinity) {
		Cmd cmd = push_cmd(cmd_buf, CmdType.WAIT_KEY);
		cmd.key = key;
		cmd.duration = timeout;
		cmd.next_index = index;
		return cmd;
	}

	public static void push_delay_cmd(CmdBuf cmd_buf, string str = ".\n") {
		float duration = 0.5f;

		push_wait_cmd(cmd_buf, duration, true);
		push_commit_str_cmd(cmd_buf, str).play_audio = true;
		push_wait_cmd(cmd_buf, duration, true);
		push_commit_str_cmd(cmd_buf, str).play_audio = true;
		push_wait_cmd(cmd_buf, duration, true);
		push_commit_str_cmd(cmd_buf, str).play_audio = true;
		push_wait_cmd(cmd_buf, duration, true);
	}

	public static void push_tutorial_key_cmd(CmdBuf cmd_buf, string str, Player1Controller.ControlType control_type, float duration, bool enabled = false) {
		push_print_str_cmd(cmd_buf, str);

		if(control_type == Player1Controller.ControlType.TOGGLE_INFRARED) {
			push_print_str_cmd(cmd_buf, "\n");
			push_wait_key_cmd(cmd_buf, KeyCode.R);

			Cmd cmd = Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.ENABLE_CONTROLS);
			cmd.control_type = Player1Controller.ControlType.TOGGLE_INFRARED;
			cmd.num = enabled ? 1 : 0;
		}
		else {
			Cmd cmd = push_cmd(cmd_buf, CmdType.TUTORIAL_KEY);
			cmd.control_type = control_type;
			cmd.duration = duration;
		}

		push_wait_cmd(cmd_buf, 1.0f);
		push_cls_cmd(cmd_buf);
	}

	public static void push_fire_missile_cmd(CmdBuf cmd_buf) {
		push_delay_cmd(cmd_buf);
		push_print_str_cmd(cmd_buf, "\nLAUNCHED\n\n");
		push_cmd(cmd_buf, CmdType.FIRE_MISSILE);
		//TODO: Temp!!
		push_wait_cmd(cmd_buf, 11.0f);
	}

	public static void push_confirm_deaths_cmd(CmdBuf cmd_buf) {
		Cmd cmd = push_user_str_cmd(cmd_buf, UserStrId.DEATH_COUNT, 2, true, false, 10.0f);

		push_delay_cmd(cmd_buf);
		push_print_str_cmd(cmd_buf, "\n");
		push_print_str_cmd(cmd_buf, UserStrId.DEATH_COUNT);
		push_print_str_cmd(cmd_buf, " ");
		Cmd deaths_cmd = push_print_str_cmd(cmd_buf, "DEATHS");
		push_print_str_cmd(cmd_buf, " CONFIRMED\n\n");

		cmd.num = deaths_cmd.index;
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

	public static Item push_empty_space(Player1Console inst, float height) {
		Item item = new Item();
		item.transform = Util.new_transform(inst.transform, "EmptySpace");
		item.height = height;
		item.type = ItemType.EMPTY_SPACE;
		push_back_item(inst, item);
		return item;
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

		Renderer renderer = transform.GetComponent<Renderer>();
		float height = renderer.bounds.size.y + LINE_HEIGHT;

		Item item = new Item();
		item.transform = transform;
		item.height = height;
		item.type = ItemType.TEXT_MESH;
		item.text_mesh = text_mesh;
		item.text_mesh_renderer = text_mesh.GetComponent<Renderer>();
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

	public static Player1Console new_inst(Transform transform, Audio audio) {
		Player1Console inst = new Player1Console();

		inst.transform = transform;
		inst.renderer = transform.GetComponent<Renderer>();
		inst.enabled = true;

		inst.width = 0.28125f * ((float)Screen.width / (float)Screen.height);

		inst.str_builder = new StringBuilder();

		inst.text_mesh_prefab = Util.load_prefab("TextMeshPrefab");

		inst.item_queue = new Item[64];
		inst.item_count = 0;
		inst.item_head_index = 0;

		push_text_mesh(inst, "");

		inst.cursor_time = 0.0f;
		inst.last_cursor_time = -0.5f;
		inst.prompt = false;
		inst.prompt_length = 0;

		inst.print_source = Audio.new_source(audio, transform, Audio.Clip.CONSOLE_PRINT);
		inst.prompt_source = Audio.new_source(audio, transform, Audio.Clip.CONSOLE_PROMPT);
		inst.laser_source = Audio.new_source(audio, transform, Audio.Clip.CONSOLE_LASER);

		inst.cmd_buf = new_cmd_buf(MAX_CMD_COUNT);
		inst.current_cmd_index = Player1Util.parse_script(inst.cmd_buf, "killbox_script");
		inst.next_cmd_index = inst.current_cmd_index + 1;

		inst.current_cmd_time = 0.0f;
		inst.current_cmd_str = "";
		inst.current_cmd_str_it = 0;
		inst.current_cmd_first_pass = true;
		inst.current_cmd_item = null;

		inst.user_str_table = new string[(int)UserStrId.COUNT];
		for(int i = 0; i < inst.user_str_table.Length; i++) {
			inst.user_str_table[i] = "";
		}
		inst.user_str_table[(int)UserStrId.DEATH_COUNT] = "4";
		inst.logged_user_details = false;

		return inst;
	}

	public static void update(Player1Console inst, Player1Controller player1) {
		if(inst.enabled) {
			GameManager game_manager = player1.game_manager;

			float time_left = Time.deltaTime;
			inst.cursor_time += Time.deltaTime;

			bool skip = false;
#if UNITY_EDITOR
			if(game_manager.get_key(KeyCode.Alpha1)) {
				skip = true;
			}
#endif

			Audio.stop_on_next_loop(inst.print_source);

			while(time_left > 0.0f && inst.current_cmd_index < inst.cmd_buf.elem_count) {
				Cmd cmd = inst.cmd_buf.elems[inst.current_cmd_index];
				bool done = false;

				switch(cmd.type) {
					case CmdType.NOOP: {
						done = true;
						break;
					}

					case CmdType.GO_TO: {
						int next_index = cmd.next_index;
						if(next_index > -1) {
							inst.next_cmd_index = next_index;
						}
						else {
							Assert.invalid_path();
						}

						done = true;
						break;
					}

					case CmdType.COMMIT_STR: {
						inst.str_builder.Append(cmd.str);

						if(cmd.play_audio) {
							Audio.play_or_continue_loop(inst.print_source);
						}

						done = true;
						break;
					}

					case CmdType.PRINT_STR: {
						string str = cmd.str_id != UserStrId.NONE ? inst.user_str_table[(int)cmd.str_id] : cmd.str;

						inst.current_cmd_time += time_left;
						int chars_left = str.Length - inst.current_cmd_str_it;
						int chars_to_print = Mathf.Min((int)(inst.current_cmd_time / SECS_PER_CHAR), chars_left);
						inst.current_cmd_time -= chars_to_print * SECS_PER_CHAR;

						for(int i = 0; i < chars_to_print; i++) {
							inst.str_builder.Append(str[inst.current_cmd_str_it++]);
						}

						if(inst.current_cmd_str_it >= str.Length) {
							time_left = inst.current_cmd_time;
							done = true;
						}
						else if(skip) {
							for(int i = inst.current_cmd_str_it; i < str.Length; i++) {
								char char_ = str[inst.current_cmd_str_it++];
								inst.str_builder.Append(char_);
								if(Player1Util.is_new_line(char_)) {
									break;
								}
							}

							time_left = 0.0f;
							done = true;
						}

						Audio.play_or_continue_loop(inst.print_source);

						inst.cursor_time = inst.last_cursor_time = 0.0f;
						break;
					}

					case CmdType.USER_STR: {
						string str = inst.current_cmd_str;

						inst.current_cmd_time += time_left;
						if(inst.current_cmd_time >= cmd.duration) {
							time_left = inst.current_cmd_time - cmd.duration;
							done = true;
						}
						else if(skip) {
							time_left = 0.0f;
							done = true;
						}
						else {
							string input_str = game_manager.get_input_str();
							if(input_str.Length > 0) {
								for(int i = 0; i < input_str.Length; i++) {
									char input_char = input_str[i];
									if(input_char == '\b') {
										if(str.Length > 0) {
											str = str.Substring(0, str.Length - 1);
											inst.str_builder.Remove(inst.str_builder.Length - 1, 1);

											Audio.play(game_manager.audio, Audio.Clip.CONSOLE_USER_TYPING);
										}
									}
									else {
										if(Player1Util.is_new_line(input_char)) {
											if(str.Length > 0) {
												Audio.play(game_manager.audio, Audio.Clip.CONSOLE_USER_KEY);

												done = true;
												break;
											}
										}
										else {
											if(str.Length < cmd.max_str_len) {
												if(!cmd.numeric_only || (input_char >= '0' && input_char <= '9')) {
													str += input_char;
													inst.str_builder.Append(cmd.hide_str ? '*' : input_char);

													Audio.play(game_manager.audio, Audio.Clip.CONSOLE_USER_TYPING);
												}
											}
										}
									}
								}

								inst.current_cmd_str = str;
								inst.cursor_time = inst.last_cursor_time = 0.0f;
							}
						}

						if(done) {
							inst.str_builder.Append('\n');

							if(str.Length > 0) {
								Assert.is_true(cmd.str_id != UserStrId.NONE);
								//TODO: This is a hack!!
								if(cmd.str_id == UserStrId.DEATH_COUNT && str.Equals("1")) {
									inst.cmd_buf.elems[cmd.num].str = "DEATH";
								}
								inst.user_str_table[(int)cmd.str_id] = str;
							}
						}

						break;
					}

					case CmdType.WAIT: {
						inst.current_cmd_time += time_left;
						if(inst.current_cmd_time >= cmd.duration) {
							time_left = inst.current_cmd_time - cmd.duration;
							done = true;
						}

						if(cmd.cursor_on) {
							inst.cursor_time = inst.last_cursor_time = 0.0f;
						}

						if(skip) {
							time_left = 0.0f;
							done = true;
						}

						break;
					}

					case CmdType.WAIT_KEY: {
						inst.current_cmd_time += time_left;
						if(inst.current_cmd_time >= cmd.duration) {
							time_left = inst.current_cmd_time - cmd.duration;
							done = true;
						}
						else {
							if(game_manager.get_key_down(cmd.key)) {
								if(cmd.next_index > -1) {
									inst.next_cmd_index = cmd.next_index;
								}

								Audio.play(game_manager.audio, Audio.Clip.CONSOLE_USER_KEY);

								done = true;
							}
						}

						if(skip) {
							time_left = 0.0f;
							done = true;
						}

						inst.prompt = true;

						break;
					}

					case CmdType.CHATTER: {
						player1.chatter_source.Play();

						done = true;
						break;
					}

					case CmdType.LOG_OFF: {
						string username = inst.user_str_table[(int)UserStrId.USERNAME];
						string password = inst.user_str_table[(int)UserStrId.PASSWORD];
						string kills = inst.user_str_table[(int)UserStrId.DEATH_COUNT];

						System.DateTime date = System.DateTime.Now;
						System.Globalization.CultureInfo locale = new System.Globalization.CultureInfo("en-GB");

						int hours = (int)game_manager.total_playing_time / 60;
						int mins = (int)game_manager.total_playing_time % 60;
						string time = string.Format("{0:00}:{1:00}", hours, mins);

						string details_str = string.Format("{0} - username: {1}, password: {2}, kills: {3}, length: {4}\n", date.ToString(locale), username, password, kills, time);
						Debug.Log(details_str);
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
						System.IO.File.AppendAllText(Application.dataPath + "/passwords.txt", details_str);
#endif

						player1.StartCoroutine(player1.log_off());

						done = true;
						break;
					}

					case CmdType.SWITCH_ON_DISPLAY: {
						player1.StartCoroutine(FadeImageEffect.lerp_alpha(player1.camera_fade, 0.0f, DISPLAY_FADE_IN_DURATION));

						done = true;
						break;
					}

					case CmdType.SWITCH_OFF_DISPLAY: {
						player1.StartCoroutine(FadeImageEffect.lerp_alpha(player1.camera_fade, 1.0f, DISPLAY_FADE_OUT_DURATION));

						done = true;
						break;
					}

					case CmdType.TUTORIAL_KEY: {
						Player1Controller.Control control = player1.controls[(int)cmd.control_type];
						control.enabled = true;

						if(inst.current_cmd_first_pass) {
							Item tail = get_tail_item(inst);
							if(tail.type == ItemType.TEXT_MESH) {
								tail.text_mesh.text = inst.str_builder.ToString();
								inst.str_builder.Remove(0, inst.str_builder.Length);
							}
							else {
								Assert.invalid_path();
							}

							push_empty_space(inst, LINE_HEIGHT * 2.0f);
							push_text_mesh(inst, "");
						}

						if(game_manager.get_key(control.key)) {
							float new_time = inst.current_cmd_time + time_left;

							int step_count = 12;
							int current_progress = (int)((inst.current_cmd_time / cmd.duration) * step_count);
							int new_progress = (int)((new_time / cmd.duration) * step_count);
							if(new_progress > current_progress) {
								inst.str_builder.Append(CURSOR_SYM);
								inst.str_builder.Append(" ");
								Audio.play(game_manager.audio, Audio.Clip.CONSOLE_FILL);
							}

							inst.current_cmd_time = new_time;

							if(inst.current_cmd_time >= cmd.duration) {
								inst.str_builder.Append("\n");
								Audio.play(game_manager.audio, Audio.Clip.CONSOLE_DONE);

								time_left = inst.current_cmd_time - cmd.duration;
								done = true;
							}

							inst.cursor_time = inst.last_cursor_time = 0.0f;
						}

						if(skip) {
							time_left = 0.0f;
							done = true;
						}

						if(done) {
							for(int i = 0; i < player1.controls.Length; i++) {
								player1.controls[i].enabled = false;
							}
						}

						inst.prompt = true;

						break;
					}

					case CmdType.ENABLE_CONTROLS: {
						if(cmd.control_type == Player1Controller.ControlType.COUNT) {
							for(int i = 0; i < player1.controls.Length; i++) {
								player1.controls[i].enabled = true;
							}
						}
						else {
							if(cmd.control_type == Player1Controller.ControlType.TOGGLE_INFRARED) {
								Player1Controller.set_infrared_mode(player1, cmd.num > 0 ? true : false);
								time_left = 0.0f;
							}
						}

						done = true;
						break;
					}

					case CmdType.CROSSHAIR_STYLE: {
						if(cmd.num < player1.crosshair.materials.Length) {
							player1.crosshair.style_id = cmd.num;
							Audio.play(game_manager.audio, Audio.Clip.CONSOLE_UI_CHANGE);
							if(player1.crosshair.style_id == 2) {
								Audio.play_or_continue_loop(inst.laser_source);
							}
							else {
								Audio.stop_on_next_loop(inst.laser_source);
							}
						}
						else {
							Assert.invalid_path();
						}

						done = true;
						break;
					}

					case CmdType.TARGET_STYLE: {
						if(cmd.num < player1.marker.materials.Length) {
							player1.marker.style_id = cmd.num;
							Audio.play(game_manager.audio, Audio.Clip.CONSOLE_UI_CHANGE);
						}
						else {
							Assert.invalid_path();
						}

						done = true;
						break;
					}

					case CmdType.FIRE_MISSILE: {
						player1.StartCoroutine(player1.fire_missile_());

						done = true;
						break;
					}

					default: {
						Debug.Log("LOG: Cmd_" + cmd.type.ToString() + " not implemented!!");

						done = true;
						break;
					}
				}

				if(done) {
					inst.current_cmd_index = inst.next_cmd_index;
					inst.next_cmd_index = inst.current_cmd_index + 1;
					inst.current_cmd_time = 0.0f;
					inst.current_cmd_str = "";
					inst.current_cmd_str_it = 0;
					inst.current_cmd_first_pass = true;
					inst.current_cmd_item = null;

					inst.prompt = false;
					inst.prompt_length = 0;
				}
				else {
					inst.current_cmd_first_pass = false;
					time_left = 0.0f;
				}

				Item tail_item = get_tail_item(inst);

				for(int i = 0; i < inst.str_builder.Length; i++) {
					char c = inst.str_builder[i];

					int new_line_index = -1;
					if(Player1Util.is_new_line(c)) {
						new_line_index = i;
					}
					else {
						tail_item.text_mesh.text = inst.str_builder.ToString(0, i);

						float width = tail_item.text_mesh_renderer.bounds.size.x;
						if(width >= inst.width) {
							int word_start_index = 0;
							for(int j = i - 1; j >= 0; j--) {
								if(inst.str_builder[j] == ' ') {
									word_start_index = j;
									break;
								}
							}

							if(word_start_index != 0) {
								new_line_index = word_start_index;
							}
						}
					}

					if(new_line_index != -1) {
						tail_item.text_mesh.text = inst.str_builder.ToString(0, new_line_index);
						tail_item = push_text_mesh(inst, "");

						inst.str_builder.Remove(0, new_line_index + 1);
						i = -1;
					}
				}

				tail_item.text_mesh.text = inst.str_builder.ToString();

				bool stop_prompt = true;

				if(inst.prompt) {
					float rate = 48.0f;
					float delay = 0.5f;

					if(inst.cursor_time > delay) {
						int current_progress = (int)(inst.last_cursor_time * rate);
						int new_progress = (int)(inst.cursor_time * rate);
						if(new_progress > current_progress) {
							inst.prompt_length++;
						}

						Audio.play_or_continue_loop(inst.prompt_source);

						stop_prompt = false;
					}

					string prompt_str = "";
					for(int i = 0; i < inst.prompt_length; i++) {
						prompt_str += "   ";
					}

					string str = tail_item.text_mesh.text;
					tail_item.text_mesh.text += prompt_str + CURSOR_SYM;

					if(tail_item.text_mesh_renderer.bounds.size.x >= inst.width) {
						inst.prompt_length = 0;
						tail_item.text_mesh.text = str + CURSOR_SYM;
					}
				}
				else {
					if(Player1Util.flash(inst.cursor_time)) {
						tail_item.text_mesh.text += CURSOR_SYM;

						if(!Player1Util.flash(inst.last_cursor_time)) {
							Audio.play(game_manager.audio, Audio.Clip.CONSOLE_CURSOR_FLASH);
						}
					}
				}

				if(stop_prompt) {
					inst.prompt_length = 0;
					Audio.stop_on_next_loop(inst.prompt_source);
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

	public class Marker {
		public Renderer renderer;
		public Color color;
		public Material[] materials;
		public Material off_screen_material;

		public int style_id;
	}

	public class Crosshair {
		public Renderer renderer;
		public Material[] materials;

		public int style_id;
	}

	[System.NonSerialized] public GameManager game_manager = null;

	[System.NonSerialized] public NetworkView network_view;
	[System.NonSerialized] public float join_time_stamp;

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
	float max_fov = 65.0f;
	float zoom_speed = 12.0f;

	MissileController missile_controller;
	int missile_index;

	Camera hud_camera = null;
	TextMesh hud_acft_text = null;

	[System.NonSerialized] public Marker marker;
	[System.NonSerialized] public Crosshair crosshair;
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

	[System.NonSerialized] public AudioSource air_source;
	[System.NonSerialized] public AudioSource chatter_source;

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
		controls[(int)ControlType.ZOOM_IN] = Control.new_inst(KeyCode.J);
		controls[(int)ControlType.ZOOM_OUT] = Control.new_inst(KeyCode.K);
		controls[(int)ControlType.TOGGLE_INFRARED] = Control.new_inst(KeyCode.R);
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

	public static void set_infrared_mode(Player1Controller player1, bool enabled) {
		GameManager.set_infrared_mode(enabled);

		player1.infrared_mode = enabled;
		Color ui_color = enabled ? Util.green : Util.white;
		for(int i = 0; i < player1.ui_text_meshes.Length; i++) {
			player1.ui_text_meshes[i].color = ui_color;
		}
	}

	void Start() {
		camera_aa = main_camera.GetComponent<UnityStandardAssets.ImageEffects.Antialiasing>();
		// camera_aa.enabled = true;

		hud_camera = main_camera.transform.Find("HudCamera").GetComponent<Camera>();
		hud_acft_text = hud_camera.transform.Find("Hud/ACFT").GetComponent<TextMesh>();
		camera_fade = hud_camera.GetComponent<FadeImageEffect>();

		marker = new Marker();
		marker.renderer = hud_camera.transform.Find("Hud/Marker").GetComponent<Renderer>();
		marker.color = Util.white;
		marker.materials = new Material[3];
		marker.materials[0] = (Material)Resources.Load("player1_target_mat");
		marker.materials[1] = (Material)Resources.Load("player1_target_mat");
		marker.materials[2] = (Material)Resources.Load("player1_target_id_mat");
		marker.off_screen_material = (Material)Resources.Load("player1_target_off_screen_mat");
		marker.style_id = 0;

		crosshair = new Crosshair();
		crosshair.renderer = hud_camera.transform.Find("Hud/Crosshair").GetComponent<Renderer>();
		crosshair.materials = new Material[3];
		crosshair.materials[0] = (Material)Resources.Load("player1_crosshair_mat");
		crosshair.materials[1] = (Material)Resources.Load("player1_crosshair_mat");
		crosshair.materials[2] = (Material)Resources.Load("player1_crosshair_locked_mat");
		crosshair.style_id = 1;

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
		// meter_y = Meter.new_inst(hud_transform, new Vector3(-0.525f, 0.0f, 0.0f), Quaternion.identity, 0.025f, 0.015f, 2);
		meter_y = Meter.new_inst(hud_transform, new Vector3(-0.435f, 0.0f, 0.0f), Quaternion.identity, 0.025f, 0.015f, 2);

		missile_controller = main_camera.transform.Find("Missile").GetComponent<MissileController>();
		missile_controller.player1 = this;
		missile_index = 0;

		ui_camera = main_camera.transform.Find("UiCamera").GetComponent<Camera>();
		ui_camera.transform.parent = null;
		ui_camera.transform.position = Vector3.zero;
		ui_camera.transform.rotation = Quaternion.identity;
		FadeImageEffect ui_camera_fade = ui_camera.GetComponent<FadeImageEffect>();
		FadeImageEffect.set_alpha(ui_camera_fade, 0.0f);
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

		air_source = Audio.new_source(game_manager.audio, transform, Audio.Clip.PLAYER1_AIR);
		chatter_source = Audio.new_source(game_manager.audio, transform, Audio.Clip.PLAYER1_CHATTER);

		if(game_manager.network_player2_inst != null) {
			game_manager.network_player2_inst.renderer_.enabled = true;
			game_manager.network_player2_inst.collider_.enabled = true;

			Debug.Log("LOG: Showing network player2");
		}

		console_ = Player1Console.new_inst(console_transform, game_manager.audio);
		console_.enabled = false;
		StartCoroutine(start_console());
	}

	public IEnumerator fire_missile_() {
		Assert.is_true(missile_index < 2);
		int index = missile_index++;

		firing_missile = true;
		StartCoroutine(fire_missile());

		UiIndicator indicator = ui_indicators[index];
		for(int i = 0; i < indicator.lines.Length; i++) {
			indicator.lines[i].material.color = Color.red;
		}

		float flash_duration = 0.25f;
		float flash_hz = 1.0f / (flash_duration * 2.0f);

		float total_time = Mathf.Infinity;

		float t = 0.0f;
		while(t < total_time) {
			bool flash_off = Player1Util.flash(t, flash_hz);
			marker.renderer.material.color = flash_off ? Util.white_no_alpha : Util.red;
			indicator.fill.material.color = flash_off ? Util.white_no_alpha : Util.new_color(Color.red, ui_indicator_alpha);
			indicator.fill.material.SetColor("_Temperature", flash_off ? Util.white_no_alpha : Util.new_color(Color.green, ui_indicator_alpha));

			t += Time.deltaTime;
			yield return Util.wait_for_frame;

			if(!firing_missile && total_time == Mathf.Infinity) {
				int flash_count = Mathf.CeilToInt(t / flash_duration) | 1;
				total_time = flash_duration * flash_count;
			}

			if(flash_off && !Player1Util.flash(t, flash_hz)) {
				Audio.play(game_manager.audio, Audio.Clip.CONSOLE_MISSILE_FLASH);
			}
		}

		indicator.fill.material.color = Util.white_no_alpha;
		for(int i = 0; i < indicator.lines.Length; i++) {
			float alpha = ui_indicator_alpha * 0.25f;
			indicator.lines[i].material.color = Util.new_color(Util.white, alpha);
			indicator.lines[i].material.SetColor("_Temperature", Util.new_color(Util.green, alpha));
		}

		marker.renderer.material.color = Util.white;

		yield return null;
	}

	IEnumerator start_console() {
		FadeImageEffect.set_alpha(camera_fade, 1.0f);

		air_source.volume = 0.0f;
		air_source.Play();

		if(Settings.USE_TRANSITIONS) {
			StartCoroutine(Util.lerp_audio_volume(air_source, 0.0f, 3.0f));
			yield return Util.wait_for_4s;
		}
		else {
			air_source.volume = 1.0f;
		}

		console_.enabled = true;
	}

	public IEnumerator log_off() {
		FadeImageEffect ui_camera_fade = ui_camera.GetComponent<FadeImageEffect>();
		// FadeImageEffect.set_alpha(ui_camera_fade, 0.0f);
		yield return StartCoroutine(FadeImageEffect.lerp_alpha(ui_camera_fade, 1.0f));

		//TODO: Tidy up how we shutdown player1!!
		ui_camera.transform.parent = main_camera.transform;
		console_.enabled = false;

		StartCoroutine(Util.lerp_audio_volume(air_source, 1.0f, 0.0f, 2.0f));
		yield return StartCoroutine(Util.lerp_audio_volume(chatter_source, 1.0f, 0.0f, 2.0f));
		air_source.Stop();
		chatter_source.Stop();

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
			set_infrared_mode(this, !infrared_mode);
		}

		camera_xy.y = Mathf.Clamp(camera_xy.y, -45.0f, 20.0f);

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

		transform.position = game_manager.env.target_point.pos + new Vector3(x, GameManager.drone_height, z);
		transform.rotation = Quaternion.Euler(0.0f, -90.0f + angular_pos * Mathf.Rad2Deg, 0.0f);

		camera_zoom = Mathf.Clamp(camera_zoom, min_fov - zero_fov, max_fov - zero_fov);
		main_camera.fieldOfView = Mathf.Lerp(main_camera.fieldOfView, zero_fov + camera_zoom, Time.smoothDeltaTime * 8.0f);

		//TODO: This string manipulation allocates 3kb+ per frame!!
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

			// hud_acft_text.text = "    ACFT\nN " + n_x + "°39'" + n_z + "\"\nE " + e_x + "°" + e_y + "'" + e_z+ "\"\n   " + h_x + " HAT";
			hud_acft_text.text = "ACFT\nN:    " + n_x + "°39'" + n_z + "\"\nE:    " + e_x + "°" + e_y + "'" + e_z+ "\"\n\nALT:    24,886\nSPD:  192.057";
		}
	}

	void LateUpdate() {
		//TODO: Only change the material when we need to!!
		crosshair.renderer.enabled = crosshair.style_id != 0;
		crosshair.renderer.material = crosshair.materials[crosshair.style_id];

		Transform target = game_manager.env.target_point.high_value_target;
		Assert.is_true(target != null);
		Vector3 target_screen_pos = main_camera.WorldToScreenPoint(target.position);

		bool marker_off_screen = false;
		{
			// float marker_size = 0.0275f * Screen.width;
			float marker_size = 0.005f * Screen.width;

			float viewport_min_x = main_camera.rect.min.x * Screen.width + marker_size;
			float viewport_max_x = main_camera.rect.max.x * Screen.width - marker_size;

			float viewport_min_y = main_camera.rect.min.y * Screen.height + marker_size;
			float viewport_max_y = main_camera.rect.max.y * Screen.height - marker_size;

			if(target_screen_pos.x < viewport_min_x) {
				target_screen_pos.x = viewport_min_x;
				marker_off_screen = true;
			}
			else if(target_screen_pos.x > viewport_max_x) {
				target_screen_pos.x = viewport_max_x;
				marker_off_screen = true;
			}

			if(target_screen_pos.y < viewport_min_y) {
				target_screen_pos.y = viewport_min_y;
				marker_off_screen = true;
			}
			else if(target_screen_pos.y > viewport_max_y) {
				target_screen_pos.y = viewport_max_y;
				marker_off_screen = true;
			}

			// target_screen_pos.x = Mathf.Clamp(target_screen_pos.x, viewport_min_x + marker_size, viewport_max_x - marker_size);
			// target_screen_pos.y = Mathf.Clamp(target_screen_pos.y, viewport_min_y + marker_size, viewport_max_y - marker_size);
		}

		Ray ray_to_target = hud_camera.ScreenPointToRay(target_screen_pos);

		Plane hud_plane = new Plane(-main_camera.transform.forward, main_camera.transform.position + main_camera.transform.forward);

		bool marker_active = false;

		float hit_dist = Mathf.Infinity;
		if(hud_plane.Raycast(ray_to_target, out hit_dist)) {
			marker.renderer.transform.position = ray_to_target.GetPoint(hit_dist);
			marker_active = true;
		}

		Color marker_color = marker.renderer.material.color;
		marker.renderer.enabled = marker.style_id != 0;
		if(marker_off_screen) {
			marker.renderer.material = marker.off_screen_material;
			marker.renderer.transform.localScale = new Vector3(1.547f * 2.0f, 1.16025f * 2.0f, 1.0f);
		}
		else {
			marker.renderer.material = marker.materials[marker.style_id];
			marker.renderer.transform.localScale = new Vector3(1.547f, 1.16025f, 1.0f);
		}
		marker.renderer.material.color = marker_color;

		if(marker.renderer.gameObject.activeSelf != marker_active) {
			marker.renderer.gameObject.SetActive(marker_active);
		}

		Meter.set_pos(meter_x, camera_ref.transform.localEulerAngles.y * Mathf.Deg2Rad * -2.0f);
		Meter.set_pos(meter_y, main_camera.transform.localEulerAngles.x * Mathf.Deg2Rad * 2.0f);
	}

	public IEnumerator fire_missile() {
		firing_missile = true;

		yield return Util.wait_for_2s;

		// Vector3 missile_direction = main_camera.transform.forward;
		Vector3 missile_direction = (game_manager.env.target_point.pos - main_camera.transform.position).normalized;
		Vector3 missile_position = main_camera.transform.position - missile_direction * 4000.0f;
		float missile_speed = Settings.USE_TRANSITIONS ? 100.0f : 1000.0f;
		float missile_time = Mathf.Sqrt((2.0f * Vector3.Distance(missile_position, game_manager.env.target_point.pos)) / missile_speed);
		// Debug.Log(missile_time.ToString());

		if(!Settings.LAN_MODE) {
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
			float dist_to_target = Vector3.Distance(start_pos, game_manager.env.target_point.pos);

			bool hit_target = false;
			while(hit_target == false) {
				velocity += acceleration * Time.deltaTime;
				missile_position += velocity * Time.deltaTime;

				float dist_to_missile = Vector3.Distance(start_pos, missile_position);
				if(dist_to_missile >= dist_to_target) {
					missile_position = game_manager.env.target_point.pos;
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
