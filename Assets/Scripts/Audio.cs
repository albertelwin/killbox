using UnityEngine;
using System.Collections;

public class Audio {
	public enum LoadState {
		UNLOADED,
		LOADING,
		LOADED,
	}

	public enum Clip {
		//TODO: Add these!!
		// PLAYER2_BIRDS,
		// PLAYER2_DRONE,

		PLAYER_WALK,
		PLAYER_JUMP,
		PLAYER_LAND,

		NPC_ADULT,
		NPC_CHILD,
		NPC_CHICKEN,
		SCREAM,
		COLLECTABLE,

		MISSILE,
		EXPLOSION,
		EXPLOSION_BIRDS,

		PLAYER1_AIR,
		PLAYER1_CHATTER,

		CONSOLE_CURSOR_FLASH,
		CONSOLE_PROMPT,
		CONSOLE_FILL,
		CONSOLE_DONE,
		CONSOLE_PRINT,
		CONSOLE_USER_TYPING,
		CONSOLE_USER_KEY,
		CONSOLE_UI_CHANGE,
		CONSOLE_MISSILE_FLASH,
		CONSOLE_LASER,

		COUNT,
	}

	public class ClipGroup {
		public Clip clip;

		public int first_index;
		public int count;
	}

	public Transform transform;

	public AudioSource[] source_pool;
	public AudioClip[] clips;
	public int clip_count;

	public ClipGroup[] clip_groups;

	public LoadState load_state;

	public static void push_audio_clip(Audio audio, AudioClip audio_clip) {
		Assert.is_true(audio.clip_count < audio.clips.Length);
		audio.clips[audio.clip_count++] = audio_clip;
	}

	public static IEnumerator __load(Audio audio, YieldInstruction yield_instruction) {
		if(audio.load_state == LoadState.UNLOADED) {
			audio.load_state = LoadState.LOADING;

			for(int i = 0; i < audio.clip_groups.Length; i++) {
				ClipGroup clip_group = new ClipGroup();
				clip_group.clip = (Clip)i;
				clip_group.first_index = audio.clip_count;
				clip_group.count = 0;

				string clip_id = ((Clip)i).ToString().ToLower();

				AudioClip single_clip = (AudioClip)Resources.Load(clip_id);
				if(single_clip != null) {
					clip_group.count = 1;
					push_audio_clip(audio, single_clip);

					if(yield_instruction != null) {
						yield return yield_instruction;
					}
				}
				else {
					while(true) {
						AudioClip multi_clip = (AudioClip)Resources.Load(clip_id + clip_group.count);
						if(multi_clip != null) {
							clip_group.count++;
							push_audio_clip(audio, multi_clip);

							if(yield_instruction != null) {
								yield return yield_instruction;
							}
						}
						else {
							break;
						}
					}
				}

				Assert.is_true(clip_group.count > 0, "Missing audio file(s): " + clip_id);
				// Debug.Log(clip_id + ": " + clip_group.count);

				audio.clip_groups[i] = clip_group;
			}

			audio.load_state = LoadState.LOADED;

			// Debug.Log("Loaded audio: " + audio.clip_count);
		}
	}

	public static void load(Audio audio, MonoBehaviour mono_behaviour, bool async = false) {
		YieldInstruction yield_instruction = null;
		if(async) {
			yield_instruction = Util.wait_for_10ms;
		}
		mono_behaviour.StartCoroutine(__load(audio, yield_instruction));
	}

	public static Audio new_inst(MonoBehaviour mono_behaviour = null) {
		Audio audio = new Audio();
		audio.load_state = LoadState.UNLOADED;

		audio.transform = (new GameObject("Audio_")).transform;

		audio.source_pool = new AudioSource[20];
		for(int i = 0; i < audio.source_pool.Length; i++) {
			AudioSource source = Util.new_audio_source(audio.transform, "AudioSourcePool" + i);
			audio.source_pool[i] = source;
		}

		audio.clip_count = 0;
		audio.clips = new AudioClip[256];

		audio.clip_groups = new ClipGroup[(int)Clip.COUNT];

		if(mono_behaviour != null) {
			mono_behaviour.StartCoroutine(__load(audio, null));
		}

		return audio;
	}

	public static AudioClip get_clip(Audio audio, Clip clip, int index = 0) {
		AudioClip audio_clip = null;
		if(audio.load_state == LoadState.LOADED) {
			ClipGroup clip_group = audio.clip_groups[(int)clip];
			if(index >= clip_group.count) {
				//TODO: Should this be valid behaviour??
				index = index % clip_group.count;
			}

			audio_clip = audio.clips[clip_group.first_index + index];
		}
		else {
			Assert.invalid_path();
		}

		return audio_clip;
	}

	public static AudioClip get_random_clip(Audio audio, Clip clip) {
		AudioClip audio_clip = null;
		if(audio.load_state == LoadState.LOADED) {
			ClipGroup clip_group = audio.clip_groups[(int)clip];
			int index = clip_group.first_index + (int)(Random.value * clip_group.count);
			audio_clip = audio.clips[index];
		}
		else {
			Assert.invalid_path();
		}

		return audio_clip;
	}

	public static void play(Audio audio, AudioClip clip, float volume = 1.0f, float pitch = 1.0f) {
		if(clip != null) {
			for(int i = 0; i < audio.source_pool.Length; i++) {
				AudioSource source = audio.source_pool[i];
				if(!source.isPlaying) {
					source.clip = clip;
					source.volume = volume;
					source.pitch = pitch;
					source.Play();

					break;
				}
			}
		}
	}

	public static void play(Audio audio, Clip clip, float volume = 1.0f, float pitch = 1.0f) {
		if(audio.load_state == LoadState.LOADED) {
			AudioClip audio_clip = get_clip(audio, clip);
			play(audio, audio_clip, volume, pitch);
		}
	}

	public static AudioSource new_source(Audio audio, Transform parent, Clip clip) {
		AudioSource source = Util.new_audio_source(parent, clip.ToString() + "AudioSource");
		source.clip = get_clip(audio, clip);
		source.loop = true;
		return source;
	}

	public static void play_or_continue_loop(AudioSource source) {
		source.loop = true;
		if(!source.isPlaying) {
			source.Play();
		}
	}

	public static void stop_on_next_loop(AudioSource source) {
		source.loop = false;
	}
}