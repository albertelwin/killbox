using UnityEngine;
using System.Collections;

public static class Assert {
	public const string assert_str_ = "Assert thrown!";

	public static void assert_(bool condition, Object context, string str) {
		if(!condition) {
			Debug.LogError("ASSERT: " + str, context);
#if UNITY_EDITOR
			Debug.Break();
#endif
		}
	}

	public static void is_true(bool condition, string str = assert_str_) {
		assert_(condition, null, str);
	}

	public static void is_true(bool condition, Object context, string str = assert_str_) {
		assert_(condition, context, str);
	}

	public static void invalid_code_path(string str = assert_str_) {
		assert_(false, null, assert_str_);
	}
}