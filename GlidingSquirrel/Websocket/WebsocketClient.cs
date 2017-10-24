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
using System.Threading;

namespace SBRL.GlidingSquirrel.Websocket
{
	/// <summary>
	/// Something went wrong during the websocket handshake.
	/// </summary>
	public class WebsocketClientHandshakeException : Exception {
		/// <summary>
		/// Creates a new WebsocketClientHandshakeException.
		/// Normally, you won't need to call this directly.
		/// </summary>
		/// <param name="message">The exception message..</param>
		/// <param name="innerException">The inner exception that was thrown, if any.</param>
		public WebsocketClientHandshakeException(string message, Exception innerException = null)
			: base(message, innerException)
		{
		}
	}

	/// <summary>
	/// The delegate definition for the websockets OnFrameRecieved event.
	/// </summary>
	public delegate Task NextFrameEventHandler(object sender, NextFrameEventArgs eventArgs);
	/// <summary>
	/// The delegate definition for the websockets OnTextMessage event.
	/// </summary>
	public delegate Task TextMessageEventHandler(object sender, TextMessageEventArgs eventArgs);
	/// <summary>
	/// The delegate definition for the websockets OnBinaryMessage event.
	/// </summary>
	public delegate Task BinaryMessageEventHandler(object sender, BinaryMessageEventArgs eventArgs);
	/// <summary>
	/// The delegate definition for the websockets OnDisconnection event on both the websocket server and the client classes.
	/// </summary>
	public delegate Task ClientDisconnectedEventHandler(object sender, ClientDisconnectedEventArgs eventArgs);

	/// <summary>
	/// Represents a websockets connection to a remote client.
	/// Also used by the <see cref="WebsocketServer">WebsocketServer</see> class to represent
	/// a currently conencted client, since they practically use the same handling code and all :P
	/// </summary>
	public class WebsocketClient
	{
		private TcpClient connection;
		/// <summary>
		/// An instance of C#'s built-in random number generator. 
		/// </summary>
		protected Random rand = new Random();
		/// <summary>
		/// The date and time that the last communication from this websockets client was received.
		/// Useful for dropping idle connections, since this value takes pong frames into account.
		/// </summary>
		public DateTime LastCommunication = DateTime.Now;

		/// <summary>
		/// Whether the server should close the connection if a frame with an unknown frame type is receieved.
		/// </summary>
		public readonly bool CloseOnUnknownFrameType = true;
		/// <summary>
		/// The number of milliseconds to wait for a reply to close frames we send out.
		/// </summary>
		public int CloseFrameReplyWaitTime = 5000;

		/// <summary>
		/// Whether we've received a close frame from this client.
		/// </summary>
		public bool ReceivedCloseFrame { get; private set; } = false;
		/// <summary>
		/// Whether we've sent a close frame to this client.
		/// </summary>
		public bool SentCloseFrame { get; private set; } = false;
		/// <summary>
		/// Whether this connection was closed uncleanly or not.
		/// </summary>
		public bool UncleanExit { get; private set; } = false;
		/// <summary>
		/// The code with which this websocket client connection exited with.
		/// </summary>
		public WebsocketCloseReason ExitCode { get; private set; } = WebsocketCloseReason.NotClosedYet;
		/// <summary>
		/// Whether this websocket client is currently in the process of closing it's connection.
		/// </summary>
		public bool IsClosing {
			get {
				return ReceivedCloseFrame || SentCloseFrame;
			}
		}
		/// <summary>
		/// Whether this websocket client has closed it connection or not.
		/// Note that even if the connection is open, you may not be able to send a message.
		/// See <see cref="IsClosing"/> for more information.
		/// </summary>
		public bool IsClosed {
			get {
				return (ReceivedCloseFrame && SentCloseFrame) || !connection.Connected;
			}
		}

		/// <summary>
		/// The maximum size of any websocket frames sent to this client.
		/// Defaults to 2MiB.
		/// </summary>
		public int MaximumTransmissionSize { get; set; } = 2 * 1024 * 1024;
		/// <summary>
		/// The remote endpoint that this websocket client is connected to.
		/// </summary>
		/// <value>The remote endpoint.</value>
		public IPEndPoint RemoteEndpoint { get; private set; }
		/// <summary>
		/// Occurs when a frame is recieved from the remote host. You probably want OnTextMessage or OnBinaryMessage ;-)
		/// </summary>
		public event NextFrameEventHandler OnFrameRecieved;
		/// <summary>
		/// Occurs when a text message is received from the remote host. The default encoding is UTF8.
		/// </summary>
		public event TextMessageEventHandler OnTextMessage;
		/// <summary>
		/// Occurs when a binary message is received.
		/// </summary>
		public event BinaryMessageEventHandler OnBinaryMessage;
		/// <summary>
		/// Occurs when this client disconnects. Don't try to send any more frames after this event has been fired!
		/// </summary>
		public event ClientDisconnectedEventHandler OnDisconnection;

		/// <summary>
		/// Creates a new blank Websocket connection.
		/// Does not perform any negotiation!
		/// See WebsocketClient::AfterServerNegotiation() and WebsocketClient(string remoteAddress).
		/// </summary>
		private WebsocketClient()
		{
			OnFrameRecieved += handleNextFrame;
		}
		/// <summary>
		/// Creates a new websocket client connection.
		/// </summary>
		/// <param name="remoteAddress">Remote address.</param>
		public WebsocketClient(string remoteAddress) : this()
		{
			throw new NotImplementedException("Error: Websocket Client connections haven't been implemented yet!");
		}


		#region Frame Handling

		/// <summary>
		/// Called when the listening socket is being setup. Useful for, ummm setup logic that needs
		/// variables to be preinitialised. May be overridden, but make sure that this method is still called!
		/// </summary>
		protected void setup()
		{
			// Record the remote endpoint
			// In case the connection gets garbage collected we want to know who it was who was connected!
			RemoteEndpoint = (IPEndPoint)connection.Client.RemoteEndPoint;
		}

		/// <summary>
		/// Listens for frames from this Websockets client and handles them appropriately, firing the right events.
		/// </summary>
		public async Task Listen()
		{
			while(true)
			{
				WebsocketFrame nextFrame = await WebsocketFrame.Decode(connection.GetStream());

				if(nextFrame == null)
					break;

				await OnFrameRecieved(this, new NextFrameEventArgs() { Frame = nextFrame });
				LastCommunication = DateTime.Now;

				if(IsClosed)
					break;
			}
		}

		/// <summary>
		/// Sends a websocket frame to this client.
		/// </summary>
		/// <param name="frame">The frame to send..</param>
		protected async Task sendFrame(WebsocketFrame frame)
		{
			if(IsClosed)
				return;

			try
			{
				await frame.SendToAsync(connection.GetStream());
			}
			catch(Exception error)
			{
				if(!(error is IOException || error is SocketException))
					throw;

				Log.WriteLine(LogLevel.Error, "Caught {0} whilst sending message to client - closing connection", error.GetType());
				Log.WriteLine(LogLevel.Error, "{0} message: {1}", error.GetType(), error.Message);
				// Close the connection and tell everyone about what has just happened
				await Destroy();
			}
		}

		/// <summary>
		/// Handles a single WebsocketFrame.
		/// </summary>
		/// <param name="sender">The sending websockets client.</param>
		/// <param name="nextFrameEventArgs">The ${ParameterType} instance containing the event data to process - including the all-important websockets frame.</param>
		protected async Task handleNextFrame(object sender, NextFrameEventArgs nextFrameEventArgs)
		{
			WebsocketClient client = sender as WebsocketClient;
			WebsocketFrame nextFrame = nextFrameEventArgs.Frame;
			WebsocketFrame nextSeqFrame;

			Log.WriteLine(
				LogLevel.Debug,
				"[GlidingSquirrel/WebsocketClient] Got {0} frame of length {1} ({2}) from {3}",
				nextFrame.Type,
				nextFrame.RawPayload.Length,
				nextFrame.PayloadLengthType,
				client.RemoteEndpoint
			);

			// todo close the connection properly here

			switch(nextFrame.Type)
			{
				case WebsocketFrameType.Close:
					// Close the connection as requested
					ReceivedCloseFrame = true;


					WebsocketCloseReason closeReason = WebsocketCloseReason.NoStatusCodePresent;
					string closeMessage = string.Empty;
					if(nextFrame.RawPayload.Length >= 2)
					{
						// Convert the close reason to host byte order for decoding, if present
						nextFrame.RawPayload = ByteUtilities.NetworkToHostByteOrder(
							nextFrame.RawPayload,
							0, 2
						);
						// Actually decode the close reason
						closeReason = (WebsocketCloseReason)BitConverter.ToUInt16(nextFrame.RawPayload, 0);

						// The close message must be at least 0 bytes long! :P
						int closeMessageLength = Math.Max(nextFrame.RawPayload.Length - 2, 0);
						if(closeMessageLength > 0)
							closeMessage = new string(Encoding.UTF8.GetChars(nextFrame.RawPayload, 2, closeMessageLength));
					}


					await Close(closeReason, closeMessage);
					break;	

				case WebsocketFrameType.ContinuationFrame:
					throw new Exception("Error: Can't process a continuation frame when there's" +
				"nothing to continue!");

				case WebsocketFrameType.Ping:
					if(nextFrame.RawPayload.Length > 125)
					{
						// The payload is too long! Drop it like a hot potato
						await Close(WebsocketCloseReason.FrameTooBig, "That last ping frame was too big! This server supports a maximum of 125 bytes in a ping frame.");
						return;
					}

					nextFrame.Type = WebsocketFrameType.Pong;
					nextFrame.IsLastFrame = true;

					// Return a pong
					await sendFrame(nextFrame);

					break;
					
				case WebsocketFrameType.Pong:
					Log.WriteLine(LogLevel.Debug, "[GlidingSquirrel/Websocket/FrameHandler] Received pong from {0}", RemoteEndpoint);
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

				default:
					Log.WriteLine(
						LogLevel.Error,
						"[GlidingSquirrel/WebsocketClient] Got unknown frame with opcode {0} from {1}" + (CloseOnUnknownFrameType ? " - closing connection" : " - ignoring"),
						nextFrame.Type,
						RemoteEndpoint
					);

					if(CloseOnUnknownFrameType)
					{
						Log.WriteLine(LogLevel.Error, "Closing connection because of unknown frame type");
						await Close(
							WebsocketCloseReason.NotAcceptableDataType,
							$"The opcode {nextFrame.Opcode} is not supported by this server. Perhaps you're trying to use an old websockets draft?"
						);
					}
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
		/// Gracefully closes the connection to this Websocket client.
		/// </summary>
		public async Task Close(WebsocketCloseReason closeReason, string closingMessage)
		{
			// If we haven't received a close frame yet, give them a chance to send one
			if(!ReceivedCloseFrame)
			{
				OnFrameRecieved += async (object sender, NextFrameEventArgs eventArgs) => {
					WebsocketFrame frame = eventArgs.Frame;
					// We're only interested in close frames here
					if(frame.Type != WebsocketFrameType.Close)
						return;
					
					ReceivedCloseFrame = true;
					await Destroy();
				};

				// Queue a task that kills the connection if the client is sluggish in responding to our close frame
				ThreadPool.QueueUserWorkItem(async (object state) => {
					await Task.Delay(CloseFrameReplyWaitTime);
					if(!IsClosed)  {
						await Destroy();
						UncleanExit = true;
						Log.WriteLine(LogLevel.Warning, "[GlidingSquirrel/WebsocketClient] Killing unresponsive connection - we're  not waiting any longer for the ack close frame");
					}
				});
			}

			// Record the reason we're closing this connection
			ExitCode = closeReason;

			// If we haven't yet sent a close frame, do so
			if(!SentCloseFrame) {
				// Send an empty close frame in response if we've already received a close frame
				if(ReceivedCloseFrame)
					await sendEmptyCloseFrame();
				else
					await sendCloseFrame(closeReason, closingMessage);
			}

			// If we've completed the closing handshake, go out in style :D
			if(SentCloseFrame && ReceivedCloseFrame)
				await Destroy();
			
		}

		/// <summary>
		/// Sends a close frame to this websocket client.
		/// You don't normally want to call this directly - use the normal Close() method instead.
		/// </summary>
		/// <param name="closeReason">The reason for closing the connection.</param>
		/// <param name="closingMessage">An explanatory message to send in the close frame.</param>
		private async Task sendCloseFrame(WebsocketCloseReason closeReason, string closingMessage)
		{
			WebsocketFrame closeFrame = WebsocketFrame.GenerateCloseFrame(closeReason, closingMessage);
			await sendFrame(closeFrame);
			SentCloseFrame = true;
		}

		/// <summary>
		/// Sends an empty close frame to this client.
		/// Useful when replying to close frames sent to us.
		/// </summary>
		private async Task sendEmptyCloseFrame()
		{
			WebsocketFrame closeFrame = new WebsocketFrame() {
				Type = WebsocketFrameType.Close,
				RawPayload = new byte[0]
			};
			await sendFrame(closeFrame);
			SentCloseFrame = true;
		}

		/// <summary>
		/// Destroys this connection as fast as possible.
		/// Useful when a client is misbehaving.
		/// </summary>
		public async Task Destroy()
		{
			// Close the connection
			connection.Close();

			// Fake the fact we've sent & received a closing frame
			SentCloseFrame = true;
			ReceivedCloseFrame = true;

			// Give everyone chance to clean up the mess
			if(OnDisconnection != null)
				await OnDisconnection(this, new ClientDisconnectedEventArgs() { CloseReason = ExitCode });
		}

		#endregion

		#region Handshake Logic

		/// <summary>
		/// Handles an incoming Websocket client request and performing the associated handshake.
		/// </summary>
		/// <returns>A new Websocket client connection with the negotation completed.</returns>

		public static async Task<WebsocketClient> WithServerNegotiation(TcpClient rawClient, HttpRequest handshakeRequest, HttpResponse handshakeResponse)
		{
			if(!handshakeRequest.Headers.ContainsKey("sec-websocket-key"))
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required sec-websocket-key header.");
			if(!handshakeRequest.Headers.ContainsKey("upgrade") || handshakeRequest.Headers["upgrade"].ToLower() != "websocket")
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required upgrade header set to 'websocket'.");
			if(!handshakeRequest.Headers.ContainsKey("connection") || !Connection.Contains(handshakeRequest.Headers["connection"], Connection.Upgrade))
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required connection header set to 'upgrade'.");
			if(!handshakeRequest.Headers.ContainsKey("sec-websocket-version") || handshakeRequest.Headers["sec-websocket-version"] != "13")
			{
				handshakeResponse.Headers.Add("Sec-WebSocket-Version", "13");
				// todo handle this properly
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required sec-websocket-version header set to '13'.");
			}

			// Disable the read timeout set to kill idle http connections, as we're setting up a websocket naow!
			handshakeRequest.ClientConnection.ReceiveTimeout = 0;

			WebsocketClient client = new WebsocketClient() {
				connection = handshakeRequest.ClientConnection
			};
			client.setup();

			handshakeResponse.ResponseCode = HttpResponseCode.SwitchingProtocotolsWebsocket;
			handshakeResponse.Headers["Upgrade"] = "websocket";
			handshakeResponse.Headers["Connection"] = Connection.Upgrade;
			handshakeResponse.Headers.Add(
				"Sec-WebSocket-Accept",
				CompleteWebsocketKeyChallenge(handshakeRequest.GetHeaderValue("sec-websocket-key", ""))
			);

			StreamWriter outgoing = new StreamWriter(
				rawClient.GetStream(),
				new UTF8Encoding(false),
				1024,
				true
			) { AutoFlush = true };
			await handshakeResponse.SendTo(outgoing);
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
