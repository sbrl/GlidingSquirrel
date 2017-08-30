using System;
using System.Collections.Generic;
using System.IO;

namespace SBRL.GlidingSquirrel.Http
{
	public abstract class HttpMessage
	{
		public float HttpVersion = 1.0f;

		public Dictionary<string, string> Headers = new Dictionary<string, string>();

		public StreamReader Body;

		public string ContentType {
			get {
				return GetHeaderValue("content-type", "application/octet-stream");
			}
			set {
				Headers["content-type"] = value;
			}
		}
		public int ContentLength {
			get {
				return int.Parse(GetHeaderValue("content-length", "-1"));
			}
			set {
				Headers["content-length"] = value.ToString();
			}
		}
		public string TransferEncoding {
			get {
				return GetHeaderValue("transfer-encoding", "");
			}
			set {
				Headers["transfer-encoding"] = value;
			}
		}

		public HttpMessage()
		{
		}


		public string GetHeaderValue(string headerName, string defaultValue)
		{
			if(Headers.ContainsKey(headerName))
				return Headers[headerName];
			return defaultValue;
		}
	}
}
