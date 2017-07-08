using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SBRL.GlidingSquirrel
{
	public class HttpResponse
	{
		public HttpResponseCode ResponseCode = HttpResponseCode.Ok;
		public Dictionary<string, string> Headers = new Dictionary<string, string>();

		public StreamReader Body;

		public HttpResponse()
		{
		}

		public async Task<bool> SendTo(StreamWriter destination)
		{
			throw new NotImplementedException();

			return true;
		}
	}
}
