using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class VideoFilterImageEffect : UnityStandardAssets.ImageEffects.ImageEffectBase {
	//NOTE: Medium -> soft
	[Range(-16.0f, -8.0f)] public float scanline_hardness = -8.0f;

	//NOTE: Hard -> soft
	[Range(-4.0f, 0.0f)] public float pixel_hardness = -3.0f;

	[Range(0.0f, 4.0f)] public float mask_dark = 0.5f;
	[Range(0.0f, 4.0f)] public float mask_light = 1.5f;

	[Range(0.0f, 1.0f)] public float saturation = 1.0f;

	void OnRenderImage(RenderTexture src, RenderTexture dst) {
		material.SetFloat("_HardScan", scanline_hardness);
		material.SetFloat("_HardPix", pixel_hardness);

		material.SetVector("_Mask", new Vector4(mask_dark, mask_light, 0.0f, 0.0f));

		material.SetFloat("_Saturation", saturation);

		Graphics.Blit(src, dst, material);
	}
}
