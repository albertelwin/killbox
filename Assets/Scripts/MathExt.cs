using UnityEngine;
using System.Collections;

public static class MathExt {
	public static float TAU = 6.28318530718f;

	public static float frac(float x) {
		return x - (int)x;
	}

	public static float ease(float x, float y, float t) {
		float t2 = t * t;
		float u = (1.0f - t);
		float u2 = u * u;
		return 3.0f * t * u2 * x + 3.0f * t2 * u * y + (t2 * t);
	}
}
