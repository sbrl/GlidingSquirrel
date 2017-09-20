using System;
using System.Net;
using System.Threading.Tasks;

using SBRL.GlidingSquirrel.Http;
using SBRL.GlidingSquirrel.Websocket;
using SBRL.Utilities;

namespace SBRL.GlidingSquirrel.CLI.Modes
{
	public class ChatWebsocketServer : WebsocketServer
	{
		public ChatWebsocketServer(IPAddress inBindAddress, int inPort) : base(inBindAddress, inPort)
		{
		}

		public override async Task HandleClientConnected(object sender, ClientConnectedEventArgs eventArgs)
		{
			WebsocketClient client = eventArgs.ConnectingClient;
			// Send a welcome message
			await client.Send(
				"Welcome to this sample websockets server!<br />\n" +
				"This server will echo any frames you send it to all other connected clients."
			);

			// Echo text and binary messages we geet sent
			client.OnTextMessage += async (object textSender, TextMessageEventArgs textEventArgs) => {
				Console.WriteLine("Reflecting message '{0}'.", textEventArgs.Payload);
				await Reflect(textSender as WebsocketClient, textEventArgs.Payload);
			};
			client.OnBinaryMessage += async (object binarySender, BinaryMessageEventArgs binaryEventArgs) => {
				Console.WriteLine("Reflecting binary message.");
				await Reflect(binarySender as WebsocketClient, binaryEventArgs.Payload);
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
				await EmbeddedFiles.ReadAllTextAsync("SBRL.GlidingSquirrel.CLI.Modes.Resources.ChatWebsocketClient.html")
			);
		}

		protected override Task setup()
		{
			return Task.CompletedTask;
		}
	}
}
