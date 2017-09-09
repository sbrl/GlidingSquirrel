using System;
using System.Collections.Generic;
using System.IO;

namespace SBRL.GlidingSquirrel.Http
{
	/// <summary>
	/// Represents a general HTTP message. May be either a request or a response.
	/// This is the base class for all the common functionality that HttpRequest and HttpResponse share.
	/// </summary>
	public abstract class HttpMessage
	{
		/// <summary>
		/// The version of the HTTP protocol in use.
		/// </summary>
		public float HttpVersion = 1.0f;
		/// <summary>
		/// A dictionary of key => value headers present in the message. 
		/// </summary>
		public Dictionary<string, string> Headers = new Dictionary<string, string>();
		/// <summary>
		/// The request body.
		/// </summary>
		public StreamReader Body;
		/// <summary>
		/// Quality of life property providing quick access to the content type of the message.
		/// </summary>
		public string ContentType {
			get {
				return GetHeaderValue("content-type", "application/octet-stream");
			}
			set {
				Headers["content-type"] = value;
			}
		}
		/// <summary>
		/// Quality of life property providing quick access to the length of the body of the message.
		/// </summary>
		public int ContentLength {
			get {
				return int.Parse(GetHeaderValue("content-length", "-1"));
			}
			set {
				Headers["content-length"] = value.ToString();
			}
		}
		/// <summary>
		/// Quality of life property providing quick access to the transfer encoding of the body of the message.
		/// </summary>
		public string TransferEncoding {
			get {
				return GetHeaderValue("transfer-encoding", "");
			}
			set {
				Headers["transfer-encoding"] = value;
			}
		}

		/// <summary>
		/// Creates a new HttpMessage class instance.
		/// </summary>
		public HttpMessage()
		{
		}

		/// <summary>
		/// Fetches the value of a header present in the message. Returns the specified default
		/// value if no value was found.
		/// </summary>
		/// <returns>The value of the header name specified.</returns>
		/// <param name="headerName">The name of the header to fetch the value for.</param>
		/// <param name="defaultValue">The default value to return if no header value can be found.</param>
		public string GetHeaderValue(string headerName, string defaultValue)
		{
			if(Headers.ContainsKey(headerName))
				return Headers[headerName];
			return defaultValue;
		}
	}
}
