
using UnityEditor;
using UnityEngine;

public class Player1Editor {
	[MenuItem("Killbox/Validate Script")]
	public static void validate_script() {
		Player1Console.CmdBuf cmd_buf = Player1Console.new_cmd_buf(512);
		Player1Util.parse_script(cmd_buf, "player1_script");

		for(int i = 0; i < cmd_buf.elem_count; i++) {
			Player1Console.Cmd cmd = cmd_buf.elems[i];
			Debug.Log(cmd.type.ToString());
		}
	}
}