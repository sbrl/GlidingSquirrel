using System;

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
}
