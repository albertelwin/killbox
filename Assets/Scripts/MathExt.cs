
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

	//NOTE: Tavian Barnes Ray-AABB Intersection -> https://tavianator.com/fast-branchless-raybounding-box-intersections/
	public static bool ray_box_intersect(Vector3 min, Vector3 max, Ray ray) {
		float r_dx = 1.0f / ray.direction.x;
		float r_dy = 1.0f / ray.direction.y;
		float r_dz = 1.0f / ray.direction.z;

		float tx1 = (min.x - ray.origin.x) * r_dx;
		float tx2 = (max.x - ray.origin.x) * r_dx;

		float t_min = Mathf.Min(tx1, tx2);
		float t_max = Mathf.Max(tx1, tx2);

		float ty1 = (min.y - ray.origin.y) * r_dy;
		float ty2 = (max.y - ray.origin.y) * r_dy;

		t_min = Mathf.Max(t_min, Mathf.Min(ty1, ty2));
		t_max = Mathf.Min(t_max, Mathf.Max(ty1, ty2));

		float tz1 = (min.z - ray.origin.z) * r_dz;
		float tz2 = (max.z - ray.origin.z) * r_dz;

		t_min = Mathf.Max(t_min, Mathf.Min(tz1, tz2));
		t_max = Mathf.Min(t_max, Mathf.Max(tz1, tz2));

		return t_max >= t_min;
	}
}
