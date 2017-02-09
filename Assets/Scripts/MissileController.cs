using UnityEngine;
using System.Collections;

public class MissileController : MonoBehaviour {
	[System.NonSerialized] public Player1Controller player1;

	void OnPreRender() {
		GameManager.set_infrared_mode(false);
	}

	void OnPostRender() {
		GameManager.set_infrared_mode(player1.infrared_mode);
	}
}