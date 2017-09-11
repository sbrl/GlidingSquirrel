using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using SBRL.GlidingSquirrel.Http;

namespace SBRL.GlidingSquirrel.CLI.Modes
{
	public class FileHttpServer : HttpServer
	{
		public readonly string WebRoot = ".";

		public FileHttpServer(IPAddress inBindAddress, int inPort, string inWebRoot) : base(inBindAddress, inPort)
		{
			WebRoot = inWebRoot;
		}
		public FileHttpServer(int inPort, string inWebRoot) : this(IPAddress.IPv6Any, inPort, inWebRoot)
		{
		}

		protected override Task setup()
		{
			Log.WriteLine($"Webroot set to {WebRoot}");

			return Task.CompletedTask;
		}

		public override async Task<HttpConnectionAction> HandleRequest(HttpRequest request, HttpResponse response)
		{
			if(request.Url.Contains(".."))
			{
				response.ResponseCode = HttpResponseCode.BadRequest;
				await response.SetBody("Error the requested path contains dangerous characters.");
				return HttpConnectionAction.SendAndKillConnection;
			}

			string filePath = getFilePathFromRequestUrl(request.Url);
			if(!File.Exists(filePath))
			{
				response.ResponseCode = HttpResponseCode.NotFound;
				await response.SetBody($"Error: The file path '{request.Url}' could not be found.\n");
				return HttpConnectionAction.Continue;
			}

			FileInfo requestFileStat = null;
			try {
				requestFileStat = new FileInfo(filePath);
			}
			catch(UnauthorizedAccessException error) {
				response.ResponseCode = HttpResponseCode.Forbidden;
				await response.SetBody(
					"Unfortunately, the server was unable to access the file requested.\n" + 
					"Details:\n\n" + 
					error.ToString() + 
					"\n"
				);
				return HttpConnectionAction.Continue;
			}

			response.Headers.Add("content-type", LookupMimeType(filePath));
			response.Headers.Add("content-length", requestFileStat.Length.ToString());

			if(request.Method == HttpMethod.GET)
			{
				response.Body = new StreamReader(filePath);
			}

			return HttpConnectionAction.Continue;
		}

		protected string getFilePathFromRequestUrl(string requestUrl)
		{
			return $"{WebRoot}{requestUrl}";
		}
	}
}
