using System;
using System.Net;
using System.Threading.Tasks;

using SBRL.GlidingSquirrel.Http;
using SBRL.GlidingSquirrel.Websocket;
using SBRL.Utilities;

namespace SBRL.GlidingSquirrel.Modes
{
	public class EchoWebsocketServer : WebsocketServer
	{
		public EchoWebsocketServer(IPAddress inBindAddress, int inPort) : base(inBindAddress, inPort)
		{
		}

		public override async Task HandleClientConnected(object sender, ClientConnectedEventArgs eventArgs)
		{
			WebsocketClient client = eventArgs.ConnectingClient;
			// Send a welcome message
			await client.Send(
				"Welcome to this sample websockets server!<br />\n" +
				"This server will echo any frames you send it."
			);

			// Echo text and binary messages we geet sent
			client.OnTextMessage += async (object textSender, TextMessageEventArgs textEventArgs) => {
				await client.Send(textEventArgs.Payload);
			};
			client.OnBinaryMessage += async (object binarySender, BinaryMessageEventArgs binaryEventArgs) => {
				await client.Send(binaryEventArgs.Payload);
			};
		}

		public override Task HandleClientDisconnected(object sender, ClientDisconnectedEventArgs eventArgs)
		{
			return Task.CompletedTask;
		}

		public override async Task HandleHttpRequest(HttpRequest request, HttpResponse response)
		{
			if(request.Url != "/")
			{
				response.ResponseCode = HttpResponseCode.NotFound;
				response.ContentType = "text/plain";
				await response.SetBody("Couldn't find anything at '{request.Url}'.");
				return;
			}

			response.ResponseCode = HttpResponseCode.Ok;
			response.ContentType = "text/html";
			await response.SetBody(
				await EmbeddedFiles.ReadAllTextAsync("SBRL.GlidingSquirrel.Modes.Resources.EchoWebsocketClient.html")
			);
		}

		protected override Task setup()
		{
			return Task.CompletedTask;
		}
	}
}
