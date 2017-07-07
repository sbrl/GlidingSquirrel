using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace SBRL.GlidingSquirrel
{
	public class HttpRequest
	{
		float HttpVersion;
		HttpMethod Method;
		string RequestUrl;


		public HttpRequest()
		{
		}

		public static async Task<HttpRequest> FromStream(StreamReader reader)
		{
			HttpRequest request = new HttpRequest();

			string firstLine = await reader.ReadLineAsync();
			var firstLineData = ParseFirstLine(firstLine);

			request.HttpVersion = firstLineData.httpVersion;
			request.Method = firstLineData.requestMethod;
			request.RequestUrl = firstLineData.requestPath;
			    
			return request;
		}

		/// <summary>
		/// Parses the first line of a http request, and return the result as a C# 7 tuple.
		/// Don't panic if your editor doesn't appear to like this - it'll compile just fine :P
		/// </summary>
		public static (float httpVersion, HttpMethod requestMethod, string requestPath) ParseFirstLine(string firstLine)
		{
			List<string> lineParts = new List<string>(firstLine.Split(' '));

			return (
				float.Parse(lineParts.Last().Split('/')[1]),
				MethodFromString(lineParts.First()),
				lineParts
					.Skip(1)
					.TakeWhile((string part, int index) => index < lineParts.Count)
					.Aggregate((string one, string two) => $"{one} {two}")
			);
		}

		public static HttpMethod MethodFromString(string methodText)
		{
			return (HttpMethod)Enum.Parse(typeof(HttpMethod), methodText);
		}

	}
}
