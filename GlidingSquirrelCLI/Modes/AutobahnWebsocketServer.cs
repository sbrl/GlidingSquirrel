using System;
using System.Net;
using System.Threading.Tasks;

using SBRL.GlidingSquirrel.Http;
using SBRL.GlidingSquirrel.Websocket;
using SBRL.Utilities;

namespace SBRL.GlidingSquirrel.CLI.Modes
{
	public class AutobahnWebsocketServer : WebsocketServer
	{
		public AutobahnWebsocketServer(IPAddress inBindAddress, int inPort) : base(inBindAddress, inPort)
		{
		}

		public override Task HandleClientConnected(object sender, ClientConnectedEventArgs eventArgs)
		{
			WebsocketClient client = eventArgs.ConnectingClient;

			// Echo text and binary messages we get sent
			client.OnTextMessage += async (object textSender, TextMessageEventArgs textEventArgs) => {
				Log.WriteLine(LogLevel.Debug, "[GlidingSquirrel/Autobahn] Replying to text frame with '{0}'", textEventArgs.Payload);
				await client.Send(textEventArgs.Payload);
			};
			client.OnBinaryMessage += async (object binarySender, BinaryMessageEventArgs binaryEventArgs) => {
				string binaryRepresentation = BitConverter.ToString(binaryEventArgs.Payload).Replace("-", " ");
				if(binaryRepresentation.Length > 200)
					binaryRepresentation = binaryRepresentation.Substring(0, 200) + "...";
				Log.WriteLine(
					LogLevel.Debug,
					"[GlidingSquirrel/Autobahn] Replying to binary frame with '{0}'",
					binaryRepresentation
				);
				await client.Send(binaryEventArgs.Payload);
			};

			return Task.CompletedTask;
		}

		public override Task HandleClientDisconnected(object sender, ClientDisconnectedEventArgs eventArgs)
		{
			return Task.CompletedTask;
		}

		public override async Task<HttpConnectionAction> HandleHttpRequest(HttpRequest request, HttpResponse response)
		{
			if(request.Url != "/")
			{
				response.ResponseCode = HttpResponseCode.NotFound;
				response.ContentType = "text/plain";
				await response.SetBody("Couldn't find anything at '{request.Url}'.");
				return HttpConnectionAction.Continue;
			}

			response.ResponseCode = HttpResponseCode.Ok;
			response.ContentType = "text/html";
			await response.SetBody(
				await EmbeddedFiles.ReadAllTextAsync("SBRL.GlidingSquirrel.CLI.Modes.Resources.EchoWebsocketClient.html")
			);
			return HttpConnectionAction.Continue;
		}

		public override bool ShouldAcceptConnection(HttpRequest connectionRequest, HttpResponse connectionResponse)
		{
			return true;
		}

		protected override Task setup()
		{
			return Task.CompletedTask;
		}
	}
}
