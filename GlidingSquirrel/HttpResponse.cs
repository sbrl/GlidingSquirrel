using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SBRL.GlidingSquirrel
{
	public class HttpResponse
	{
		public float HttpVersion = 1.0f;
		public HttpResponseCode ResponseCode = HttpResponseCode.Ok;
		public Dictionary<string, string> Headers = new Dictionary<string, string>();

		public StreamReader Body;
		public int ReadBufferSize = 32768;

		public HttpResponse()
		{
		}

		public async Task SetBody(string body)
		{
			MemoryStream ms = new MemoryStream();
			StreamWriter msInput = new StreamWriter(ms) { AutoFlush = true };
			await msInput.WriteAsync(body);
			ms.Position = 0;
			Body = new StreamReader(ms);
		}

		/// <summary>
		/// Sends this HttpResponse to the specified destination.
		/// </summary>
		/// <param name="destination">The StreamWriter to send the response to.</param>
		public async Task SendTo(StreamWriter destination)
		{
			// Write the first line out
			await destination.WriteAsync(string.Format(
				"HTTP/{0:0.0} {1} {2}\r\n",
				HttpVersion,
				ResponseCode.Code,
				ResponseCode.Message
			));
			// Write the headers out
			foreach(KeyValuePair<string, string> header in Headers)
				await destination.WriteAsync($"{header.Key}: {header.Value}\r\n");

			await destination.WriteAsync("\r\n");

			// Use a buffer to send the file in chunks
			byte[] buffer = new byte[ReadBufferSize];
			int lastReadSize;
			while((lastReadSize = await Body.BaseStream.ReadAsync(buffer, 0, ReadBufferSize)) < ReadBufferSize)
				await destination.BaseStream.WriteAsync(buffer, 0, lastReadSize);
		}
	}
}
