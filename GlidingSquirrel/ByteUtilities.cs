using System;
namespace SBRL.GlidingSquirrel
{
	public static class ByteUtilities
	{
		/// <summary>
		/// Converts a byte array in network byte order to host byte order.
		/// </summary>
		/// <param name="networkArray">The array to convert.</param>
		/// <param name="offset">The offset in the provided array at which the conversion should take place.</param>
		/// <param name="length">The number of bytes that should be converted.</param>
		/// <returns>The given array in the correct order for decoding on the current host system.</returns>
		public static byte[] NetworkToHostByteOrder(byte[] networkArray, int offset, int length)
		{
			if(!BitConverter.IsLittleEndian)
				return networkArray;

			Array.Reverse(networkArray, offset, length);
			return networkArray;
		}
		/// <summary>
		/// Converts a byte array in host byte order to network byte order.
		/// </summary>
		/// <param name="hostArray">The array to convert.</param>
		/// <param name="offset">The offset in the provided array at which the conversion should take place.</param>
		/// <param name="length">The number of bytes that should be converted.</param>
		/// <returns>The given array in the correct order for sending over the network.</returns>
		public static byte[] HostToNetworkByteOrder(byte[] hostArray, int offset, int length)
		{
			return NetworkToHostByteOrder(hostArray, offset, length);
		}

		/// <summary>
		/// Convert a sequence of bytes (in network byte order) to a long value
		/// </summary>
		/// <description>
		/// Retrieved from https://github.com/sensaura-public/iotweb/blob/14b778b54b07e2e2b46b045869d5e4e8014d2de5/IotWeb%20Portable/Http/WebSocket.cs#L70-L83
		/// Modified by Starbeamrainbowlabs
		/// </description>
		/// <param name="data">The data to convert.</param>
		/// <param name="length">The numberof bytes the data contains.</param>
		/// <returns></returns>
		private static long LongFromNetworkBytes(byte[] data, int length)
		{
			UInt64 value = 0;
			for(int i = 0; i < length; i++)
				value = (value << 8) + (UInt64)data[i];
			return (long)value;
		}
	}
}
