using System;

namespace SBRL.GlidingSquirrel
{
	/// <summary>
	/// A list of logging levels.
	/// </summary>
	public enum LogLevel
	{
		/// <summary>
		/// Messages emitted on startup and shutdown.
		/// </summary>
		System = 150,

		/// <summary>
		/// For critical messages that mean the server is about to crash!
		/// </summary>
		Critical = 100,
		/// <summary>
		/// For serious errors.
		/// </summary>
		Error = 50,
		/// <summary>
		/// For warnings that aren't critically important, but also shouldn't be ignored.
		/// </summary>
		Warning = 25,
		/// <summary>
		/// Informational messages.
		/// </summary>
		Info = 10,
		/// <summary>
		/// Debugging messages that provide extra context to help track down an annoying bug.
		/// </summary>
		Debug = 0,
	}

	/// <summary>
	/// The internal logging class for the http / websockets server. Has properties that let
	/// you customise the logging level for debugging purposes.
	/// </summary>
	public static class Log
	{
		/// <summary>
		/// The minimum logging level messages have to be in order to be displayed.
		/// </summary>
		public static LogLevel LoggingLevel = LogLevel.Warning;

		/// <summary>
		/// Writes a line of test to the standard output, prefixing it for readability purposes.
		/// </summary>
		/// <param name="logLevel">The logging level that this message should be treated as.</param>
		/// <param name="text">The line of text to output. Will have a new line appended to it.</param>
		/// <param name="args">The arguments to substitue into the provided string.</param>
		/// <returns>The number of characters written to the standard output.</returns>
		public static int WriteLine(LogLevel logLevel, string text, params object[] args)
		{
			return Write(logLevel, text + Environment.NewLine, args);
		}
		/// <summary>
		/// Writes a string to the console, prefixing it for readability.
		/// </summary>
		/// <param name="logLevel">The logging level that this message should be treated as.</param>
		/// <param name="text">The line of text to output.</param>
		/// <param name="args">The arguments to substitue into the provided string.</param>
		/// <returns>The number of characters written to the standard output.</returns>
		public static int Write(LogLevel logLevel, string text, params object[] args)
		{
			if((int)logLevel < (int)LoggingLevel)
				return 0;
			
			string outputText = $"[{Env.SecondsSinceStart.ToString("N3")}] " + string.Format(text, args);
			Console.Write(outputText);
			return outputText.Length;
		}
	}
}