using UnityEngine;
using System.Collections;

public static class Util {
	public static float TAU = 6.283185307179586476925286766559f;

	public static Color white_no_alpha = new Color(1.0f, 1.0f, 1.0f, 0.0f);
	public static Color black_no_alpha = new Color(0.0f, 0.0f, 0.0f, 0.0f);

	public static Color white = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	public static Color black = new Color(0.0f, 0.0f, 0.0f, 1.0f);

	public static Color red = new Color(1.0f, 0.0f, 0.0f, 1.0f);
	public static Color green = new Color(0.0f, 1.0f, 0.0f, 1.0f);
	public static Color blue = new Color(0.0f, 0.0f, 1.0f, 1.0f);

	public static Color sky = new_color(203, 244, 255);
	public static Color day = new_color(16, 17, 19);
	public static Color night = new_color(0, 28, 75);

	public static YieldInstruction wait_for_frame = new WaitForEndOfFrame();
	public static YieldInstruction wait_for_30ms = new WaitForSeconds(0.03f);
	public static YieldInstruction wait_for_40ms = new WaitForSeconds(0.04f);
	public static YieldInstruction wait_for_60ms = new WaitForSeconds(0.06f);
	public static YieldInstruction wait_for_500ms = new WaitForSeconds(0.5f);
	public static YieldInstruction wait_for_1000ms = new WaitForSeconds(1.0f);
	public static YieldInstruction wait_for_2000ms = new WaitForSeconds(2.0f);

	public static Vector3 new_vec3(float x, float y, float z) {
		return new Vector3(x, y, z);
	}

	public static Vector3 new_vec3(Vector2 xy, float z) {
		return new Vector3(xy.x, xy.y, z);
	}

	public static Vector3 set_x(Vector3 yz, float x) {
		return new Vector3(x, yz.y, yz.z);
	}

	public static Vector2 xy(this Vector3 vec) {
		return new Vector2(vec.x, vec.y);
	}

	public static Color new_color(Color rgb, float a) {
		return new Color(rgb.r, rgb.g, rgb.b, a);
	}

	public static Color new_color(int r, int g, int b) {
		return new Color(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
	}

	public static string color_to_hex_str(Color rgb) {
		int r = (int)(rgb.r * 255.0f);
		int g = (int)(rgb.g * 255.0f);
		int b = (int)(rgb.b * 255.0f);

		string str = r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
		return str;
	}

	public static KeyCode char_to_key_code(char char_) {
		KeyCode key = KeyCode.None;

		if(char_ >= 'A' && char_ <= 'Z') {
			int key_int = (int)KeyCode.A + ((int)char_ - 'A');
			key = (KeyCode)key_int;
		}
		else if(char_ >= 'a' && char_ <= 'z') {
			int key_int = (int)KeyCode.A + ((int)char_ - 'a');
			key = (KeyCode)key_int;
		}

		return key;
	}

	public static Transform new_transform(Transform parent, string name, Vector3 pos, Vector3 scale, Quaternion rotation) {
		Transform transform = (new GameObject(name)).transform;
		transform.parent = parent;
		transform.localPosition = pos;
		transform.localScale = scale;
		transform.localRotation = rotation;
		return transform;
	}

	public static Transform new_transform(Transform parent, string name) {
		return new_transform(parent, name, Vector3.zero, Vector3.one, Quaternion.identity);
	}

	public static Transform new_transform(Transform parent, string name, Vector3 pos) {
		return new_transform(parent, name, pos, Vector3.one, Quaternion.identity);
	}

	public static AudioSource new_audio_source(Transform parent, string name) {
		AudioSource audio_source = (new GameObject(name)).AddComponent<AudioSource>();
		audio_source.transform.parent = parent;
		return audio_source;
	}

	public static float rand_11() {
		return Random.value * 2.0f - 1.0f;
	}

	public static Vector3 xyz(Vector4 vec) {
		return new Vector3(vec.x, vec.y, vec.z);
	}

	public static IEnumerator lerp_local_scale(Transform transform, Vector3 from, Vector3 to, float d = 1.0f) {
		transform.localScale = from;

		float t = 0.0f;
		while(t < 1.0f) {
			transform.localScale = Vector3.Lerp(from, to, t);

			t += Time.deltaTime * (1.0f / d);
			yield return wait_for_frame;
		}

		transform.localScale = to;
		yield return null;
	}

	public static IEnumerator lerp_material_color(Renderer renderer, Color from, Color to, float d = 1.0f) {
		renderer.material.color = from;

		float t = 0.0f;
		while(t < 1.0f) {
			renderer.material.color = Color.Lerp(from, to, t);

			t += Time.deltaTime * (1.0f / d);
			yield return wait_for_frame;
		}

		renderer.material.color = to;
		yield return null;
	}

	public static IEnumerator lerp_material_alpha(Renderer renderer, float to, float d = 1.0f) {
		Color from_color = renderer.material.color;
		Color to_color = new_color(from_color, to);

		float t = 0.0f;
		while(t < 1.0f) {
			renderer.material.color = Color.Lerp(from_color, to_color, t);

			t += Time.deltaTime * (1.0f / d);
			yield return wait_for_frame;
		}

		renderer.material.color = to_color;
		yield return null;
	}

	public static IEnumerator lerp_text_color(TextMesh text_mesh, Color from, Color to, float d = 1.0f) {
		text_mesh.color = from;

		float t = 0.0f;
		while(t < 1.0f) {
			text_mesh.color = Color.Lerp(from, to, t);

			t += Time.deltaTime * (1.0f / d);
			yield return wait_for_frame;
		}

		text_mesh.color = to;
		yield return null;
	}

	public static IEnumerator lerp_text_alpha(TextMesh text_mesh, float to, float d = 1.0f) {
		Color from_color = text_mesh.color;
		Color to_color = new_color(from_color, to);

		float t = 0.0f;
		while(t < 1.0f) {
			text_mesh.color = Color.Lerp(from_color, to_color, t);

			t += Time.deltaTime * (1.0f / d);
			yield return wait_for_frame;
		}

		text_mesh.color = to_color;
		yield return null;
	}

	public static IEnumerator lerp_audio_volume(AudioSource audio_source, float from, float to, float d = 1.0f) {
		audio_source.volume = from;

		float t = 0.0f;
		while(t < 1.0f) {
			audio_source.volume = Mathf.Lerp(from, to, t * t);

			t += Time.deltaTime * (1.0f / d);
			yield return wait_for_frame;
		}

		audio_source.volume = to;
		yield return null;
	}

	public static bool raycast_collider(Collider collider, Ray ray, float dist = Mathf.Infinity) {
		bool hit = false;

		RaycastHit hit_info;
		if(collider.Raycast(ray, out hit_info, dist)) {
			hit = true;
		}

		return hit;
	}

	public static int random_index(int array_length) {
	return (int)(Random.value * array_length);
	}

	public static T random_elem<T>(T[] array) {
		int index = (int)(Random.value * array.Length);
		return array[index];
	}

	public static void shuffle_array<T>(T[] array) {
		int r = array.Length;
		while(r > 0) {
			int i = (int)(Random.value * r--);

			T tmp = array[r];
			array[r] = array[i];
			array[i] = tmp;
		}
	}
}
