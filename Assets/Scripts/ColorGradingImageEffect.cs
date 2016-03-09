using System;
using UnityEngine;

[ExecuteInEditMode]
public class ColorGradingImageEffect : UnityStandardAssets.ImageEffects.PostEffectsBase {
	public AnimationCurve red_channel = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
	public AnimationCurve green_channel = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
	public AnimationCurve blue_channel = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));

	Texture2D rgb_channel_tex;

	public float saturation = 1.0f;

	public bool updateTextures = true;

	bool updateTexturesOnStartup = true;
	
	public Shader color_grading_shader = null;
	Material color_grading_material;

	new void Start() {
		base.Start();
		updateTexturesOnStartup = true;
	}

	void Awake() {}

	public override bool CheckResources() {
		color_grading_material = CheckShaderAndCreateMaterial(color_grading_shader, color_grading_material);

		if(!rgb_channel_tex) {
			rgb_channel_tex = new Texture2D(256, 4, TextureFormat.ARGB32, false, true);
		}
			
		rgb_channel_tex.hideFlags = HideFlags.DontSave;
		rgb_channel_tex.wrapMode = TextureWrapMode.Clamp;

		return true;
	}

	public void UpdateParameters() {
		CheckResources(); // textures might not be created if we're tweaking UI while disabled

		if(red_channel != null && green_channel != null && blue_channel != null) {
			for(float i = 0.0f; i <= 1.0f; i += 1.0f / 255.0f) {
				float rCh = Mathf.Clamp(red_channel.Evaluate(i), 0.0f, 1.0f);
				float gCh = Mathf.Clamp(green_channel.Evaluate(i), 0.0f, 1.0f);
				float bCh = Mathf.Clamp(blue_channel.Evaluate(i), 0.0f, 1.0f);

				rgb_channel_tex.SetPixel((int)Mathf.Floor(i * 255.0f), 0, new Color(rCh, rCh, rCh));
				rgb_channel_tex.SetPixel((int)Mathf.Floor(i * 255.0f), 1, new Color(gCh, gCh, gCh));
				rgb_channel_tex.SetPixel((int)Mathf.Floor(i * 255.0f), 2, new Color(bCh, bCh, bCh));
			}

			rgb_channel_tex.Apply();
		}
	}

	void UpdateTextures() {
		UpdateParameters();
	}

	void OnRenderImage(RenderTexture src, RenderTexture dst) {
		if(CheckResources() == false) {
			Graphics.Blit(src, dst);
		}
		else {
			if(updateTexturesOnStartup) {
				UpdateParameters();
				updateTexturesOnStartup = false;
			}

			color_grading_material.SetTexture("_RgbTex", rgb_channel_tex);
			color_grading_material.SetFloat("_Saturation", saturation);

			Graphics.Blit(src, dst, color_grading_material);
		}
	}
}