using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using SBRL.GlidingSquirrel.Http;

namespace SBRL.GlidingSquirrel.Websocket
{
	public delegate Task ClientConnectedEventHandler(object sender, ClientConnectedEventArgs ev);

	public abstract class WebsocketServer : HttpServer
	{
		public static readonly string MagicChallengeKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
		public List<WebsocketClient> Clients = new List<WebsocketClient>();


        public WebsocketServer(IPAddress inBindAddress, int inPort) : base(inBindAddress, inPort)
		{
		}


		public event ClientConnectedEventHandler OnClientConnected;
		public event ClientDisconnectedEventHandler OnClientDisconnected;

		public override async Task HandleRequest(HttpRequest request, HttpResponse response)
		{
			if(!request.Headers.ContainsKey("upgrade") || request.Headers["upgrade"] != "websocket")
				return;

            WebsocketClient newClient = await WebsocketClient.WithServerNegotiation(request, response);

			await OnClientConnected(this, new ClientConnectedEventArgs() { ConnectingClient = newClient });
			Clients.Add(newClient);


			await newClient.Listen();
		}

		protected async Task handleClientDisconnection(object sender, ClientDisconnectedEventArgs eventArgs)
		{
			WebsocketClient disconnectedClient = (WebsocketClient)sender;
			Clients.Remove(disconnectedClient);

			await OnClientDisconnected(sender, eventArgs);
		}

		protected override Task setup()
		{
			return Task.CompletedTask;
		}
	}
}
