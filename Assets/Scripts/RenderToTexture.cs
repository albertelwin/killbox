using UnityEngine;
using System.Collections;

public class RenderToTexture : MonoBehaviour {
	public class RenderTextureCamera {
		public Camera camera;
		public Renderer renderer;
		public RenderTexture render_texture;
	}

	public class PaddingOffset {
		public Vector3 scale_offset;
		public Vector3 position_offset;
	}

	PaddingOffset padding_offset(Vector2 scale_offset, Vector2 position_offset) {
		PaddingOffset padding_offset_ = new PaddingOffset();
		padding_offset_.scale_offset = new Vector3(scale_offset.x, scale_offset.y, 0.0f);
		padding_offset_.position_offset = new Vector3(position_offset.x, position_offset.y, 0.0f);
		return padding_offset_;
	}

	RenderTextureCamera[] cameras = new RenderTextureCamera[3];

	void Start() {
		Transform camera_group = GameObject.Find("CameraGroup").transform;
		Transform quad_group = GameObject.Find("Screen").transform;

		//TODO: Letterboxing for non-16:9 aspect ratios!!
		float screen_padding = 0.125f;
		float half_screen_padding = screen_padding * 0.5f;
		PaddingOffset[] offsets = {
			padding_offset(new Vector2(screen_padding, screen_padding), Vector2.zero),
			padding_offset(new Vector2(half_screen_padding, half_screen_padding), new Vector2(-half_screen_padding * 0.5f, -half_screen_padding * 0.5f)),
			padding_offset(new Vector2(half_screen_padding, screen_padding), new Vector2(-half_screen_padding * 0.5f, 0.0f)),
		};

		for(int i = 0; i < cameras.Length; i++) {
			RenderTextureCamera cam = new RenderTextureCamera();
			cam.camera = camera_group.Find("Camera" + i).GetComponent<Camera>();
			cam.renderer = quad_group.Find("Quad" + i).GetComponent<Renderer>();

			PaddingOffset offset = offsets[i];
			cam.renderer.transform.localScale -= offset.scale_offset;
			cam.renderer.transform.localPosition += offset.position_offset;

			int tex_width = (int)(cam.renderer.transform.localScale.x * (Screen.width / 16));
			int tex_height = (int)(cam.renderer.transform.localScale.y * (Screen.width / 16));

			cam.render_texture = new RenderTexture(tex_width, tex_height, 24);
			cam.render_texture.antiAliasing = 8;
			cam.render_texture.Create();
			cam.camera.targetTexture = cam.render_texture;
			cam.renderer.material.mainTexture = cam.render_texture;

			cameras[i] = cam;
		}
	}
}
