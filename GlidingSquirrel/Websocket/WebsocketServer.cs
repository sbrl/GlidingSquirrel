﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SBRL.GlidingSquirrel.Http;

namespace SBRL.GlidingSquirrel.Websocket
{
	class ClientConnectionRequest
	{
		public HttpRequest Request;
		public HttpResponse Response;
	}

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

		/// <summary>
		/// The time that a client is allowed to remain idle beforer a ping packet is sent to it.
		/// Defaults to 1 minute.
		/// </summary>
		public TimeSpan PingInterval = TimeSpan.FromSeconds(60);

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

		public sealed override async Task<bool> HandleRequest(HttpRequest request, HttpResponse response)
		{
			if(!request.Headers.ContainsKey("upgrade") || request.Headers["upgrade"].ToLower() != "websocket")
			{
				await HandleHttpRequest(request, response);
				return true;
			}

			ThreadPool.QueueUserWorkItem(handleClientConnection, new ClientConnectionRequest() {
				Request = request,
				Response = response
			});

			return false;
		}


		protected override Task setup()
		{
			ThreadPool.QueueUserWorkItem(doMaintenance);

			return Task.CompletedTask;
		}

		protected async void handleClientConnection(object rawClientConnectionRequest)
		{
			ClientConnectionRequest connectionRequest = rawClientConnectionRequest as ClientConnectionRequest;

			if(connectionRequest == null || connectionRequest.Request == null || connectionRequest.Response == null)
			{
				Log.WriteLine(
					"[GlidingSquirrel/Websockets] Client connection handler called with " +
					"null connection request (or properties thereof), ignoring.");
				return;
			}

			WebsocketClient client = null;
			try
			{
				client = await WebsocketClient.WithServerNegotiation(connectionRequest.Request, connectionRequest.Response);

				Log.WriteLine("[GlidingSquirrel/Websockets] New client connected from {0}.", client.RemoteEndpoint);

				await OnClientConnected(this, new ClientConnectedEventArgs() { ConnectingClient = client });
				Clients.Add(client);

				await client.Listen();

			}
			catch(IOException)
			{
				Log.WriteLine("[GlidingSquirrel/WebsocketClient] Caught IOException - a client probably disconnected uncleanly");
			}
			catch(SocketException)
			{
				Log.WriteLine("[GlidingSquirrel/WebsocketClient] Caught SocketException - a client probably disconnected uncleanly");
			}
			catch(Exception error)
			{
				Log.WriteLine("[GlidingSquirrel/WebsocketClient] Error: {0}", error);
			}

			Log.WriteLine("[GlidingSquirrel/Websockets] Client from {0} disconnected with code {1}.", client.RemoteEndpoint, client.ExitCode);
		}

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
			while(true)
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
					Log.WriteLine("[WebsocketServer/Maintenance] {0}", error);
				}

				await Task.Delay((int)PingInterval.TotalSeconds / 4);
			}

			Log.WriteLine("[WebsocketServer/Maintenance] Ending maintenance loop.");
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

	}
}
