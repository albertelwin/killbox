
using UnityEngine;
using System.Collections;

public class Console {
	public Transform transform;

	public Vector2 bounds;
	public Renderer bounds_quad;
	public Renderer occluder_quad;

	public Transform text_mesh_prefab;

	public Transform[] items_queue;
	public int item_count;
	public int item_head_index;

	public static Console new_inst(Transform parent) {
		Console console = new Console();

		console.bounds = new Vector2(5.0f, 8.0f);

		console.transform = (new GameObject("Transform")).transform;
		console.transform.parent = parent;
		console.transform.localPosition = new Vector3(0.514f, -0.554f, 1.0f);
		console.transform.localRotation = Quaternion.identity;
		console.transform.localScale = Vector3.one;

		console.bounds_quad = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Renderer>();
		console.bounds_quad.transform.parent = console.transform;
		console.bounds_quad.transform.localScale = Util.new_vec3(console.bounds, 1.0f);
		console.bounds_quad.transform.localPosition = Util.new_vec3(console.bounds * 0.5f, 0.0f);
		console.bounds_quad.transform.localRotation = Quaternion.identity;
		console.bounds_quad.material = (Material)Resources.Load("hud_mat");
		console.bounds_quad.material.color = Util.red;

		console.occluder_quad = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Renderer>();
		console.occluder_quad.transform.parent = console.transform;
		console.occluder_quad.transform.localScale = Util.new_vec3(console.bounds.x, console.bounds.y * 0.5f, 1.0f);
		console.occluder_quad.transform.localPosition = console.bounds_quad.transform.localPosition + Vector3.up * (console.bounds.y + console.occluder_quad.transform.localScale.y) * 0.5f;
		console.occluder_quad.transform.localRotation = Quaternion.identity;
		console.occluder_quad.material = (Material)Resources.Load("hud_background_mat");
		console.occluder_quad.material.color = Util.black;

		console.text_mesh_prefab = ((GameObject)Resources.Load("TextMeshPrefab")).transform;

		console.items_queue = new Transform[64];
		console.item_count = 0;
		console.item_head_index = 2;

		return console;
	}

	public static void push_back_item(Console console, Transform item) {
		Assert.is_true(console.item_count < console.items_queue.Length);

		int index = (console.item_head_index + console.item_count++) % console.items_queue.Length;
		console.items_queue[index] = item;
	}

	public static Transform get_item(Console console, int it) {
		int index = (console.item_head_index + it) % console.items_queue.Length;

		Transform item = console.items_queue[index];
		Assert.is_true(item != null, index.ToString());
		return item;
	}

	public static Transform pop_front_item(Console console) {
		Assert.is_true(console.item_count > 0);

		Transform item = console.items_queue[console.item_head_index];
		console.items_queue[console.item_head_index] = null;

		console.item_count--;
		console.item_head_index++;
		if(console.item_head_index >= console.items_queue.Length) {
			console.item_head_index = 0;
		}

		return item;
	}

	public static void push_text_mesh(Console console, string str) {
		//TODO: Pool text meshes!!
		Transform transform = (Transform)Object.Instantiate(console.text_mesh_prefab, console.transform.position, Quaternion.identity);
		transform.name = "TextMesh";
		transform.parent = console.transform;
		transform.localScale = Vector3.one;
		transform.GetComponent<TextMesh>().text = str;

		Renderer text_renderer = transform.GetComponent<Renderer>();
		float height = text_renderer.bounds.size.y;
		// Debug.Log(height.ToString());

		for(int i = 0; i < console.item_count; i++) {
			Transform item = get_item(console, i);
			item.localPosition += Vector3.up * height;

			if(item.localPosition.y >= console.bounds.y) {
				Assert.is_true(i == 0);
				GameObject.Destroy(item.gameObject);
				pop_front_item(console);
				i--;
			}
		}

		push_back_item(console, transform);
	}
}

public class ConsoleController : MonoBehaviour {
	[System.NonSerialized] public Console console;

	void Awake() {
		console = Console.new_inst(transform);

		Console.push_text_mesh(console, "Hello, world!");
		Console.push_text_mesh(console, "█");
	}

	void Update() {
		if(Input.GetKey(KeyCode.Return)) {
			Console.push_text_mesh(console, Time.time.ToString());
		}

	}
}