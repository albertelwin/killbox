
using UnityEngine;

public class FoliageController : MonoBehaviour {
	[System.NonSerialized] public GameManager game_manager;

	[System.NonSerialized] public Animation anim;
	[System.NonSerialized] public Renderer anim_renderer;

	[System.NonSerialized] public Renderer static_renderer;

	public void OnTriggerEnter(Collider other) {
		if(game_manager && game_manager.player2_inst != null && other.transform.parent == game_manager.player2_inst.transform) {
			anim.Blend("hit");
		}
	}
}