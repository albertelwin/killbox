
using UnityEngine;

public class FoliageController : MonoBehaviour {
	[System.NonSerialized] public GameManager game_manager;

	[System.NonSerialized] public Animation anim;
	[System.NonSerialized] public Renderer anim_renderer;

	[System.NonSerialized] public Renderer static_renderer;

	[System.NonSerialized] public Vector3 initial_pos;
	[System.NonSerialized] public Quaternion initial_rot;

	[System.NonSerialized] public float rnd;

	public void OnTriggerEnter(Collider other) {
		if(game_manager && game_manager.player2_inst != null && other.transform.parent == game_manager.player2_inst.transform) {
			anim.Blend("hit");
		}
	}

	public void OnDrawGizmos() {
		// transform.localRotation = Quaternion.identity;

	// 	// float y_offset = -0.683f;

	// 	// {
	// 	// 	Vector3 p = transform.localPosition;
	// 	// 	p.y = 1.159f + y_offset;
	// 	// 	transform.localPosition = p;
	// 	// }

	// 	// {
	// 	// 	Transform child = transform.Find("AnimatedMesh");
	// 	// 	Vector3 p = child.localPosition;
	// 	// 	p.y = -1.16f - y_offset;
	// 	// 	child.localPosition = p;
	// 	// }

	// 	// {
	// 	// 	Transform child = transform.Find("StaticMesh");
	// 	// 	Vector3 p = child.localPosition;
	// 	// 	p.y = 0.056f - y_offset;
	// 	// 	child.localPosition = p;
	// 	// }
	}
}