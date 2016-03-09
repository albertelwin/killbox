using UnityEngine;
using System.Collections;

public class Isometric : MonoBehaviour {
	void Update() {
		Quaternion iso_camera_rotation = Quaternion.Euler(30.0f, -45.0f, 0.0f);
		transform.rotation = Quaternion.Inverse(iso_camera_rotation);
	}
}