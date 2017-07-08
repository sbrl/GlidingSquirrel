using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace SBRL.GlidingSquirrel
{
	public class HttpRequest
	{
		public float HttpVersion;
		public HttpMethod Method;
		public string Url;

		public Dictionary<string, string> Headers;
		public string ContentType {
			get {
				return Headers["content-type"] ?? "application/octet-stream";
			}
		}
		public int ContentLength {
			get {
				int result = 0;
				int.TryParse(Headers["content-length"], out result);
				return result;
			}
		}

		public StreamReader Body;


		public HttpRequest()
		{
		}

		public static async Task<HttpRequest> FromStream(StreamReader source)
		{
			HttpRequest request = new HttpRequest();

			// Parse the first line
			string firstLine = await source.ReadLineAsync();
			var firstLineData = ParseFirstLine(firstLine);

			request.HttpVersion = firstLineData.httpVersion;
			request.Method = firstLineData.requestMethod;
			request.Url = firstLineData.requestPath;

			// Extract the headers
			List<string> rawHeaders = new List<string>();
			string nextLine;
			while((nextLine = source.ReadLine()).Length > 0)
				rawHeaders.Add(nextLine);

			request.Headers = ParseHeaders(rawHeaders);

			// Store the source stream as the request body now that we've extracts the headers
			request.Body = source;
			    
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

		public static Dictionary<string, string> ParseHeaders(List<string> rawHeaders)
		{
			Dictionary<string, string> result = new Dictionary<string, string>();

			foreach(string header in rawHeaders)
			{
				string[] parts = header.Split(':');
				KeyValuePair<string, string> nextHeader = new KeyValuePair<string, string>(
					parts[0].Trim().ToLower(),
					parts[1].Trim()
				);
				if(result.ContainsKey(nextHeader.Key))
					result[nextHeader.Key] = $"{result[nextHeader.Key]},{nextHeader.Value}";
				else
					result[nextHeader.Key] = nextHeader.Value;
			}

			return result;
		}
	}
}
