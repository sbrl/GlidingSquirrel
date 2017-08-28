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
		/// <summary>
		/// The magic challenge key. Used in the initial handshake.
		/// </summary>
		public static readonly string MagicChallengeKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
		/// <summary>
		/// A list of currently connected clients.
		/// </summary>
		public List<WebsocketClient> Clients = new List<WebsocketClient>();

        public WebsocketServer(IPAddress inBindAddress, int inPort) : base(inBindAddress, inPort)
		{
			OnClientConnected += HandleClientConnected;
			OnClientDisconnected += HandleClientDisconnected;
		}

		/// <summary>
		/// Occurs when a new (websocket) client connects.
		/// </summary>
		public event ClientConnectedEventHandler OnClientConnected;
		/// <summary>
		/// Occurs when a (websocket) client disconnects.
		/// </summary>
		public event ClientDisconnectedEventHandler OnClientDisconnected;

		public sealed override async Task HandleRequest(HttpRequest request, HttpResponse response)
		{
			if(!request.Headers.ContainsKey("upgrade") || request.Headers["upgrade"] != "websocket")
			{
				await HandleHttpRequest(request, response);
				return;
			}

            WebsocketClient newClient = await WebsocketClient.WithServerNegotiation(request, response);

			await OnClientConnected(this, new ClientConnectedEventArgs() { ConnectingClient = newClient });
			Clients.Add(newClient);


			await newClient.Listen();
		}

		/// <summary>
		/// Gets called automatically to handle regular HTTP requests.
		/// </summary>
		/// <param name="request">The HTTP request.</param>
		/// <param name="response">The HTTP response to send back.</param>
		public abstract Task HandleHttpRequest(HttpRequest request, HttpResponse response);
		/// <summary>
		/// Gets called automatically when a client has connected.
		/// </summary>
		/// <param name="sender">The WebsocketClient object representing the client that has connected.</param>
		/// <param name="eventArgs">The client connected event arguments.</param>
		public abstract Task HandleClientConnected(object sender, ClientConnectedEventArgs eventArgs);
		/// <summary>
		/// Gets called automatically when a client has disconnected.
		/// </summary>
		/// <param name="sender">The WebsocketClient object representing the client that has disconnected.</param>
		/// <param name="eventArgs">The client disconnected event arguments.</param>
		public abstract Task HandleClientDisconnected(object sender, ClientDisconnectedEventArgs eventArgs);


		protected async Task handleClientDisconnection(object sender, ClientDisconnectedEventArgs eventArgs)
		{
			WebsocketClient disconnectedClient = (WebsocketClient)sender;
			Clients.Remove(disconnectedClient);

			await OnClientDisconnected(sender, eventArgs);
		}
	}
}
