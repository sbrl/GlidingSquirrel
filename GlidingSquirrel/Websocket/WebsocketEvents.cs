using System;

namespace SBRL.GlidingSquirrel.Websocket
{
	public class ClientConnectedEventArgs : EventArgs
	{
		public WebsocketClient ConnectingClient;

		public ClientConnectedEventArgs(WebsocketClient inConnectingClient) : base()
		{
			ConnectingClient = inConnectingClient;
		}
	}
}
