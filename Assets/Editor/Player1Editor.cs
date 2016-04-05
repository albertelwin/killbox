
using UnityEditor;
using UnityEngine;

public class Player1Editor {
	public class Parser {
		public string str;
		public int at;
	}

	public static bool find_match(Parser parser, string match) {
		bool found = false;

		int str_len = parser.str.Length - parser.at;
		if(str_len >= match.Length) {
			int match_len = str_len - match.Length;

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

	[MenuItem("Killbox/Parse Script")]
	public static void parse_script() {
		TextAsset script_asset = (TextAsset)Resources.Load("player1_script");

		Parser parser = new Parser();
		parser.str = script_asset.text;
		parser.at = 0;

		while(find_match(parser, "<tw-passagedata") && find_match(parser, ">")) {
			int entry_start = parser.at;

			string entry_end_tag = "</tw-passagedata>";
			if(!find_match(parser, entry_end_tag)) {
				Assert.invalid_path();
			}

			int entry_len = parser.at - (entry_start + entry_end_tag.Length);

			Debug.Log(parser.str.Substring(entry_start, entry_len));
		}

		// find_match(parser, "<tw-passagedata");
		// if(at > -1) {
		// 	at += entry_begin_tag.Length;
		// 	at = find_match(script_asset.text, at, ">");
		// 	if(at > -1) {
		// 		at++;

		// 		int entry_start = at;
		// 		at = find_match(script_asset.text, at, "</tw-passagedata>");
		// 		Assert.is_true(at > -1);
		// 		int entry_len = at - entry_start;

		// 		Debug.Log(entry_len + script_asset.text.Substring(entry_start, entry_len));

		// 		at += entry_end_tag.Length;
		// 	}
		// }
	}
}