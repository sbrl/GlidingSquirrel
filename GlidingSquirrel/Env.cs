using System;

namespace SBRL.GlidingSquirrel
{
	/// <summary>
	/// A bunch of mildly interesting statistics about the global process, including the number of
	/// seconds since the process was started. You can probably igmore this - it's currently only
	/// the Log class.
	/// </summary>
	public static class Env
	{
		/// <summary>
		/// The time this process was started.
		/// </summary>
		public static readonly DateTime ServerStartTime = DateTime.Now;

		/// <summary>
		/// The number of seconds since this process was started.
		/// </summary>
		public static double SecondsSinceStart {
			get {
				TimeSpan timeSinceStart = DateTime.Now - ServerStartTime;
				return Math.Round(timeSinceStart.TotalSeconds, 4);
			}
		}
	}
}