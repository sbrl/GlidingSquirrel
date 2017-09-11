using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SBRL.GlidingSquirrel.Websocket
{
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
		public bool Fin { get; private set; } = true;
		/// <summary>
		/// Whether the RSV1 bit is set in the websocket frame.
		/// </summary>
		public bool Rsv1 { get; set; } = false;
		/// <summary>
		/// Whether the RSV2 bit is set in the websocket frame.
		/// </summary>
		public bool Rsv2 { get; set; } = false;
		/// <summary>
		/// Whether the RSV3 bit is set in the websocket frame.
		/// </summary>
		public bool Rsv3 { get; set; } = false;

		/// <summary>
		/// Whether the websocket frame's payload is (or was) masked.
		/// </summary>
		public bool Masked { get; set; } = false;
		/// <summary>
		/// The frame type of this websocket frame.
		/// </summary>
		public int Opcode { get; private set; }
		/// <summary>
		/// The key used to mask the payload.
		/// </summary>
		public byte[] MaskingKey;

		/// <summary>
		/// The method used when encoding the payload length.
		/// </summary>
		public PayloadLengthType PayloadLengthType;

		#endregion

		/// <summary>
		/// Whether this is the last frame in it's series.
		/// Useful when dealing with continuation frames.
		/// </summary>
		public bool IsLastFrame {
			get {
				return Fin;
			}
			set {
				Fin = value;
			}
		}

		/// <summary>
		/// The type of websocket frame.
		/// </summary>
		public WebsocketFrameType Type {
			get {
				return (WebsocketFrameType)Enum.Parse(typeof(WebsocketFrameType), Opcode.ToString());
			}
			set {
				Opcode = (int)value;
			}
		}

		/// <summary>
		/// The raw byte-for-byte payload carried by this websocket frame.
		/// </summary>
		public byte[] RawPayload;

		/// <summary>
		/// The payload of this websocket frame, represented as a string.
		/// </summary>
		/// <value>The payload.</value>
		public string Payload {
			get {
				return Encoding.UTF8.GetString(RawPayload);
			}
			set {
				RawPayload = Encoding.UTF8.GetBytes(value);
			}
		}

		/// <summary>
		/// Creates a new blank websocket frame.
		/// Properties can be set with the { .... } syntax, since there are so many of them :P
		/// </summary>
		public WebsocketFrame()
		{
		}

		public static WebsocketFrame GenerateCloseFrame(WebsocketCloseReason closeReason, string closingMessage)
		{
			WebsocketFrame result = new WebsocketFrame();
			result.Type = WebsocketFrameType.Close;
			result.RawPayload = new byte[2 + Encoding.UTF8.GetByteCount(closingMessage)];

			// Paste the close reason code into the new close frame
			byte[] rawCloseReason = ByteUtilities.HostToNetworkByteOrder(
				BitConverter.GetBytes((ushort)closeReason),
				0, 2
			);
			Buffer.BlockCopy(rawCloseReason, 0, result.RawPayload, 0, 2);

			// Paste the closing message into the new close frame
			byte[] rawCloseMessage = Encoding.UTF8.GetBytes(closingMessage);
			Buffer.BlockCopy(rawCloseMessage, 0, result.RawPayload, 2, rawCloseMessage.Length);

			return result;
		}

		#region Sending / Receiving

		/// <summary>
		/// Transmits this websocket frame via the specified network stream.
		/// </summary>
		/// <param name="clientStream">The network stream to transmit this frame via.</param>
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
				byte[] payloadLength16 = ByteUtilities.HostToNetworkByteOrder(
					BitConverter.GetBytes((ushort)RawPayload.Length),
					0, 2
				);
				await clientStream.WriteAsync(payloadLength16, 0, 2);
			}
			if(payloadLengthType == PayloadLengthType.Bit64)
			{
				byte[] payloadLength64 = ByteUtilities.HostToNetworkByteOrder(
					BitConverter.GetBytes((ulong)RawPayload.Length),
					0, 8
				);
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


		public static async Task<WebsocketFrame> Decode(NetworkStream clientStream)
		{
			// todo change this to use a stream so that we can blow up on payloads that are too large without reading them
			WebsocketFrame result = new WebsocketFrame();

			// Read the websocket header
			byte[] headerBuffer = new byte[4];
			if(await clientStream.ReadAsync(headerBuffer, 0, 2) == 0)
				return null;

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
					// It's a 16-bit header!
					payloadLengthType = PayloadLengthType.Bit16;
					byte[] rawPayloadLength16 = new byte[2];
					await clientStream.ReadAsync(rawPayloadLength16, 0, 2);
					payloadLength = BitConverter.ToUInt16(rawPayloadLength16, 0);
					break;
				case 127:
					// It's a 64-bit header!
					payloadLengthType = PayloadLengthType.Bit64;
					byte[] rawPayloadLength64 = new byte[8];
					await clientStream.ReadAsync(rawPayloadLength64, 0, 8);
					headerBuffer = ByteUtilities.NetworkToHostByteOrder(rawPayloadLength64, 0, 8);
					payloadLength = BitConverter.ToUInt64(headerBuffer, 0);
					break;
			}
			result.PayloadLengthType = payloadLengthType;

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

		#endregion

	}
}
