using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class FadeImageEffect : UnityStandardAssets.ImageEffects.ImageEffectBase {
	[Range(0, 1)]
	public float alpha = 0.0f;

	public static IEnumerator lerp_alpha(FadeImageEffect image_effect, float to, float d = 1.0f) {
		float from = image_effect.alpha;

		float t = 0.0f;
		while(t < 1.0f) {
			image_effect.alpha = Mathf.Lerp(from, to, t);

			t += Time.deltaTime * (1.0f / d);
			yield return Util.wait_for_frame;
		}

		image_effect.alpha = to;
	}

	void OnRenderImage(RenderTexture src, RenderTexture dst) {
		float clamped_alpha = Mathf.Clamp01(alpha);
		if(clamped_alpha > 0.0f) {
			material.SetFloat("_Alpha", clamped_alpha);
			Graphics.Blit(src, dst, material);			
		}
		else {
			//TODO: Is this free?
			Graphics.Blit(src, dst);
		}
	}
}
