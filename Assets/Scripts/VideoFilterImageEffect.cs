using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class VideoFilterImageEffect : UnityStandardAssets.ImageEffects.ImageEffectBase {
	//TODO: Should this be a float??
	[Range(1, 8)] public int scale = 2;

	//NOTE: Medium -> soft
	[Range(-16.0f, -8.0f)] public float scanline_hardness = -8.0f;

	//NOTE: Hard -> soft
	[Range(-4.0f, 0.0f)] public float pixel_hardness = -3.0f;

	// [Range(0.0f, 1.0f)] public float warp_amount = 0.25f;
	public float warp_amount = 0.25f;

	[Range(0.0f, 4.0f)] public float mask_dark = 0.5f;
	[Range(0.0f, 4.0f)] public float mask_light = 1.5f;

	void OnRenderImage(RenderTexture src, RenderTexture dst) {
		material.SetFloat("_Scale", 1.0f / (float)scale);

		material.SetFloat("_HardScan", scanline_hardness);
		material.SetFloat("_HardPix", pixel_hardness);

		//TODO: Get aspect ratio from texture!!
		Vector2 warp = new Vector2(1.0f / 16.0f, 1.0f / 9.0f) * warp_amount;
		material.SetVector("_WarpAmount", new Vector4(warp.x, warp.y, 0.0f, 0.0f));

		material.SetVector("_Mask", new Vector4(mask_dark, mask_light, 0.0f, 0.0f));

		Graphics.Blit(src, dst, material);
	}
}
