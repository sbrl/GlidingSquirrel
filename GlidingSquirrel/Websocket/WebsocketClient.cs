using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SBRL.GlidingSquirrel.Http;

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

    public delegate void TextMessageEventHandler(object sender, TextMessageEventArgs eventArgs);
    public delegate void BinaryMessageEventHandler(object sender, BinaryMessageEventArgs eventArgs);

	public class WebsocketClient
	{
		private TcpClient connection;
		private StreamReader incoming;
		private StreamWriter outgoing;

        public event TextMessageEventHandler OnTextMessage;
        public event BinaryMessageEventHandler OnBinaryMessage;

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
            
        }

        protected async Task handleNextFrame(object sender, NextFrameEventArgs nextFrameEventArgs)
        {
            WebsocketFrame nextFrame = nextFrameEventArgs.Frame;

            switch(nextFrame.Type) {
                case WebsocketFrameType.ContinuationFrame:
                    throw new Exception("Error: Can't process a continuation frame when there's" +
                "nothing to continue!");

                case WebsocketFrameType.TextData:
                    if(nextFrame.IsLastFrame)
                        OnTextMessage(this, new TextMessageEventArgs() { Payload = nextFrame.Payload });

                    throw new NotImplementedException("Error: Frame fragmentation hasn't been implemented yet.");

                    break;
            }
        }

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
			if(!request.Headers.ContainsKey("sec-websocket-version") || request.Headers["sec-websocket-version"] != "13") {
				response.Headers.Add("sec-websocket-version", "13");
				throw new WebsocketClientHandshakeException("Error: That request didn't contain the required sec-websocket-version header set to '13'.");
			}

			// Disable the read timeout set to kill idle http connections, as we're setting up a websocket naow!
			request.ClientConnection.ReceiveTimeout = 0;

			WebsocketClient client = new WebsocketClient() {
				connection = request.ClientConnection,
				incoming = new StreamReader(request.ClientConnection.GetStream()),
				outgoing = new StreamWriter(request.ClientConnection.GetStream()) { AutoFlush = true }
			};

			response.ResponseCode = HttpResponseCode.SwitchingProtocols;
			response.Headers.Add("upgrade", "websocket");
			response.Headers.Add("connection", Connection.Upgrade);
			response.Headers.Add(
				"sec-websocket-accept",
				completeWebsocketKeyChallenge(request.GetHeaderValue("sec-websocket-key", ""))
			);

			await response.SendTo(client.outgoing);


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
	}
}
