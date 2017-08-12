using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Net;

namespace SBRL.GlidingSquirrel.Http
{
	public class HttpRequest : HttpMessage
	{
		public IPEndPoint ClientAddress;

		public HttpMethod Method;
		public string Url;


		public string Host {
			get {
				return GetHeaderValue("host", "");
			}
		}
		public string UserAgent {
			get {
				return GetHeaderValue("user-agent", "");
			}
		}
		public string Accepts {
			get {
				return GetHeaderValue("accepts", "");
			}
		}


		public HttpRequest() : base()
		{
		}

		/// <summary>
		/// Works out whether this request will accept a given mime type as a response.
		/// </summary>
		/// <param name="targetMimeType">The mime type to check.</param>
		/// <returns>Whether the specified mime type is acceptable as a response to this request..</returns>
		public bool WillAccept(string targetMimeType)
		{
			List<string> acceptedMimes = new List<string>(
				Accepts.Split(',').Select((string acceptedMimeType) => acceptedMimeType.Split(';')[0])
			);

			string[] targetMimeParts = targetMimeType.Split('/');

			if(targetMimeType.Length != 2)
				throw new ArgumentException("Error: Mime types should contain exactly 1 forward slash.");

			foreach(string acceptedMimeType in acceptedMimes)
			{
				string[] acceptedMimeParts = acceptedMimeType.Split('/');

				// Ignore invalid mime types
				if(acceptedMimeParts.Length != 2)
					continue;

				if(targetMimeType == acceptedMimeType)
					return true;
				
				if(acceptedMimeParts[0] == "*" && acceptedMimeParts[1] == "*")
					return true;

				if(targetMimeParts[0] == acceptedMimeParts[0] && acceptedMimeParts[1] == "*")
					return true;
				
			}

			return false;
		}

		//--------------------------------------------------------------------------------------

		public static async Task<HttpRequest> FromStream(StreamReader source)
		{
			HttpRequest request = new HttpRequest();

			// Parse the first line
			string firstLine = await source.ReadLineAsync();
			if(firstLine == null)
				return null;
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

			float httpVersion = float.Parse(lineParts.Last().Split('/')[1]);
			HttpMethod httpMethod = MethodFromString(lineParts.First());

			lineParts.RemoveAt(0); lineParts.RemoveAt(lineParts.Count - 1);
			string requestUrl = lineParts.Aggregate((string one, string two) => $"{one} {two}");

			return (
				httpVersion,
				httpMethod,
				requestUrl
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
