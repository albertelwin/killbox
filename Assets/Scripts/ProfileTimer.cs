
using UnityEngine;

public class ProfileTimer {
	public System.Diagnostics.Stopwatch stop_watch;
	public string name;

	public static ProfileTimer new_inst() {
		ProfileTimer timer = new ProfileTimer();
		timer.stop_watch = new System.Diagnostics.Stopwatch();
		timer.name = "NULL";
		return timer;
	}

	public void s(string name) {
		this.name = name;
		this.stop_watch.Reset();
		this.stop_watch.Start();
	}

	public void e() {
		this.stop_watch.Stop();
		Debug.Log(name + ": " + stop_watch.ElapsedMilliseconds);
	}
}