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
	}
}
