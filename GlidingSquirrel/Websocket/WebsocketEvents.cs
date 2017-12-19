using System;

namespace SBRL.GlidingSquirrel.Websocket
{
	/// <summary>
	/// Event arguments passed when the ClientConnected event is fired.
	/// </summary>
	public class ClientConnectedEventArgs : EventArgs
	{
		/// <summary>
		/// The connecting client.
		/// </summary>
		public WebsocketClient ConnectingClient;
	}

	/// <summary>
	/// Event arguments passed when the NextFrame event is fired.
	/// </summary>
	public class NextFrameEventArgs : EventArgs
    {
		/// <summary>
		/// The frame received frame.
		/// </summary>
        public WebsocketFrame Frame;
		/// <summary>
		/// Whether this is a stray control frame or not.
		/// </summary>
		public bool IsStrayControlFrame;
    }

	/// <summary>
	/// Event arguments passed when the TextMessage event is fired.
	/// </summary>
	public class TextMessageEventArgs : EventArgs
    {
		/// <summary>
		/// The reassembled payload received.
		/// </summary>
		public string Payload;
    }

	/// <summary>
	/// Event arguments passed when the BinaryMessage event is fired.
	/// </summary>
	public class BinaryMessageEventArgs : EventArgs
    {
		/// <summary>
		/// The reassembled payload received.
		/// </summary>
        public byte[] Payload;
    }

	/// <summary>
	/// Event arguments passed when the ClientDisconnected event is fired.
	/// </summary>
	public class ClientDisconnectedEventArgs : EventArgs
	{
		/// <summary>
		/// The reason the client disconnected.
		/// </summary>
		public WebsocketCloseReason CloseReason;
	}
}
