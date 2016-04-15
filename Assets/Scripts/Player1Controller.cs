
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
							Assert.is_true(cmd.input_count == 2);
							Assert.is_true(cmd.inputs[1].type == ParsedCmdInputType.STR);

							Player1Console.push_go_to_cmd(cmd_buf, get_cmd_input_branch(cmd.inputs[1]));

							break;
						}

						case "yes_no": {
							Assert.is_true(cmd.input_count == 3);
							Assert.is_true(cmd.inputs[1].type == ParsedCmdInputType.STR);
							Assert.is_true(cmd.inputs[2].type == ParsedCmdInputType.STR);

							Player1Console.push_yes_no_cmd(cmd_buf, get_cmd_input_branch(cmd.inputs[1]), get_cmd_input_branch(cmd.inputs[2]));

							break;
						}

						case "wait": {
							Assert.is_true(cmd.input_count == 2);
							Assert.is_true(cmd.inputs[1].type == ParsedCmdInputType.NUM);

							float wait_time = cmd.inputs[1].num;
							if(wait_time > 0.0f) {
								Player1Console.push_wait_cmd(cmd_buf, wait_time);
							}

							break;
						}

						case "delay": {
							Assert.is_true(cmd.input_count == 1);
							Player1Console.push_delay_cmd(cmd_buf);
							break;
						}

						case "wait_key": {
							Assert.is_true(cmd.input_count >= 2 && cmd.input_count <= 4);

							KeyCode key = get_cmd_input_key(cmd.inputs[1]);
							string branch = cmd.input_count >= 3 ? get_cmd_input_branch(cmd.inputs[2]) : "";
							float timeout = cmd.input_count >= 4 ? cmd.inputs[3].num : Mathf.Infinity;

							Player1Console.Cmd pushed_cmd = Player1Console.push_wait_key_cmd(cmd_buf, key, -1, timeout);
							pushed_cmd.str = branch;

							break;
						}

						case "log_in": {
							Assert.is_true(cmd.input_count == 1);

							Player1Console.push_print_str_cmd(cmd_buf, "USERNAME: ");
							Player1Console.push_user_str_cmd(cmd_buf, Player1Console.UserStrId.USERNAME, 16);
							Player1Console.push_print_str_cmd(cmd_buf, "PASSWORD: ");
							Player1Console.push_user_str_cmd(cmd_buf, Player1Console.UserStrId.PASSWORD, 16, false, true);

							Player1Console.push_delay_cmd(cmd_buf);

							break;
						}

						case "chatter": {
							Assert.is_true(cmd.input_count == 1);

							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.CHATTER);

							break;
						}

						case "log_off": {
							Assert.is_true(cmd.input_count == 1);
							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.LOG_OFF);
							break;
						}

						case "display_on": {
							Assert.is_true(cmd.input_count == 1);
							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.SWITCH_ON_DISPLAY);
							break;
						}

						case "display_off": {
							Assert.is_true(cmd.input_count == 1);
							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.SWITCH_OFF_DISPLAY);
							break;
						}

						case "tutorial": {
							Assert.is_true(cmd.input_count == 1);

							Player1Console.push_tutorial_key_cmd(cmd_buf, "HOLD \"A\" TO LOOK LEFT\n", Player1Controller.ControlType.LOOK_LEFT, 1.8f, 0);
							Player1Console.push_tutorial_key_cmd(cmd_buf, "HOLD \"D\" TO LOOK RIGHT\n", Player1Controller.ControlType.LOOK_RIGHT, 1.8f, 1);
							Player1Console.push_tutorial_key_cmd(cmd_buf, "HOLD \"W\" TO LOOK UP\n", Player1Controller.ControlType.LOOK_UP, 1.8f, 2);
							Player1Console.push_tutorial_key_cmd(cmd_buf, "HOLD \"S\" TO LOOK DOWN\n", Player1Controller.ControlType.LOOK_DOWN, 1.8f, 3);
							Player1Console.push_tutorial_key_cmd(cmd_buf, "HOLD \"Q\" TO ZOOM IN\n", Player1Controller.ControlType.ZOOM_IN, 1.2f, 4);
							Player1Console.push_tutorial_key_cmd(cmd_buf, "HOLD \"E\" TO ZOOM OUT\n", Player1Controller.ControlType.ZOOM_OUT, 1.2f, 5);

							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.ENABLE_CONTROLS);

							break;
						}

						case "acquire": {
							Assert.is_true(cmd.input_count == 1);
							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.ACQUIRE_TARGET);
							break;
						}

						case "lock": {
							Assert.is_true(cmd.input_count == 1);
							Player1Console.push_cmd(cmd_buf, Player1Console.CmdType.LOCK_TARGET);
							break;
						}

						case "fire": {
							Assert.is_true(cmd.input_count == 1);
							Player1Console.push_fire_missile_cmd(cmd_buf);
							break;
						}

						case "death_count": {
							Assert.is_true(cmd.input_count == 1);

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
					Assert.is_true(is_new_line(passage_parser.str[passage_parser.at]));
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

				case Player1Console.CmdType.YES_NO: {
					cmd.next_index = find_branch_cmd_index(cmd_buf, cmd.str);
					cmd.next_index2 = find_branch_cmd_index(cmd_buf, cmd.str2);
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
		NOOP,

		GO_TO,
		YES_NO,

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

		ACQUIRE_TARGET,
		LOCK_TARGET,

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
		public int next_index2;

		public bool cursor_on;
		public bool play_audio;

		public float duration;

		public KeyCode key;
		public Player1Controller.ControlType control_type;

		public int num;

		public string str;
		public string str2;
		public UserStrId str_id;
		public int max_str_len;
		public bool numeric_only;
		public bool hide_str;
	}

	public class CmdBuf {
		public Cmd[] elems;
		public int elem_count;
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

	public static float SECS_PER_CHAR = 0.03f;

	public static float DISPLAY_FADE_IN_DURATION = 0.25f;
	public static float DISPLAY_FADE_OUT_DURATION = 1.0f;

	public Transform transform;
	public Renderer renderer;
	public bool enabled;

	public float width;

	public string working_text_buffer;

	public Transform text_mesh_prefab;

	public static float LOADING_BAR_WIDTH = 0.4f;
	public static float LOADING_BAR_HEIGHT = 0.04f;

	public Item[] item_queue;
	public int item_count;
	public int item_head_index;

	public CmdBuf cmd_buf;

	public float cursor_time;
	public float last_cursor_time;

	public int current_cmd_index;
	public int next_cmd_index;
	public float current_cmd_time;
	public string current_cmd_str;
	public int current_cmd_str_it;
	public Item current_cmd_item;

	public string[] user_str_table;
	public bool logged_user_details;

	public static int MAX_CMD_COUNT = 512;

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
		cmd.next_index2 = -1;

		cmd.cursor_on = false;
		cmd.play_audio = false;

		cmd.duration = 0.0f;

		cmd.key = KeyCode.None;
		cmd.control_type = Player1Controller.ControlType.COUNT;

		cmd.num = 0;

		cmd.str = "";
		cmd.str2 = "";
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

	public static Cmd push_yes_no_cmd(CmdBuf cmd_buf, string yes, string no) {
		Cmd cmd = push_cmd(cmd_buf, CmdType.YES_NO);
		cmd.str = yes;
		cmd.str2 = no;
		return cmd;
	}

	public static Cmd push_commit_str_cmd(CmdBuf cmd_buf, string str) {
		Cmd cmd = push_cmd(cmd_buf, CmdType.COMMIT_STR);
		cmd.str = str;
		return cmd;
	}

	public static Cmd push_print_str_cmd(CmdBuf cmd_buf, string str) {
		//TODO: Extract html tags, we don't want to print them!!
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

	public static void push_rich_text_str_cmd(CmdBuf cmd_buf, string str_) {
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
							push_print_str_cmd(cmd_buf, str);
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

						push_commit_str_cmd(cmd_buf, substr);
						if(tag.is_glyph) {
							push_wait_cmd(cmd_buf, SECS_PER_CHAR);
						}
					}
				}
			}
		}

		if(str.Length > 0) {
			push_print_str_cmd(cmd_buf, str);
		}
	}

	public static Cmd push_user_str_cmd(CmdBuf cmd_buf, UserStrId str_id, int max_str_len, bool numeric_only = false, bool hide_str = false) {
		Assert.is_true(max_str_len > 0);
		Assert.is_true(str_id != UserStrId.NONE);

		Cmd cmd = push_cmd(cmd_buf, CmdType.USER_STR);
		cmd.str_id = str_id;
		cmd.max_str_len = max_str_len;
		cmd.numeric_only = numeric_only;
		cmd.hide_str = hide_str;
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

	public static void push_tutorial_key_cmd(CmdBuf cmd_buf, string str, Player1Controller.ControlType control_type, float duration, int id) {
		push_print_str_cmd(cmd_buf, str);

		Cmd cmd = push_cmd(cmd_buf, CmdType.TUTORIAL_KEY);
		cmd.control_type = control_type;
		cmd.duration = duration;
		cmd.num = id;

		push_delay_cmd(cmd_buf, "\n");
	}

	public static void push_fire_missile_cmd(CmdBuf cmd_buf) {
		push_print_str_cmd(cmd_buf, "\nREADYING MISSILE...\n");
		push_delay_cmd(cmd_buf);
		push_print_str_cmd(cmd_buf, "\nMISSILE LAUNCHED\n\n");
		push_cmd(cmd_buf, CmdType.FIRE_MISSILE);
		//TODO: Temp!!
		push_wait_cmd(cmd_buf, 11.0f);
	}

	public static void push_confirm_deaths_cmd(CmdBuf cmd_buf) {
		//TODO: Time out!!
		push_user_str_cmd(cmd_buf, UserStrId.DEATH_COUNT, 2, true);

		push_delay_cmd(cmd_buf);
		push_print_str_cmd(cmd_buf, "\n");
		push_print_str_cmd(cmd_buf, UserStrId.DEATH_COUNT);
		push_print_str_cmd(cmd_buf, " DEATHS CONFIRMED\n\n");
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

		inst.width = 0.28125f * ((float)Screen.width / (float)Screen.height);

		inst.working_text_buffer = "";

		inst.text_mesh_prefab = ((GameObject)Resources.Load("TextMeshPrefab")).transform;

		inst.item_queue = new Item[64];
		inst.item_count = 0;
		inst.item_head_index = 0;

		push_text_mesh(inst, inst.working_text_buffer);

		inst.cursor_time = 0.0f;
		inst.last_cursor_time = -0.5f;

		inst.cmd_buf = new_cmd_buf(MAX_CMD_COUNT);
		inst.current_cmd_index = Player1Util.parse_script(inst.cmd_buf, "killbox_script");
		inst.next_cmd_index = inst.current_cmd_index + 1;

		inst.current_cmd_time = 0.0f;
		inst.current_cmd_str = "";
		inst.current_cmd_str_it = 0;
		inst.current_cmd_item = null;

		inst.user_str_table = new string[(int)UserStrId.COUNT];
		for(int i = 0; i < inst.user_str_table.Length; i++) {
			inst.user_str_table[i] = "";
		}
		inst.logged_user_details = false;

		return inst;
	}

	public static bool is_cursor_on(float cursor_time) {
		return ((int)(cursor_time * 2.0f)) % 2 == 0;
	}

	public static void update(Player1Console inst, Player1Controller player1) {
		if(!inst.logged_user_details) {
			string user = inst.user_str_table[(int)UserStrId.USERNAME];
			string pass = inst.user_str_table[(int)UserStrId.PASSWORD];

			if(user != null && user != "" && pass != null && pass != "") {
				string details_str = "username: " + user + ", password: " + pass + "\n";
				Debug.Log(details_str);
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
				System.IO.File.AppendAllText(Application.dataPath + "/passwords.txt", details_str);
#endif
				inst.logged_user_details = true;
			}
		}

		if(inst.enabled) {
			GameManager game_manager = player1.game_manager;

			float time_left = Time.deltaTime;
			inst.cursor_time += Time.deltaTime;

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

					case CmdType.YES_NO: {
						int next_index = -1;
						if(game_manager.get_key_down(KeyCode.Y)) {
							next_index = cmd.next_index;
							Assert.is_true(next_index > -1);
						}
						else if(game_manager.get_key_down(KeyCode.N)) {
							next_index = cmd.next_index2;
							Assert.is_true(next_index > -1);
						}

						if(next_index > -1) {
							inst.next_cmd_index = next_index;
							done = true;
						}

						break;
					}

					case CmdType.COMMIT_STR: {
						inst.working_text_buffer += cmd.str;

						if(cmd.play_audio) {
							Audio.play(game_manager.audio, Audio.Clip.CONSOLE_TYPING);
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
							inst.working_text_buffer += str[inst.current_cmd_str_it++];
						}

						if(chars_to_print > 0) {
							Audio.play(game_manager.audio, Audio.Clip.CONSOLE_TYPING);
						}

						if(inst.current_cmd_str_it >= str.Length) {
							time_left = inst.current_cmd_time;
							done = true;
						}

						inst.cursor_time = inst.last_cursor_time = 0.0f;
						break;
					}

					case CmdType.USER_STR: {
						string str = inst.current_cmd_str;

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
									if(Player1Util.is_new_line(input_char)) {
										if(str.Length > 0) {
											inst.working_text_buffer += "\n";

											Assert.is_true(cmd.str_id != UserStrId.NONE);
											inst.user_str_table[(int)cmd.str_id] = str;

											done = true;
											break;
										}
									}
									else {
										if(str.Length < cmd.max_str_len) {
											if(!cmd.numeric_only || (input_char >= '0' && input_char <= '9')) {
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
							}

							inst.current_cmd_str = str;
							inst.cursor_time = inst.last_cursor_time = 0.0f;
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

								done = true;
							}
						}

						break;
					}

					case CmdType.CHATTER: {
						// player1.audio_sources[0].Stop();
						if(Settings.USE_MUSIC) {
							player1.audio_sources[1].Play();
						}

						done = true;
						break;
					}

					case CmdType.LOG_OFF: {
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

						if(inst.current_cmd_item == null) {
							//TODO: Actually parse the working buffer and push as many text meshes as needed!!
							Item tail = get_tail_item(inst);
							if(tail.type == ItemType.TEXT_MESH) {
								tail.text_mesh.text = "";
							}

							inst.current_cmd_item = push_loading_bar(inst);
							push_text_mesh(inst, "");
						}

						if(game_manager.get_key(control.key)) {
							inst.current_cmd_time += time_left;

							set_loading_bar_progress(inst.current_cmd_item, inst.current_cmd_time / cmd.duration);

							if(inst.current_cmd_time >= cmd.duration) {
								time_left = inst.current_cmd_time - cmd.duration;
								done = true;

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

						done = true;
						break;
					}

					case CmdType.ACQUIRE_TARGET: {
						player1.marker_renderer.material.color = Util.white;
						player1.marker_renderer.material.SetColor("_Temperature", Util.green);
						player1.locked_target = game_manager.scenario.high_value_target;

						done = true;
						break;
					}

					case CmdType.LOCK_TARGET: {
						player1.marker_renderer.material.color = Util.red;
						// player1.marker_renderer.material.SetColor("_Temperature", Util.red);

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
					inst.current_cmd_item = null;
				}
				else {
					time_left = 0.0f;
				}

				Item tail_item = get_tail_item(inst);

				string working_buffer = inst.working_text_buffer;

				for(int i = 0; i < working_buffer.Length; i++) {
					char char_ = working_buffer[i];

					if(Player1Util.is_new_line(char_)) {
						tail_item.text_mesh.text = working_buffer.Substring(0, i);
						tail_item = push_text_mesh(inst, "");

						working_buffer = working_buffer.Substring(i + 1);
						i = -1;
					}
					else {
						tail_item.text_mesh.text = working_buffer.Substring(0, i);

						Renderer renderer = tail_item.text_mesh.GetComponent<Renderer>();
						float width = renderer.bounds.size.x;

						if(width >= inst.width) {
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
								i = -1;
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
	int missile_index;

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
		missile_index = 0;

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

	public IEnumerator fire_missile_() {
		Assert.is_true(missile_index < 2);
		int index = missile_index++;

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
		FadeImageEffect ui_camera_fade = ui_camera.GetComponent<FadeImageEffect>();
		ui_camera_fade.alpha = 0.0f;
		yield return StartCoroutine(FadeImageEffect.lerp_alpha(ui_camera_fade, 1.0f));

		//TODO: Tidy up how we shutdown player1!!
		ui_camera.transform.parent = main_camera.transform;
		console_.enabled = false;

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

		// Vector3 missile_direction = main_camera.transform.forward;
		Vector3 missile_direction = (game_manager.scenario.pos - main_camera.transform.position).normalized;
		Vector3 missile_position = main_camera.transform.position - missile_direction * 4000.0f;
		float missile_speed = Settings.USE_TRANSITIONS ? 100.0f : 1000.0f;
		float missile_time = Mathf.Sqrt((2.0f * Vector3.Distance(missile_position, game_manager.scenario.pos)) / missile_speed);
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
