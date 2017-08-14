using System;
using System.IO;
using SBRL.GlidingSquirrel.Http;

namespace SBRL.GlidingSquirrel.Websocket
{
	public class WebsocketClient
	{
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

        /// <summary>
        /// Handles an incoming Websocket client request and performing the associated handshake.
        /// </summary>
        /// <returns>A new Websocket client connection with the negotation completed.</returns>
        public static async WebsocketClient AfterServerNegotiation(HttpRequest request, HttpResponse response)
        {
            WebsocketClient client = new WebsocketClient();

            StreamReader incoming = request.Body;
            StreamWriter outgoing = response.

            return client;
        }
	}
}
