using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SBRL.GlidingSquirrel.Http;
using System.Linq;
using System.Net;

namespace SBRL.GlidingSquirrel.Websocket
{
	/// <summary>
	/// Something went wrong during the websocket handshake.
	/// </summary>
	public class WebsocketClientHandshakeException : Exception {
		public WebsocketClientHandshakeException(string message, Exception innerException = null)
			: base(message, innerException)
		{
		}
	}

	public delegate Task NextFrameEventHandler(object sender, NextFrameEventArgs eventArgs);

    public delegate Task TextMessageEventHandler(object sender, TextMessageEventArgs eventArgs);
	public delegate Task BinaryMessageEventHandler(object sender, BinaryMessageEventArgs eventArgs);
	public delegate Task ClientDisconnectedEventHandler(object sender, ClientDisconnectedEventArgs eventArgs);

	public class WebsocketClient
	{
		private TcpClient connection;
		private bool closingConnection = false;

		protected Random rand = new Random();

		public DateTime LastCommunication = DateTime.Now;

		/// <summary>
		/// The code with which this websocket client connection exited with.
		/// </summary>
		public int ExitCode = -1;

		/// <summary>
		/// The maximum size of any websocket frames sent to this client.
		/// Defaults to 2MiB.
		/// </summary>
		public int MaximumTransmissionSize { get; set; } = 2 * 1024 * 1024;

		public IPEndPoint RemoteEndpoint {
			get {
				return (IPEndPoint)connection.Client.RemoteEndPoint;
			}
		}

		public event NextFrameEventHandler OnFrameRecieved;

		public event TextMessageEventHandler OnTextMessage;
		public event BinaryMessageEventHandler OnBinaryMessage;

		public event ClientDisconnectedEventHandler OnDisconnection;

		/// <summary>
		/// Creates a new blank Websocket connection.
		/// Does not perform any negotiation!
		/// See WebsocketClient::AfterServerNegotiation() and WebsocketClient(string remoteAddress).
		/// </summary>
		private WebsocketClient()
		{
		}
		/// <summary>
		/// Creates a new websocket client connection.
		/// </summary>
		/// <param name="remoteAddress">Remote address.</param>
		public WebsocketClient(string remoteAddress)
		{
			OnFrameRecieved += handleNextFrame;
		}


		#region Frame Handling

		public async Task Listen()
		{
			while(true)
			{
				WebsocketFrame nextFrame = await WebsocketFrame.Decode(connection.GetStream());

				if(nextFrame == null)
					break;

				await OnFrameRecieved(this, new NextFrameEventArgs() { Frame = nextFrame });
				LastCommunication = DateTime.Now;

				if(!connection.Connected || closingConnection)
					break;
			}

			await OnDisconnection(this, new ClientDisconnectedEventArgs());
		}

		/// <summary>
		/// Sends a websocket frame to this client.
		/// </summary>
		/// <param name="frame">The frame to send..</param>
		protected async Task sendFrame(WebsocketFrame frame)
		{
			await frame.SendTo(connection.GetStream());
		}

		protected async Task handleNextFrame(object sender, NextFrameEventArgs nextFrameEventArgs)
		{
			WebsocketFrame nextFrame = nextFrameEventArgs.Frame;
			WebsocketFrame nextSeqFrame;

			// todo close the connection properly here

			switch(nextFrame.Type)
			{
				case WebsocketFrameType.ContinuationFrame:
					throw new Exception("Error: Can't process a continuation frame when there's" +
				"nothing to continue!");

				case WebsocketFrameType.Ping:
					if(nextFrame.RawPayload.Length > 125)
					{
						// The payload is too long! Drop it like a hot potato
						Close();
						return;
					}

					nextFrame.Type = WebsocketFrameType.Pong;
					nextFrame.IsLastFrame = true;

					// Return a pong
					await sendFrame(nextFrame);

					break;
					
				case WebsocketFrameType.Pong:
					Log.WriteLine("[GlidingSquirrel/Websocket/FrameHandler] Received pong from {0}", RemoteEndpoint);
					break;

				case WebsocketFrameType.TextData:
					string recievedMessage = nextFrame.Payload;

					nextSeqFrame = nextFrame;
					while(!nextSeqFrame.IsLastFrame)
					{
						nextSeqFrame = await WebsocketFrame.Decode(connection.GetStream());
						if(nextSeqFrame.Type != WebsocketFrameType.ContinuationFrame)
						{
							// Handle any stray control frames we find
							await handleNextFrame(this, new NextFrameEventArgs() { Frame = nextSeqFrame });
						}
						recievedMessage += nextSeqFrame.Payload;
					}

					await OnTextMessage(this, new TextMessageEventArgs() {
						Payload = recievedMessage
					});

					break;

				case WebsocketFrameType.BinaryData:
					List<byte[]> receivedChunks = new List<byte[]>() {
						nextFrame.RawPayload
					};

					nextSeqFrame = nextFrame;
					while(!nextSeqFrame.IsLastFrame)
					{
						nextSeqFrame = await WebsocketFrame.Decode(connection.GetStream());
						if(nextSeqFrame.Type != WebsocketFrameType.ContinuationFrame)
						{
							// Handle any stray control frames we find
							await handleNextFrame(this, new NextFrameEventArgs() { Frame = nextSeqFrame });
						}
						receivedChunks.Add(nextSeqFrame.RawPayload);
					}

					long finalPayloadLength = receivedChunks.Sum((byte[] chunk) => (uint)chunk.Length);

					byte[] reassembledPayload = new byte[finalPayloadLength];
					long reassembledPosition = 0;
					foreach(byte[] chunk in receivedChunks)
					{
						Buffer.BlockCopy(chunk, 0, reassembledPayload, (int)reassembledPosition, chunk.Length);
						reassembledPosition += chunk.LongLength;
					}

					await OnBinaryMessage(this, new BinaryMessageEventArgs() { Payload = reassembledPayload });

					break;
			}
		}

		#endregion


		#region Interface Methods

		/// <summary>
		/// Sends the specified string to this client in a text frame.
		/// </summary>
		/// <param name="message">The message to send.</param>
		public async Task Send(string message)
		{
			WebsocketFrame frame = new WebsocketFrame() {
				IsLastFrame = true,
				Type = WebsocketFrameType.TextData,
				Payload = message
			};

			await sendFrame(frame);
		}

		/// <summary>
		/// Sends the given byte array to this client in a binary frame.
		/// </summary>
		/// <param name="payload">The payload to send.</param>
		public async Task Send(byte[] payload)
		{
			WebsocketFrame frame = new WebsocketFrame() {
				IsLastFrame = true,
				Type = WebsocketFrameType.BinaryData,
				RawPayload = payload
			};
			await sendFrame(frame);
		}

		/// <summary>
		/// Send a ping frame to this client.
		/// The websocket server handles sending these automagically, so you shouldn't
		/// need to call this method directly.
		/// </summary>
		public async Task Ping()
		{

			WebsocketFrame pingFrame = new WebsocketFrame() {
				Type = WebsocketFrameType.Ping,
				RawPayload = new byte[64]
			};
			rand.NextBytes(pingFrame.RawPayload);

			await sendFrame(pingFrame);
		}

		/// <summary>
		/// Gracefully closses the connection to thsi Websocket client.
		/// </summary>
		public void Close()
		{
			connection.Close();
			closingConnection = true;
		}
		/// <summary>
		/// Destroys this connection as fast as possible.
		/// Useful when a client is misbehaving.
		/// </summary>
		public void Destroy()
		{
			connection.Close();
			closingConnection = true;
		}

		#endregion

		#region Handshake Logic

		/// <summary>
		/// Handles an incoming Websocket client request and performing the associated handshake.
		/// </summary>
		/// <returns>A new Websocket client connection with the negotation completed.</returns>

		public static async Task<WebsocketClient> WithServerNegotiation(HttpRequest request, HttpResponse response)
		{
			if(!request.Headers.ContainsKey("sec-websocket-key"))
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required sec-websocket-key header.");
			if(!request.Headers.ContainsKey("upgrade") || request.Headers["upgrade"].ToLower() != "websocket")
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required upgrade header set to 'websocket'.");
			if(!request.Headers.ContainsKey("connection") || !Connection.Contains(request.Headers["connection"], Connection.Upgrade))
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required connection header set to 'upgrade'.");
			if(!request.Headers.ContainsKey("sec-websocket-version") || request.Headers["sec-websocket-version"] != "13")
			{
				response.Headers.Add("Sec-WebSocket-Version", "13");
				// todo handle this properly
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required sec-websocket-version header set to '13'.");
			}

			// Disable the read timeout set to kill idle http connections, as we're setting up a websocket naow!
			request.ClientConnection.ReceiveTimeout = 0;

			WebsocketClient client = new WebsocketClient() {
				connection = request.ClientConnection
			};

			response.ResponseCode = HttpResponseCode.SwitchingProtocotolsWebsocket;
			response.Headers["Upgrade"] = "websocket";
			response.Headers["Connection"] = Connection.Upgrade;
			response.Headers.Add(
				"Sec-WebSocket-Accept",
				CompleteWebsocketKeyChallenge(request.GetHeaderValue("sec-websocket-key", ""))
			);

			StreamWriter outgoing = new StreamWriter(
				request.ClientConnection.GetStream(),
				new UTF8Encoding(false),
				1024,
				true
			) { AutoFlush = true };
			await response.SendTo(outgoing);
			outgoing.Dispose();

			return client;
		}

		/// <summary>
		/// Calculates the appropriate response to the sec-websocket-key header challenge.
		/// </summary>
		/// <param name="key">The challenge key to calculate the response for.</param>
		/// <returns>The websoocket key challenge.</returns>
		public static string CompleteWebsocketKeyChallenge(string key)
		{
			if(key.Trim().Length == 0)
				throw new WebsocketClientHandshakeException("Error: That sec-websocket-key is invalid.");

			byte[] hash;
			using(SHA1Managed sha1hasher = new SHA1Managed())
			{
				hash = sha1hasher.ComputeHash(Encoding.UTF8.GetBytes(key.Trim() + WebsocketServer.MagicChallengeKey));
			}
			return Convert.ToBase64String(hash);
		}

		#endregion
	}
}
