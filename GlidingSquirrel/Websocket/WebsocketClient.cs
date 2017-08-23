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

		public async Task Listen()
		{
			while(true)
			{
				WebsocketFrame nextFrame = await WebsocketFrame.Decode(connection.GetStream());

				await OnFrameRecieved(this, new NextFrameEventArgs() { Frame = nextFrame });

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

		public void Close()
		{
			connection.Close();
			closingConnection = true;
		}

		#region Handshake

		/// <summary>
		/// Handles an incoming Websocket client request and performing the associated handshake.
		/// </summary>
		/// <returns>A new Websocket client connection with the negotation completed.</returns>

		public static async Task<WebsocketClient> WithServerNegotiation(HttpRequest request, HttpResponse response)
		{
			if(!request.Headers.ContainsKey("sec-websocket-key"))
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required sec-websocket-key header.");
			if(!request.Headers.ContainsKey("upgrade") || request.Headers["upgrade"] != "websocket")
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required upgrade header set to 'websocket'.");
			if(!request.Headers.ContainsKey("connection") || request.Headers["connection"] != Connection.Upgrade)
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required connection header set to 'upgrade'.");
			if(!request.Headers.ContainsKey("sec-websocket-version") || request.Headers["sec-websocket-version"] != "13")
			{
				response.Headers.Add("sec-websocket-version", "13");
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required sec-websocket-version header set to '13'.");
			}

			// Disable the read timeout set to kill idle http connections, as we're setting up a websocket naow!
			request.ClientConnection.ReceiveTimeout = 0;

			WebsocketClient client = new WebsocketClient() {
				connection = request.ClientConnection
			};

			response.ResponseCode = HttpResponseCode.SwitchingProtocols;
			response.Headers.Add("upgrade", "websocket");
			response.Headers.Add("connection", Connection.Upgrade);
			response.Headers.Add(
				"sec-websocket-accept",
				completeWebsocketKeyChallenge(request.GetHeaderValue("sec-websocket-key", ""))
			);

			StreamWriter outgoing = new StreamWriter(
				request.ClientConnection.GetStream(),
				Encoding.UTF8,
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
		protected static string completeWebsocketKeyChallenge(string key)
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
