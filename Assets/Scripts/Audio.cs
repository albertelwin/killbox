using UnityEngine;
using System.Collections;

public class Audio {
	public enum Clip {
		PLAYER_WALK,
		PLAYER_JUMP,
		PLAYER_LAND,

		NPC,
		COLLECTABLE,

		MISSILE,
		EXPLOSION,
		EXPLOSION_BIRDS,

		CONSOLE_CURSOR_FLASH,
		CONSOLE_TYPING,

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

	public ClipGroup[] clip_groups;

	public static Audio new_inst() {
		Audio audio = new Audio();

		audio.transform = (new GameObject("Audio_")).transform;

		audio.source_pool = new AudioSource[20];
		for(int i = 0; i < audio.source_pool.Length; i++) {
			AudioSource source = Util.new_audio_source(audio.transform, "AudioSourcePool" + i);
			audio.source_pool[i] = source;
		}

		audio.clip_groups = new ClipGroup[(int)Clip.COUNT];
		for(int i = 0; i < audio.clip_groups.Length; i++) {
			ClipGroup clip_group = new ClipGroup();
			clip_group.clip = (Clip)i;
			clip_group.count = 1;

			audio.clip_groups[i] = clip_group;
		}

		//TODO: Figure out a way to automate this!!
		audio.clip_groups[(int)Clip.NPC].count = 9;
		audio.clip_groups[(int)Clip.COLLECTABLE].count = 12;

		int total_clip_count = 0;
		for(int i = 0; i < audio.clip_groups.Length; i++) {
			total_clip_count += audio.clip_groups[i].count;
		}

		audio.clips = new AudioClip[total_clip_count];

		int clip_index = 0;
		for(int i = 0; i < audio.clip_groups.Length; i++) {
			ClipGroup clip_group = audio.clip_groups[i];
			clip_group.first_index = clip_index;

			if(clip_group.count > 1) {
				for(int ii = 0; ii < clip_group.count; ii++) {
					string name = ((Clip)i).ToString().ToLower() + ii;
					audio.clips[clip_index++] = (AudioClip)Resources.Load(name);
				}
			}
			else {
				string name = ((Clip)i).ToString().ToLower();
				audio.clips[clip_index++] = (AudioClip)Resources.Load(name);
			}
		}

		return audio;
	}

	public static AudioClip get_first_clip(Audio audio, Clip clip) {
		ClipGroup clip_group = audio.clip_groups[(int)clip];
		return audio.clips[clip_group.first_index];
	}

	public static AudioClip get_random_clip(Audio audio, Clip clip) {
		ClipGroup clip_group = audio.clip_groups[(int)clip];
		int clip_index = clip_group.first_index + (int)(Random.value * clip_group.count);
		return audio.clips[clip_index];
	}

	public static void play(Audio audio, AudioClip clip, float volume = 1.0f, float pitch = 1.0f) {
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

	public static void play(Audio audio, Clip clip, float volume = 1.0f, float pitch = 1.0f) {
		AudioClip audio_clip = get_first_clip(audio, clip);
		play(audio, audio_clip, volume, pitch);
	}
}