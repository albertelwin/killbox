using UnityEngine;
using System.Collections;

public class Killbox {
	public static IEnumerator show(KillboxController killbox, Vector3 hit_pos, float hit_time) {
		float anim_end_time = 3.0f / killbox.anim_speed + 0.5f;

		if(hit_time > anim_end_time) {
			float wait_time = hit_time - anim_end_time;
			yield return new WaitForSeconds(wait_time);
		}

		killbox.renderer_.enabled = false;
		killbox.transform.position = hit_pos + Vector3.up * killbox.extent;
		killbox.transform.localScale = Vector3.one * killbox.size;

		killbox.animating = true;
		killbox.anim_time = 0.0f;
	}

	public static void hide(KillboxController killbox) {
		killbox.renderer_.enabled = false;

		killbox.animating = false;
		killbox.anim_time = 0.0f;
	}

	public static void update(KillboxController killbox) {
		if(killbox.animating) {
			killbox.anim_time += Time.deltaTime * killbox.anim_speed;
			if(killbox.anim_time > 3.0f) {
				killbox.renderer_.enabled = true;
			}
		}		
	}

	public static void gl_render(KillboxController killbox) {
		if(killbox.animating) {
			killbox.line_material.SetPass(0);

			GL.PushMatrix();
			GL.MultMatrix(killbox.transform.localToWorldMatrix);
			GL.Begin(GL.LINES);

			float t = killbox.anim_time;

			Vector3 p0 = new Vector3( 0.5f, -0.5f, 0.5f);
			Vector3 p1 = new Vector3( 0.5f, -0.5f,-0.5f);
			Vector3 p2 = new Vector3(-0.5f, -0.5f,-0.5f);
			Vector3 p3 = new Vector3(-0.5f, -0.5f, 0.5f);

			GL.Vertex(p0);
			GL.Vertex(Vector3.Lerp(p0, p1, t));

			GL.Vertex(p1);
			GL.Vertex(Vector3.Lerp(p1, p2, t));

			GL.Vertex(p2);
			GL.Vertex(Vector3.Lerp(p2, p3, t));

			GL.Vertex(p3);
			GL.Vertex(Vector3.Lerp(p3, p0, t));

			Vector3 q0 = new Vector3( 0.5f, 0.5f, 0.5f);
			Vector3 q1 = new Vector3( 0.5f, 0.5f,-0.5f);
			Vector3 q2 = new Vector3(-0.5f, 0.5f,-0.5f);
			Vector3 q3 = new Vector3(-0.5f, 0.5f, 0.5f);

			if(t > 1.0f) {
				float tt = t - 1.0f;

				GL.Vertex(p1);
				GL.Vertex(Vector3.Lerp(p1, q1, tt));

				GL.Vertex(p2);
				GL.Vertex(Vector3.Lerp(p2, q2, tt));

				GL.Vertex(p3);
				GL.Vertex(Vector3.Lerp(p3, q3, tt));

				GL.Vertex(p0);
				GL.Vertex(Vector3.Lerp(p0, q0, tt));
			}

			if(t > 2.0f) {
				float tt = t - 2.0f;

				GL.Vertex(q0);
				GL.Vertex(Vector3.Lerp(q0, q1, tt));

				GL.Vertex(q1);
				GL.Vertex(Vector3.Lerp(q1, q2, tt));

				GL.Vertex(q2);
				GL.Vertex(Vector3.Lerp(q2, q3, tt));

				GL.Vertex(q3);
				GL.Vertex(Vector3.Lerp(q3, q0, tt));			
			}

			GL.End();
			GL.PopMatrix();
		}
	}

	public static void gl_line(Vector3 p0, Vector3 p1) {
		GL.Vertex(p0);
		GL.Vertex(p1);
	}

	public static void gl_render_(Transform transform, Material material, float t) {
		material.SetPass(0);

		GL.PushMatrix();
		GL.MultMatrix(transform.localToWorldMatrix);
		GL.Begin(GL.LINES);

		Vector3 p0 = new Vector3( 0.5f, -0.5f, 0.5f);
		Vector3 p1 = new Vector3( 0.5f, -0.5f,-0.5f);
		Vector3 p2 = new Vector3(-0.5f, -0.5f,-0.5f);
		Vector3 p3 = new Vector3(-0.5f, -0.5f, 0.5f);

		Vector3 q0 = new Vector3( 0.5f, 0.5f, 0.5f);
		Vector3 q1 = new Vector3( 0.5f, 0.5f,-0.5f);
		Vector3 q2 = new Vector3(-0.5f, 0.5f,-0.5f);
		Vector3 q3 = new Vector3(-0.5f, 0.5f, 0.5f);

		float d = 4.0f;
		t = MathExt.frac(t / d) * d;

		if(t < 1.0f) {
			float tt = t - 0.0f;

			gl_line(Vector3.Lerp(q0, p0, tt), p0);
			gl_line(Vector3.Lerp(q1, p1, tt), p1);
			gl_line(Vector3.Lerp(q2, p2, tt), p2);
			gl_line(Vector3.Lerp(q3, p3, tt), p3);

			gl_line(p0, Vector3.Lerp(p0, p1, tt));
			gl_line(p1, Vector3.Lerp(p1, p2, tt));
			gl_line(p2, Vector3.Lerp(p2, p3, tt));
			gl_line(p3, Vector3.Lerp(p3, p0, tt));
		}
		else if(t < 2.0f) {
			float tt = t - 1.0f;

			gl_line(Vector3.Lerp(p0, p1, tt), p1);
			gl_line(Vector3.Lerp(p1, p2, tt), p2);
			gl_line(Vector3.Lerp(p2, p3, tt), p3);
			gl_line(Vector3.Lerp(p3, p0, tt), p0);

			gl_line(p1, Vector3.Lerp(p1, q1, tt));
			gl_line(p2, Vector3.Lerp(p2, q2, tt));
			gl_line(p3, Vector3.Lerp(p3, q3, tt));
			gl_line(p0, Vector3.Lerp(p0, q0, tt));
		}
		else if(t < 3.0f) {
			float tt = t - 2.0f;

			gl_line(Vector3.Lerp(p1, q1, tt), q1);
			gl_line(Vector3.Lerp(p2, q2, tt), q2);
			gl_line(Vector3.Lerp(p3, q3, tt), q3);
			gl_line(Vector3.Lerp(p0, q0, tt), q0);

			gl_line(q0, Vector3.Lerp(q0, q1, tt));
			gl_line(q1, Vector3.Lerp(q1, q2, tt));
			gl_line(q2, Vector3.Lerp(q2, q3, tt));
			gl_line(q3, Vector3.Lerp(q3, q0, tt));
		}
		else if(t < 4.0f) {
			float tt = t - 3.0f;

			gl_line(Vector3.Lerp(q0, q1, tt), q1);
			gl_line(Vector3.Lerp(q1, q2, tt), q2);
			gl_line(Vector3.Lerp(q2, q3, tt), q3);
			gl_line(Vector3.Lerp(q3, q0, tt), q0);

			gl_line(q0, Vector3.Lerp(q0, p0, tt));
			gl_line(q1, Vector3.Lerp(q1, p1, tt));
			gl_line(q2, Vector3.Lerp(q2, p2, tt));
			gl_line(q3, Vector3.Lerp(q3, p3, tt));
		}

		GL.End();
		GL.PopMatrix();
	}
}

public class KillboxController : MonoBehaviour {
	[System.NonSerialized] public Renderer renderer_;

	[System.NonSerialized] public Material line_material;

	[System.NonSerialized] public float size;
	[System.NonSerialized] public float extent;

	[System.NonSerialized] public bool animating;
	[System.NonSerialized] public float anim_time;
	[System.NonSerialized] public float anim_speed;

	void Start() {
		renderer_ = GetComponent<Renderer>();
		renderer_.enabled = false;

		line_material = (Material)Resources.Load("box_line_mat");

		size = 100.0f;
		extent = size * 0.5f;

		animating = false;
		anim_time = 0.0f;
		anim_speed = 0.4f;
	}
}