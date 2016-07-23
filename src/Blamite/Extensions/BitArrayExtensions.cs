using System.Collections;

namespace Blamite.Extensions
{
	public static class BitArrayExtensions
	{
		public static byte[] ToByteArray(this BitArray bitArray)
		{
			var bytes = new byte[bitArray.Length / 8];
			for (var i = 0; i < bitArray.Length; i += 8)
			{
				byte b = 0x00;
				for (var y = 0; y < 8; y++)
					if (bitArray[i + y])
						b |= (byte)(1 << y);
				
				bytes[i == 0 ? 0 : (i / 8)] = b;
			}

			return bytes;
		}

		// public static int[] ToIntArray(this BitArray bitArray)
		// {
		// 	var ints = new int[bitArray.Length / 8];
		// 	for (var i = 0; i < bitArray.Length; i += 8)
		// 	{
		// 		byte b = 0x00;
		// 		for (var y = 0; y < 8; y++)
		// 			if (bitArray[i + y])
		// 				b |= (byte)(1 << y);
				
		// 		ints[i == 0 ? 0 : (i / 8)] = b;
		// 	}

		// 	return ints;
		// }
	}
}
