using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using SBRL.GlidingSquirrel.Http;
using SBRL.GlidingSquirrel.Websocket;
using SBRL.Utilities;

namespace SBRL.GlidingSquirrel.CLI.Modes
{
	public class HttpBasicAuthDemoServer : WebsocketServer
	{
		public readonly string AuthRealm = "Test realm";

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

		public override async Task<HttpConnectionAction> HandleHttpRequest(HttpRequest request, HttpResponse response)
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

			if(request.BasicAuthCredentials.Password != password) {
				response.RequireHttpBasicAuthentication(AuthRealm);
				await response.SetBody("Invalid username or password! Please try again.\n");
				return HttpConnectionAction.Continue;
			}

			response.ContentType = "text/html";
			response.ContentLength = EmbeddedFiles.ReadAllBytes("SBRL.GlidingSquirrel.CLI.Modes.Resources.HttpBasicAuthDemo.html").Length;

			response.Body = EmbeddedFiles.GetReader("SBRL.GlidingSquirrel.CLI.Modes.Resources.HttpBasicAuthDemo.html");

			return HttpConnectionAction.Continue;
		}

		public override bool ShouldAcceptConnection(HttpRequest connectionRequest, HttpResponse connectionResponse)
		{
			if(connectionRequest.BasicAuthCredentials.Password != password)
			{
				connectionResponse.RequireHttpBasicAuthentication("demo realm (anything/cheese)");
				connectionResponse.ContentType = "text/plain";
				connectionResponse.SetBody("Please authenticate via HTTP Basic authentication. The username is anything you like, and the password is cheese.").Wait();
				return false;
			}

			return true;
		}

		public override async Task HandleClientConnected(object sender, ClientConnectedEventArgs eventArgs)
		{
			string name = eventArgs.ConnectingClient.HandshakeRequest.BasicAuthCredentials.Username;
			await eventArgs.ConnectingClient.Send($"Welcome to the demo server, {name}! You've successfully authenticated.");
			await eventArgs.ConnectingClient.Send("This is a demo server that will echo back any textual messages you send to it.");

			eventArgs.ConnectingClient.OnTextMessage += async (object messageSender, TextMessageEventArgs messageEventArgs) => {
				await eventArgs.ConnectingClient.Send(messageEventArgs.Payload);
			};
		}

		public override Task HandleClientDisconnected(object sender, ClientDisconnectedEventArgs eventArgs)
		{
			return Task.CompletedTask;
		}
	}
}
