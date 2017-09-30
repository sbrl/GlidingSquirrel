using System;
namespace SBRL.GlidingSquirrel.Websocket
{

	/// <summary>
	/// Indicates the method by which a payload's length was specified.
	/// </summary>
	public enum PayloadLengthType
	{
		Bit7 = 0,
		Bit16 = 16,
		Bit64 = 64
	}

	/// <summary>
	/// Indicates the a type of websocket frame.
	/// </summary>
	public enum WebsocketFrameType
	{
		ContinuationFrame = 0x0,
		TextData = 0x1,
		BinaryData = 0x2,

		Close = 0x8,

		Ping = 0x9,
		Pong = 0xA
	}

	/// <summary>
	/// Indicates the reason why a websocket connection is beingg closed.
	/// </summary>
	public enum WebsocketCloseReason
	{
		/// <summary>
		/// The connection hasn't closed yet. You shouldn't ever see this status code when a connection has
		/// been closed! If you do, then it means there's a _bug_.
		/// </summary>
		NotClosedYet = 0,
		/// <summary>
		/// The connection was closed normally.
		/// </summary>
		Normal = 1000,
		/// <summary>
		/// The connection was closed because either or both parties are shutting down / going away.
		/// </summary>
		Shutdown = 1001,
		/// <summary>
		/// An involved party had an issue with the syntax of a message they received from the other party.
		/// </summary>
		ProtocolError = 1002,
		/// <summary>
		/// An involved party didn't like the type of frame sent by the other party.
		/// </summary>
		NotAcceptableDataType = 1003,
		/// <summary>
		/// A reserved close code. Should not be used.
		/// </summary>
		Reserved = 1004,
		/// <summary>
		/// No status code was specified.
		/// </summary>
		NoStatusCodePresent = 1005,
		/// <summary>
		/// The connection was closed abnormally - probably due to an exception being thrown somewhere.
		/// </summary>
		AbnormalClosure = 1006,
		/// <summary>
		/// A text message was received that doesn't match the encoding that it claims to be written in.
		/// </summary>
		InconsistentDataEncoding = 1007,
		/// <summary>
		/// A generic code for when a message sent by the other party violates the receiving party's policy.
		/// </summary>
		PolicyViolation = 1008,
		/// <summary>
		/// A frame was received that's too big for the receiving party to process.
		/// </summary>
		FrameTooBig = 1009,
		/// <summary>
		/// One or more required extensions weren't negotiated in the initial handshake.
		/// </summary>
		ExtensionsNotPresent = 1010,
		/// <summary>
		/// The server has encountered an 'unexpected' condition that is preventing it from fulfilling the
		/// request in the frame received.
		/// </summary>
		UnexpectedCondition = 1011,
		/// <summary>
		/// The TLS handshake failed. This _shouldn't_ be actually sent to the client - it's for logging
		/// purposes only.
		/// </summary>
		TlsHandshakeFailure = 1015,
		/// <summary>
		/// The close reason sent or received has been lost somehow - probably due to garbage collection.
		/// </summary>
		CloseReasonLost = 4888
	}
}
