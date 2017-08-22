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

    public class NextFrameEventArgs : EventArgs
    {
        public WebsocketFrame Frame;

    }

    public class TextMessageEventArgs : EventArgs
    {
        public string Payload;

    }

    public class BinaryMessageEventArgs : EventArgs
    {
        public byte[] Payload;

    }

	public class ClientDisconnectedEventArgs : EventArgs
	{

	}
}
