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
		NotClosedYet = 0,
		Normal = 1000,
		Shutdown = 1001,
		ProtocolError = 1002,
		NotAcceptableDataType = 1003,
		Reserved = 1004,
		NoStatusCodePresent = 1005,
		AbnormalClosure = 1006,
		InConsistentDataEncoding = 1007,
		PolicyViolation = 1008,
		FrameTooBig = 1009,
		ExtensionsNotPresent = 1010,
		UnexpectedCondition = 1011,
		TlsHandshakeFailure = 1015
	}
}
