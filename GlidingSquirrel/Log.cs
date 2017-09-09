using System;

namespace SBRL.GlidingSquirrel
{
	/// <summary>
	/// The internal logging class for the http / websockets server. (Will) have properties that let
	/// you customise the logging level for debugging purposes.
	/// </summary>
	public static class Log
	{
		/// <summary>
		/// Writes a line of test to the standard output, prefixing it for readability purposes.
		/// </summary>
		/// <param name="text">The line of text to output. Will have a new line appended to it.</param>
		/// <param name="args">The arguments to substitue into the provided string.</param>
		/// <returns>The number of characters written to the standard output.</returns>
		public static int WriteLine(string text, params object[] args)
		{
			return Write(text + Environment.NewLine, args);
		}
		/// <summary>
		/// Writes a string to the console, prefixing it for readability.
		/// </summary>
		/// <param name="text">The line of text to output.</param>
		/// <param name="args">The arguments to substitue into the provided string.</param>
		/// <returns>The number of characters written to the standard output.</returns>
		public static int Write(string text, params object[] args)
		{
			string outputText = $"[{Env.SecondsSinceStart.ToString("N3")}] " + string.Format(text, args);
			Console.Write(outputText);
			return outputText.Length;
		}
	}
}