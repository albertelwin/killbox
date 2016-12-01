
using UnityEditor;
using UnityEngine;
using System.IO;

using Ionic.Zip;

public class Build {
	public class Target {
		public BuildTarget platform;
		public string name;
		public string ext;

		public static Target new_inst(BuildTarget platform, string name, string ext) {
			Target target = new Target();
			target.platform = platform;
			target.name = name;
			target.ext = ext;
			return target;
		}
	}

	public static bool USER_FOLDER = false;
	public static bool ARCHIVE_BUILD = true;
	public static string[] SCENES_IN_BUILD = {
		"Assets/game_scene.unity",
	};

	public static void zip_progress_bar(object sender, SaveProgressEventArgs args) {
		if(args.EventType == ZipProgressEventType.Saving_EntryBytesRead) {
			EditorUtility.DisplayProgressBar("Archiving...", args.CurrentEntry.FileName, (float)args.BytesTransferred / (float)args.TotalBytesToTransfer);
		}
		else if(args.EventType == ZipProgressEventType.Saving_Completed) {
			EditorUtility.ClearProgressBar();
		}
	}

	[MenuItem("Tools/Build %&b")]
	public static void build() {
		string project_name = Application.productName.ToLower();

		string build_folder;
		if(USER_FOLDER) {
			build_folder = EditorUtility.SaveFolderPanel("Build", "", "") + "/";
		}
		else {
			build_folder = Application.dataPath.Remove(Application.dataPath.Length - "Assets".Length) + "Builds/";
		}

		if(build_folder.Length > 1) {
			double begin_timestamp = EditorApplication.timeSinceStartup;

			Target[] targets = new Target[] {
				Target.new_inst(BuildTarget.StandaloneWindows, "win", ".exe"),
				Target.new_inst(BuildTarget.StandaloneOSXUniversal, "osx", ".app"),
			};
			for(int i = 0; i < targets.Length; i++) {
				Target target = targets[i];

				bool has_separate_exe = target.platform != BuildTarget.StandaloneOSXUniversal;

				string exe_name = project_name + target.ext;
				string exe_path = build_folder + exe_name;

				string data_folder = has_separate_exe ? project_name + "_Data" : project_name + target.ext;
				string data_folder_path = build_folder + data_folder;

				if(has_separate_exe && File.Exists(exe_path)) {
					File.Delete(exe_path);
				}
				if(Directory.Exists(data_folder_path)) {
					Directory.Delete(data_folder_path, true);
				}

				string build_result = BuildPipeline.BuildPlayer(SCENES_IN_BUILD, has_separate_exe ? exe_path : data_folder_path, target.platform, BuildOptions.None);
				if(build_result == "") {
					if(ARCHIVE_BUILD) {
						string zip_path = build_folder + project_name + "_" + target.name + ".zip";
						if(File.Exists(zip_path)) {
							File.Delete(zip_path);
						}

						ZipFile zip = new ZipFile();
						zip.SaveProgress += zip_progress_bar;
						if(has_separate_exe) {
							zip.AddFile(exe_path, "");
						}
						zip.AddDirectory(data_folder_path, data_folder);
						zip.Save(zip_path);
						zip.Dispose();
					}
				}
				else {
					Debug.LogError(build_result);
				}
			}

			double total_build_time = EditorApplication.timeSinceStartup - begin_timestamp;
			Debug.Log("Build complete: " + total_build_time.ToString("0.0000"));
		}
	}
}