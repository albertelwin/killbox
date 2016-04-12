
using UnityEditor;
using UnityEngine;

public class Player1Editor {
	[MenuItem("Killbox/Validate Script")]
	public static void validate_script() {
		Player1Console.CmdBuf cmd_buf = Player1Console.new_cmd_buf(Player1Console.MAX_CMD_COUNT);
		Player1Util.parse_script(cmd_buf, "killbox_script");
	}
}