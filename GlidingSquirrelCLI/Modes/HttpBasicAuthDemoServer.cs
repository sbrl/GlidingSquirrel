using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using SBRL.GlidingSquirrel.Http;
using SBRL.Utilities;

namespace SBRL.GlidingSquirrel.CLI.Modes
{
	public class HttpBasicAuthDemoServer : HttpServer
	{
		public readonly string AuthRealm = "Test realm";

		private string username = "user";
		private string password = "cheese";

		public HttpBasicAuthDemoServer(IPAddress inBindAddress, int inPort) : base(inBindAddress, inPort)
		{
		}
		public HttpBasicAuthDemoServer(int inPort) : this(IPAddress.IPv6Any, inPort)
		{
		}

		protected override Task setup()
		{
			return Task.CompletedTask;
		}

		public override async Task<HttpConnectionAction> HandleRequest(HttpRequest request, HttpResponse response)
		{
			await Task.Delay(0);

			if(request.Method != HttpMethod.GET) {
				response.ResponseCode = HttpResponseCode.MethodNotAllowed;
				return HttpConnectionAction.Continue;
			}

			if(request.BasicAuthCredentials == null) {
				response.RequireHttpBasicAuthentication(AuthRealm);
				response.ContentType = "text/plain";
				await response.SetBody("Please authorise yourself with HTTP Basic Authentication.\n");
				return HttpConnectionAction.Continue;
			}

			if(!(request.BasicAuthCredentials.Username == username &&
			     request.BasicAuthCredentials.Password == password)) {
				response.RequireHttpBasicAuthentication(AuthRealm);
				await response.SetBody("Invalid username or password! Please try again.\n");
				return HttpConnectionAction.Continue;
			}

			response.ContentType = "text/html";
			response.ContentLength = EmbeddedFiles.ReadAllBytes("SBRL.GlidingSquirrel.CLI.Modes.Resources.HttpBasicAuthDemo.html").Length;

			response.Body = EmbeddedFiles.GetReader("SBRL.GlidingSquirrel.CLI.Modes.Resources.HttpBasicAuthDemo.html");

			return HttpConnectionAction.Continue;
		}

	}
}
