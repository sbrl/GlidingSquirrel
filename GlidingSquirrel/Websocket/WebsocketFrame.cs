using System;
namespace SBRL.GlidingSquirrel.Websocket
{
	enum PayloadLengthType
	{
		Bit7 = 0,
		Bit16 = 16,
		Bit64 = 64
	}

	public class WebsocketFrame
	{
		public static readonly int MaximumPayloadSize = int.MaxValue;

		public bool Fin { get; set; }
		public bool Rsv1 { get; set; }
		public bool Rsv2 { get; set; }
		public bool Rsv3 { get; set; }

		public bool Masked { get; set; }

		public int Opcode { get; set; }

		public byte[] MaskingKey;

		public byte[] Payload;

		public WebsocketFrame()
		{
		}


		public static WebsocketFrame Decode(byte[] rawFrame)
		{
			// todo change this to use a stream so that we can blow up on payloads that are too large without reading them
			WebsocketFrame result = new WebsocketFrame();

			result.Fin = (rawFrame[0] & 128) == 128;
			result.Rsv1 = (rawFrame[0] & 64) == 64;
			result.Rsv2 = (rawFrame[0] & 32) == 32;
			result.Rsv3 = (rawFrame[0] & 16) == 16;
			result.Opcode = rawFrame[0] & 15;

			result.Masked = (rawFrame[1] & 128) == 128;

			// Calculate the payload length
			PayloadLengthType payloadLengthType = PayloadLengthType.Bit7;
			ulong payloadLength = (byte)(rawFrame[1] & 127);
			switch(payloadLength)
			{
				case 126:
					payloadLengthType = PayloadLengthType.Bit16;
					payloadLength = BitConverter.ToUInt16(rawFrame, 2);
					break;
				case 127:
					payloadLengthType = PayloadLengthType.Bit64;
					payloadLength = BitConverter.ToUInt64(rawFrame, 2);
					break;
			}

			int offset = (int)payloadLengthType;

			if(result.Masked)
			{
				offset += 4;
				result.MaskingKey = new byte[4];
				Buffer.BlockCopy(rawFrame, 2 + offset, result.MaskingKey, 0, 4);
			}

			Buffer.BlockCopy(rawFrame, 2 + offset, result.Payload, 0, 

			return result;
		}
	}
}
