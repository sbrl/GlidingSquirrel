using System;
namespace SBRL.GlidingSquirrel.Http
{
	/// <summary>
	/// Represents a set of credentials recieved via HTTP Basic authentication.
	/// </summary>
	public class HttpBasicAuthCredentials
	{
		/// <summary>
		/// The username received.
		/// </summary>
		public readonly string Username;
		/// <summary>
		/// The password received.
		/// </summary>
		public readonly string Password;

		public HttpBasicAuthCredentials(string inUsername, string inPassword)
		{
			Username = inUsername;
			Password = inPassword;
		}
	}
}
