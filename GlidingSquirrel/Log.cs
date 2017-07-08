using System;

namespace SBRL.GlidingSquirrel
{
	public static class Log
	{

		public static int WriteLine(string text, params object[] args)
		{
			return Write(text + Environment.NewLine, args);
		}

		public static int Write(string text, params object[] args)
		{
			string outputText = $"[{Env.SecondsSinceStart.ToString("N3")}] " + string.Format(text, args);
			Console.Write(outputText);
			return outputText.Length;
		}
	}
}