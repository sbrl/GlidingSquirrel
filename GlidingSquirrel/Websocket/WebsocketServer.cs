using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SBRL.GlidingSquirrel.Http;

namespace SBRL.GlidingSquirrel.Websocket
{
	/// <summary>
	/// Represents a connecting websocket client.
	/// </summary>
	class ClientConnectionRequest
	{
		/// <summary>
		/// The raw client connection.
		/// </summary>
		public TcpClient RawClient;
		/// <summary>
		/// The HTTP request they sent to us.
		/// </summary>
		public HttpRequest Request;
		/// <summary>
		/// The response we will send back to them to complete the websocket handshake.
		/// </summary>
		public HttpResponse Response;
	}

	/// <summary>
	/// The delegate type used in the ClientConnected event in the WebsocketServer.
	/// This event is fired when a new client connects to the server.
	/// </summary>
	public delegate Task ClientConnectedEventHandler(object sender, ClientConnectedEventArgs ev);

	/// <summary>
	/// The main Websockets Server class. Inherit from this to create your own websockets-capable HTTP server!
	/// Note that if you don't want or need the Websockets support, you should inherit from 
	/// <see cref="HttpServer" /> instead.
	/// </summary>
	public abstract class WebsocketServer : HttpServer
	{
		/// <summary>
		/// The magic challenge key. Used in the initial handshake.
		/// </summary>
		public static readonly string MagicChallengeKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

		/// <summary>
		/// The reason string that we should send to clients when shutting down.
		/// </summary>
		private string stopReason = "The server is shutting down.";

		/// <summary>
		/// A list of currently connected clients.
		/// </summary>
		public List<WebsocketClient> Clients = new List<WebsocketClient>();

		/// <summary>
		/// The time that a client is allowed to remain idle beforer a ping packet is sent to it.
		/// Defaults to 1 minute.
		/// </summary>
		public TimeSpan PingInterval = TimeSpan.FromSeconds(60);

		/// <summary>
		/// Initialises a new WebsocketServer.
		/// </summary>
		/// <param name="inBindAddress">The IP address to bind to.</param>
		/// <param name="inPort">The port to listen on.</param>
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


		#region Request Handling

		/// <summary>
		/// Handles incoming HTTP requests. WebsocketServer implements this as it needs to eat certain 
		/// requests that are actually Websocket handshakes.
		/// You'll probably want to take a look at the requests too - that's what 
		/// <see cref="HandleHttpRequest" /> is for.
		/// </summary>
		/// <param name="request">The HTTP request to handle.</param>
		/// <param name="response">The HTTP response to send back to the client.</param>
		/// <returns>Tells HttpServer to leave the request alone.</returns>
		public sealed override async Task<HttpConnectionAction> HandleRequest(HttpRequest request, HttpResponse response)
		{
			if(!request.Headers.ContainsKey("upgrade") || request.Headers["upgrade"].ToLower() != "websocket")
			{
				return await HandleHttpRequest(request, response);
			}

			if(ShouldAcceptConnection(request, response))
			{
				// We're ok to allow the connection to continue!
				// Put it through on a separate thread from the ThreadPool.
				ThreadPool.QueueUserWorkItem(handleClientConnection, new ClientConnectionRequest() {
					RawClient = request.ClientConnection,
					Request = request,
					Response = response
				});

				return HttpConnectionAction.LeaveAlone;
			}

			// Nope, this isn't a good connection to accept, apparently.
			return HttpConnectionAction.SendAndKillConnection;
		}

		/// <summary>
		/// Performs websockets-specific setup logic.
		/// </summary>
		protected override Task setup()
		{
			ThreadPool.QueueUserWorkItem(doMaintenance);
			cancellationToken.Register(async () => await CloseAll(stopReason));
			return Task.CompletedTask;
		}

		/// <summary>
		/// Stops the Websocket (and HTTP) server, sending the specified reason text to all currently
		/// connected Websocket clients.
		/// </summary>
		/// <remarks>
		/// Note that the regular .Stop() method does infact disconnect all Websocket clients cleanly - you
		/// don't need to call this method explicitly in order to get a clean exit.
		/// </remarks>
		/// <param name="reason">The reason text to send to the clients when disconnecting them.</param>
		public void Stop(string reason)
		{
			stopReason = reason;
			base.Stop();
		}

		/// <summary>
		/// Handles a websocket client connection. Designed to be called in a separate thread.
		/// </summary>
		/// <param name="rawClientConnectionRequest">Raw client connection request.</param>
		protected async void handleClientConnection(object rawClientConnectionRequest)
		{
			ClientConnectionRequest connectionRequest = rawClientConnectionRequest as ClientConnectionRequest;

			if(connectionRequest == null || connectionRequest.RawClient == null || connectionRequest.Request == null)
			{
				Log.WriteLine(
					LogLevel.Warning,
					"[GlidingSquirrel/Websockets] Client connection handler called with " +
					"null connection request (or properties thereof), ignoring.");
				return;
			}

			WebsocketClient client = null;
			try
			{
				client = await WebsocketClient.WithServerNegotiation(
					connectionRequest.RawClient,
					connectionRequest.Request,
					connectionRequest.Response
				);

				Log.WriteLine(LogLevel.Info, "[GlidingSquirrel/Websockets] New client connected from {0}.", client.RemoteEndpoint);

				await OnClientConnected(this, new ClientConnectedEventArgs() { ConnectingClient = client });
				Clients.Add(client);

				await client.Listen();

			}
			catch(IOException error)
			{
				Log.WriteLine(LogLevel.Error, "[GlidingSquirrel/WebsocketClient] Caught IOException - a client probably disconnected uncleanly");
				Log.WriteLine(LogLevel.Error, "[GlidingSquirrel/WebsocketClient] IOException Message: {0}", error.Message);
			}
			catch(SocketException error)
			{
				Log.WriteLine(LogLevel.Error, "[GlidingSquirrel/WebsocketClient] Caught SocketException - a client probably disconnected uncleanly");
				Log.WriteLine(LogLevel.Error, "[GlidingSquirrel/WebsocketClient] SocketException Message: {0}", error.Message);
			}
			catch(Exception error)
			{
				Log.WriteLine(LogLevel.Error, "[GlidingSquirrel/WebsocketClient] Error: {0}", error);
			}

			if(client != null)
			{
				// Make sure that the OnDisconnection event gets fired on the client
				if(!client.IsClosed)
					await client.Destroy();
				
				Log.WriteLine(
					LogLevel.Info,
					"[GlidingSquirrel/Websockets] Client from {0} disconnected with code {1}.",
					client.RemoteEndpoint,
					client.ExitCode
				);
			}
			else
			{
				Log.WriteLine(LogLevel.Info, "[GlidingSquirrel/Websockets] Client disconnected with code {1}.", WebsocketCloseReason.CloseReasonLost);
			}
		}

		/// <summary>
		/// Fired when a client disconnects from the server. This is attached to the ClientDisconnected event
		/// on the WebsocketClient by the websocket server to track when clients disconnect.
		/// </summary>
		/// <param name="sender">The sender of this ClientDisconnected event.</param>
		/// <param name="eventArgs">The event arguments attached to this ClientDisconnected event.</param>
		protected async Task handleClientDisconnection(object sender, ClientDisconnectedEventArgs eventArgs)
		{
			WebsocketClient disconnectedClient = (WebsocketClient)sender;
			Clients.Remove(disconnectedClient);

			await OnClientDisconnected(sender, eventArgs);
		}

		/// <summary>
		/// Performs maintenance at regular intervals.
		/// </summary>
		protected async void doMaintenance(object state)
		{
			while(!cancellationToken.IsCancellationRequested)
			{
				try
				{
					List<Task> pingTasks = new List<Task>();
					foreach(WebsocketClient client in Clients)
					{
						if(DateTime.Now - client.LastCommunication >= PingInterval)
							pingTasks.Add(client.Ping());

					}

					Task.WaitAll(pingTasks.ToArray());
				}
				catch(Exception error)
				{
					Log.WriteLine(LogLevel.Error, "[WebsocketServer/Maintenance] {0}", error);
				}

				await Task.Delay((int)PingInterval.TotalSeconds / 4, cancellationToken);
			}

			Log.WriteLine(LogLevel.System, "[WebsocketServer/Maintenance] Ending maintenance loop.");
		}


		#endregion

		#region Interactive Interface Methods


		/// <summary>
		/// Broadcasts the specified message to all connected clients who aren't
		/// currently in the process of closing their connection.
		/// </summary>
		/// <param name="message">The message to broadcast.</param>
		public async Task Broadcast(string message)
		{
			List<Task> senders = new List<Task>();
			foreach(WebsocketClient client in Clients)
			{
				if(client.IsClosing)
					continue;
				senders.Add(client.Send(message));
			}
			await Task.WhenAll(senders.ToArray());
		}
		/// <summary>
		/// Broadcasts the specified binary message to all connected clients who aren't
		/// currently in the process of closing their connection.
		/// </summary>
		/// <param name="message">The message to broadcast.</param>
		public async Task Broadcast(byte[] message)
		{
			List<Task> senders = new List<Task>();
			foreach(WebsocketClient client in Clients)
			{
				if(client.IsClosing)
					continue;
				senders.Add(client.Send(message));
			}
			await Task.WhenAll(senders.ToArray());
		}

		/// <summary>
		/// Reflects the specified message to all connected clients who both aren't
		/// the sender and aren't currently in the process of closing their connection.
		/// </summary>
		/// <param name="sender">The WebsocketClient sending the message.</param>
		/// <param name="message">The message to broadcast.</param>
		public async Task Reflect(WebsocketClient sender, string message)
		{
			List<Task> senders = new List<Task>();
			foreach(WebsocketClient client in Clients)
			{
				if(client == sender || client.IsClosing)
					continue;
				senders.Add(client.Send(message));
			}
			await Task.WhenAll(senders.ToArray());
		}
		/// <summary>
		/// Reflects the specified binary message to all connected clients who both aren't
		/// the sender and aren't currently in the process of closing their connection.
		/// </summary>
		/// <param name="sender">The WebsocketClient sending the message.</param>
		/// <param name="message">The message to broadcast.</param>
		public async Task Reflect(WebsocketClient sender, byte[] message)
		{
			List<Task> senders = new List<Task>();
			foreach(WebsocketClient client in Clients)
			{
				if(client == sender || client.IsClosing)
					continue;
				senders.Add(client.Send(message));
			}
			await Task.WhenAll(senders.ToArray());
		}

		/// <summary>
		/// Closes all the currently open connections with the specified reason.
		/// </summary>
		/// <param name="reason">The reason message to send when closing connections.</param>
		public async Task CloseAll(string reason)
		{
			await CloseAll(WebsocketCloseReason.Shutdown, reason);
		}
		/// <summary>
		/// Closes all the currently open connections with the specified reason code and message.
		/// </summary>
		/// <remarks>
		/// Unless you've got a very unusual use case, CloseAll(string) is probably what you want.
		/// </remarks>
		/// <param name="reason">The reason code to send when closing connections.</param>
		/// <param name="closingMessage">The closing message text to send.</param>
		public async Task CloseAll(WebsocketCloseReason reason, string closingMessage)
		{
			List<Task> senders = new List<Task>();
			foreach (WebsocketClient client in Clients)
			{
				if (client.IsClosing)
					continue;
				senders.Add(client.Close(reason, closingMessage));
			}
			await Task.WhenAll(senders.ToArray());
		}

		#endregion


		#region Abstract Interface Methods


		/// <summary>
		/// Gets called automatically to handle regular HTTP requests.
		/// </summary>
		/// <param name="request">The HTTP request.</param>
		/// <param name="response">The HTTP response to send back.</param>
		/// <returns>What to do with the connection.</returns>
		public abstract Task<HttpConnectionAction> HandleHttpRequest(HttpRequest request, HttpResponse response);
		/// <summary>
		/// Gets called automatically to determine if a connection should be accepted or not.
		/// </summary>
		/// <param name="connectionRequest">The connection request received.</param>
		/// <param name="connectionResponse">
		/// The response to the connection that will be sent. If true is returned, then the contents 
		/// of this may be largely overwritten. If false is returned, then this response will be sent 
		/// verbatim to the client.</param>
		/// <returns>Whether the connection should be accepted or not.</returns>
		public abstract bool ShouldAcceptConnection(HttpRequest connectionRequest, HttpResponse connectionResponse);
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


		#endregion

	}
}
