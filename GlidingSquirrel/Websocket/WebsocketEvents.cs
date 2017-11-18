using System;

namespace SBRL.GlidingSquirrel.Websocket
{
	public class ClientConnectedEventArgs : EventArgs
	{
		public WebsocketClient ConnectingClient;
	}

    public class NextFrameEventArgs : EventArgs
    {
        public WebsocketFrame Frame;
		public bool IsStrayControlFrame;
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
		public WebsocketCloseReason CloseReason;
	}
}
