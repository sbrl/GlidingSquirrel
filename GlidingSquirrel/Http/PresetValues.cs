using System;
using System.Text.RegularExpressions;

namespace SBRL.GlidingSquirrel.Http
{
	/// <summary>
	/// Values for the transfer-encoding header.
	/// </summary>
	public static class TransferEncodings
	{
		/// <summary>
		/// The response's body is being transferred in chunks.
		/// </summary>
		public static readonly string Chunked = "chunked";
		/// <summary>
		/// The response's body is compressed.
		/// </summary>
		public static readonly string Compressed = "compress";
		/// <summary>
		/// The response's body is compressed with the deflate algorithm.
		/// </summary>
		public static readonly string Deflated = "deflate";
		/// <summary>
		/// The response's body is compressed with the gzip algorithm.
		/// </summary>
		public static readonly string Gzipped = "gzip";
		/// <summary>
		/// The response's body is not compressed.
		/// </summary>
		public static readonly string Identity = "identity";

		/// <summary>
		/// Combines multiple transfer-encoding header values.
		/// </summary>
		/// <param name="transferValues">The values to combine.</param>
		/// <returns>The combined values.</returns>
		public static string CombineValues(params string[] transferValues)
		{
			return string.Join(", ", transferValues);
		}
	}

	/// <summary>
	/// Values for the connection header.
	/// </summary>
	public static class Connection
	{
		/// <summary>
		/// The connection should be closed as soon as the request has been served.
		/// </summary>
		public static readonly string Close = "close";
		/// <summary>
		/// The connection may be kept open after the request has been served for future requests.
		/// </summary>
		public static readonly string KeepAlive = "keep-alive";
		/// <summary>
		/// The connection is upgrading to a websocket.
		/// </summary>
		public static readonly string Upgrade = "Upgrade";

		public static bool Contains(string connectionHeaderValue, string targetHeaderValue)
		{
			string[] parts = Regex.Split(connectionHeaderValue.Trim().ToLower(), ", ?");
			foreach(string part in parts)
			{
				if(part == targetHeaderValue.ToLower())
					return true;
			}
			return false;
		}
	}
}
