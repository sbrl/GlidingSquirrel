using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SBRL.GlidingSquirrel
{
	public class FileHttpServer : HttpServer
	{
		public readonly string WebRoot = ".";

		public FileHttpServer(IPAddress inBindAddress, int inPort) : base(inBindAddress, inPort)
		{
		}
		public FileHttpServer(int inPort) : this(IPAddress.IPv6Any, inPort)
		{
		}

		public override async Task HandleRequest(HttpRequest request, HttpResponse response)
		{
			if(request.Url.Contains(".."))
			{
				response.ResponseCode = HttpResponseCode.BadRequest;
				await response.SetBody("Error the requested path contains dangerous characters.");
				return;
			}

			string filePath = getFilePathFromRequestUrl(request.Url);
			FileInfo requestFileStat = new FileInfo(filePath);
			if(!requestFileStat.Exists)
			{
				response.ResponseCode = HttpResponseCode.NotFound;
			}

			response.Headers.Add("content-type", mimeLookup.Lookup(filePath));
			response.Headers.Add("content-length", requestFileStat.Length.ToString());

			response.Body = new StreamReader(filePath);
		}

		protected string getFilePathFromRequestUrl(string requestUrl)
		{
			return $"{WebRoot}{requestUrl}";
		}
	}
}
