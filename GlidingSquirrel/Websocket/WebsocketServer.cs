using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SBRL.GlidingSquirrel.Http;

namespace SBRL.GlidingSquirrel.Websocket
{
	public delegate void ClientConnectedEventHandler(object sender, ClientConnectedEventArgs ev);

	public abstract class WebsocketServer : HttpServer
	{
		public List<WebsocketClient> Clients = new List<WebsocketClient>();


		public WebsocketServer()
		{
		}


		public event ClientConnectedEventHandler OnClientConnected;


		public override async Task HandleRequest(HttpRequest request, HttpResponse response)
		{
			if(!request.Headers.ContainsKey("upgrade") || request.Headers["upgrade"] != "websocket")
				return;


		}

		protected override Task setup()
		{
			return Task.CompletedTask;
		}
	}
}
