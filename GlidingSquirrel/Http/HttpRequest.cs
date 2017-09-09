using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace SBRL.GlidingSquirrel.Http
{
	/// <summary>
	/// Represents an http requeest.
	/// </summary>
	public class HttpRequest : HttpMessage
	{
		/// <summary>
		/// The underlying conenction to the remote client.
		/// </summary>
        public TcpClient ClientConnection;
		/// <summary>
		/// The address that the client is connecting from.
		/// </summary>
		public IPEndPoint ClientAddress;
		/// <summary>
		/// The method used in this http request.
		/// </summary>
		public HttpMethod Method;
		/// <summary>
		/// The url requested by this request. Not processed according to the host header at all.
		/// </summary>
		public string Url;

		/// <summary>
		/// Quick access to the host http header.
		/// </summary>
		public string Host {
			get {
				return GetHeaderValue("host", "");
			}
		}
		/// <summary>
		/// Quick access to the user-agent http header.
		/// </summary>
		public string UserAgent {
			get {
				return GetHeaderValue("user-agent", "");
			}
		}
		/// <summary>
		/// Quick access to the accepts http header.
		/// </summary>
		public string Accepts {
			get {
				return GetHeaderValue("accepts", "");
			}
		}

		/// <summary>
		/// Creates a new http request.
		/// </summary>
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

		/// <summary>
		/// Builds a new HttpRequest instance from the supplied data stream.
		/// May throw an exception if unsuccessful.
		/// </summary>
		/// <param name="source">The source stream to build the HttpRequest instance from.</param>
		/// <returns>The completed http request.</returns>
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

		/// <summary>
		/// Converts a http method stored in a string to the HttpMethod enum.
		/// Multiple header defintions are concatenated in the order that they appear, delimited by a comma.
		/// </summary>
		/// <param name="methodText">The http method as a string to convert.</param>
		/// <returns>The http method as an enum.</returns>
		public static HttpMethod MethodFromString(string methodText)
		{
			return (HttpMethod)Enum.Parse(typeof(HttpMethod), methodText);
		}

		/// <summary>
		/// Parses a raw lsit of headers into a dictionary of headers keys and values.
		/// </summary>
		/// <param name="rawHeaders">The raw headers to parse.</param>
		/// <returns>The parsed headers.</returns>
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
