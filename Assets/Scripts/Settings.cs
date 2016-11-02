using UnityEngine;
using System.Collections;

public static class Settings {
	public static bool INSTALLATION_BUILD = true;

	public static bool LAN_MODE = false;
	public static bool LAN_SERVER_MACHINE = true;
	public static string LAN_SERVER_IP = "192.168.0.2";
	public static int LAN_SERVER_PORT = 25003;
	public static bool LAN_FORCE_CONNECTION = true;
	public static bool FORCE_OFFLINE_MODE = true;

	public static bool USE_SPLASH = false;
	public static bool USE_TRANSITIONS = true;
	public static bool USE_DEATH_VIEW = true;
}