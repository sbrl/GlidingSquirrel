using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SBRL.GlidingSquirrel.Http
{
	/// <summary>
	/// Represents an http response.
	/// </summary>
	public class HttpResponse : HttpMessage
	{
		/// <summary>
		/// The response status code.
		/// </summary>
		public HttpResponseCode ResponseCode = HttpResponseCode.Ok;

		/// <summary>
		/// The size of the buffer used when reading the response body and sending it to the client.
		/// </summary>
		public int ReadBufferSize = 32768;

		/// <summary>
		/// Creates a new http response.
		/// </summary>
		public HttpResponse() : base()
		{
		}

		/// <summary>
		/// Sets the body to be the specified string. This is a utility method that converts the supplied
		/// string into a stream compatible with HttpMessage and sets the appropriate properties to have the
		/// body accepted by the client, such as the ContentLength.
		/// </summary>
		/// <param name="body">The string to set as the body.</param>
		public async Task SetBody(string body)
		{
			MemoryStream ms = new MemoryStream();
			StreamWriter msInput = new StreamWriter(ms) { AutoFlush = true };
			await msInput.WriteAsync(body);
			ms.Position = 0;
			Body = new StreamReader(ms);
			ContentLength = Encoding.UTF8.GetByteCount(body);
		}
        /// <summary>
        /// Sets the body to be the specified byte array.
        /// </summary>
        /// <param name="body">The byte array to set as the new body.</param>
        public async Task SetBody(byte[] body)
        {
            MemoryStream ms = new MemoryStream();
            await ms.WriteAsync(body, 0, body.Length);
            ms.Position = 0;
            Body = new StreamReader(ms);
            ContentLength = body.Length;
        }

		/// <summary>
		/// Attaches a request for HTTP Basic Authentication to this request, and sets the 
		/// HTTP response code to 401 Unauthorised.
		/// </summary>
		/// <param name="realm">The authentication realm to request from the client.</param>
		public void RequireHttpBasicAuthentication(string realm)
		{
			ResponseCode = HttpResponseCode.Unauthorised;
			Headers["WWW-Authenticate"] = $"Basic realm=\"{realm}\"";
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

			if(Body != null)
			{
				// Use a buffer to send the file in chunks
				byte[] buffer = new byte[ReadBufferSize];
				int lastReadSize;
				while(true)
				{
					lastReadSize = await Body.BaseStream.ReadAsync(buffer, 0, ReadBufferSize);
					await destination.BaseStream.WriteAsync(buffer, 0, lastReadSize);

					if(lastReadSize < ReadBufferSize)
						break;
				}
			}
		}
	}
}
