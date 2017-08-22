﻿using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SBRL.GlidingSquirrel.Websocket
{
	enum PayloadLengthType
	{
		Bit7 = 0,
		Bit16 = 16,
		Bit64 = 64
	}

    public enum WebsocketFrameType
    {
        ContinuationFrame = 0x0,
        TextData = 0x1,
        BinaryData = 0x2,

        Ping = 0x9,
        Pong = 0xA
    }

	public class WebsocketFrame
	{
		/// <summary>
		/// The maximum supported payload size, in bytes.
		/// </summary>
		public static readonly int MaximumPayloadSize = int.MaxValue;

        #region Raw Frame Data
        /// <summary>
        /// Whether the FIN bit is set in the websocket frame.
        /// </summary>
        public bool Fin { get; set; }
		/// <summary>
		/// Whether the RSV1 bit is set in the websocket frame.
		/// </summary>
		public bool Rsv1 { get; set; }
		/// <summary>
		/// Whether the RSV2 bit is set in the websocket frame.
		/// </summary>
		public bool Rsv2 { get; set; }
		/// <summary>
		/// Whether the RSV3 bit is set in the websocket frame.
		/// </summary>
		public bool Rsv3 { get; set; }

		/// <summary>
		/// Whether the websocket frame's payload is (or was) masked.
		/// </summary>
		public bool Masked { get; set; }
		/// <summary>
		/// The frame type of this websocket frame.
		/// </summary>
		public int Opcode { get; set; }
		/// <summary>
		/// The key used ot mask the payload.
		/// </summary>
		public byte[] MaskingKey;

        #endregion

        public bool IsLastFrame
        {
            get {
                return Fin;
            }
        }

        public WebsocketFrameType Type
        {
            get {
                return (WebsocketFrameType)Enum.Parse(typeof(WebsocketFrameType), Opcode.ToString());
            }
        }

        public byte[] RawPayload;

		public string Payload {
			get {
				return Encoding.UTF8.GetString(RawPayload);
			}
		}


		public WebsocketFrame()
		{
		}

		public async Task SendTo(NetworkStream clientStream)
		{
			byte[] headerBuffer = new byte[4];

			headerBuffer[0] |= (byte)(Convert.ToByte(Fin) << 7);
			headerBuffer[0] |= (byte)(Convert.ToByte(Rsv1) << 6);
			headerBuffer[0] |= (byte)(Convert.ToByte(Rsv2) << 5);
			headerBuffer[0] |= (byte)(Convert.ToByte(Rsv3) << 4);
			if(Opcode > 0b1111)
				throw new InvalidDataException("Error: The opcode value can't be greater than 15 (0b1111)!");
			headerBuffer[0] |= Convert.ToByte(Opcode);

			headerBuffer[1] |= (byte)(Convert.ToByte(Masked) << 7);

			PayloadLengthType payloadLengthType = PayloadLengthType.Bit7;
			byte payloadLengthValue = (byte)RawPayload.Length;
			if(RawPayload.Length > 125 && RawPayload.Length <= 65535)
			{
				payloadLengthType = PayloadLengthType.Bit16;
				payloadLengthValue = 126;
			}
			else if(RawPayload.Length > 65535)
			{
				payloadLengthType = PayloadLengthType.Bit64;
				payloadLengthValue = 127;
			}

			if(payloadLengthValue > 127)
				throw new InvalidDataException($"Error: That payloadLengthValue of {payloadLengthValue} is invalid.");

			headerBuffer[1] |= payloadLengthValue;

			// Write the main header to the stream
			await clientStream.WriteAsync(headerBuffer, 0, 2);

			// Write the extended payload length (if any) to the stream
			if(payloadLengthType == PayloadLengthType.Bit16)
			{
				byte[] payloadLength16 = BitConverter.GetBytes((ushort)RawPayload.Length);
				await clientStream.WriteAsync(payloadLength16, 0, 2);
			}
			if(payloadLengthType == PayloadLengthType.Bit64)
			{
				byte[] payloadLength64 = BitConverter.GetBytes((ulong)RawPayload.Length);
				await clientStream.WriteAsync(payloadLength64, 0, 8);
			}

			// Write the masking key to the stream if needed
			if(Masked)
				await clientStream.WriteAsync(MaskingKey, 0, 4);

			await clientStream.FlushAsync();

			// Write the payload to the stream
			await clientStream.WriteAsync(RawPayload, 0, RawPayload.Length);

			await clientStream.FlushAsync();
		}


		public static async Task<WebsocketFrame> Decode(NetworkStream  clientStream)
		{
			// todo change this to use a stream so that we can blow up on payloads that are too large without reading them
			WebsocketFrame result = new WebsocketFrame();

			// Read the websocket header
			byte[] headerBuffer = new byte[4];
			await clientStream.ReadAsync(headerBuffer, 0, 2);

			result.Fin = (headerBuffer[0] & 128) == 128;
			result.Rsv1 = (headerBuffer[0] & 64) == 64;
			result.Rsv2 = (headerBuffer[0] & 32) == 32;
			result.Rsv3 = (headerBuffer[0] & 16) == 16;
			result.Opcode = headerBuffer[0] & 15;

			result.Masked = (headerBuffer[1] & 128) == 128;

			// Calculate the payload length
			PayloadLengthType payloadLengthType = PayloadLengthType.Bit7;
			ulong payloadLength = (byte)(headerBuffer[1] & 127);
			switch(payloadLength)
			{
				case 126:
					// It's a 16-bit header! (ab)use the headerBuffer to hold the payload size for decoding
					payloadLengthType = PayloadLengthType.Bit16;
					await clientStream.ReadAsync(headerBuffer, 0, 2);
					payloadLength = BitConverter.ToUInt16(headerBuffer, 2);
					break;
				case 127:
					// It's a 64-bit header! (ab)use the headerBuffer to hold the payload size for decoding
					payloadLengthType = PayloadLengthType.Bit64;
					await clientStream.ReadAsync(headerBuffer, 0, 4);
					payloadLength = BitConverter.ToUInt64(headerBuffer, 4);
					break;
			}

			if(result.Masked)
			{
				await clientStream.ReadAsync(headerBuffer, 0, 4);
				result.MaskingKey = (byte[])headerBuffer.Clone();
			}

			result.RawPayload = new byte[payloadLength];
			await clientStream.ReadAsync(result.RawPayload, 0, (int)payloadLength);

			// Unmask the payload if reequired
			if(result.Masked)
			{
				for(int i = 0; i < result.RawPayload.Length; i++)
				{
					result.RawPayload[i] ^= result.MaskingKey[i % 4];
				}
			}

			return result;
		}
	}
}
