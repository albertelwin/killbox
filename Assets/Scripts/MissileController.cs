using UnityEngine;
using System.Collections;

public class MissileController : MonoBehaviour {
	[System.NonSerialized] public Camera camera_;

	[System.NonSerialized] public Player1Controller player1;

	void Awake() {
		camera_ = transform.GetComponent<Camera>();
	}

	void OnPreRender() {
		GameManager.set_infrared_mode(false);
	}

	void OnPostRender() {
		GameManager.set_infrared_mode(player1.infrared_mode);
	}
}