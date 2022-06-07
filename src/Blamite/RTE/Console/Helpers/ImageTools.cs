/*
 * Thanks to -DeToX-/XeNoN (Xenomega) for the original code.
 * Adapted from the Ascension Source for Blamite/Assembly
 */
using System;

namespace Blamite.RTE.Console.Helpers
{
	public static class ImageTools
	{
		public static byte[] ConvertA2R10G10B10ToA8R8G8B8(byte[] buffer, int width, int height)
		{
			//build gamma table
			double gammaVal = 2;
			var table = new byte[0x100];
			for (int x = 0; x < 0x100; x++)
			{
				table[x] = (byte)Math.Min(0xff,
					(int)((Math.Pow(x / 255.0, gammaVal) * 255.0) + 0.5));
			}

			//Loop through and convert each pixel
			int index = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					uint argb = BitConverter.ToUInt32(buffer, index);

					uint rExt = (argb & 0x3FF00000) >> 20;
					uint gExt = (argb & 0xFFC00) >> 10;
					uint bExt = argb & 0x3FF;

					byte r = (byte)(rExt * 255 / 1023);
					byte g = (byte)(gExt * 255 / 1023);
					byte b = (byte)(bExt * 255 / 1023);

					buffer[index++] = table[b];
					buffer[index++] = table[g];
					buffer[index++] = table[r];
					buffer[index++] = 255; //Set the alpha to 255
				}
			}

			return buffer;
		}

		public static byte[] FlipA8R8G8B8(byte[] buffer, int width, int height)
		{
			//Loop through and convert each pixel
			int index = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					BitConverter.ToUInt32(buffer, index);

					uint b = buffer[index];
					uint g = buffer[index + 1];
					uint r = buffer[index + 2];

					buffer[index++] = (byte)b;
					buffer[index++] = (byte)g;
					buffer[index++] = (byte)r;
					buffer[index++] = 255; //Set the alpha as 255
				}
			}

			return buffer;
		}

		public static byte[] PostProcA8R8G8B8(byte[] buffer)
		{
			for (int i = 0; i < buffer.Length; i += 4)
			{
				buffer[i + 3] = 255;
			}
			return buffer;
		}

		public static byte[] ConvertTiledToLinear(byte[] buffer, int width, int height)
		{
			byte[] destData = new byte[buffer.Length];

			int blockSizeRow = 1;
			int blockSizeColumn = 1;
			int texelPitch = 4;

			if (width % 4 > 0)
				width = width + 4 & 0xFFFFFFC;
			if (height % 4 > 0)
				height = height + 4 & 0xFFFFFFC;

			// Figure out our block height and width
			int blockWidth = width / blockSizeRow;
			int blockHeight = height / blockSizeColumn + (texelPitch * texelPitch);

			// Loop through the height and width and copy our data
			try
			{
				for (int j = 0; j < blockHeight; j++)
					for (int i = 0; i < blockWidth; i++)
					{
						int blockOffset = j * blockWidth + i;

						int x = XGAddress2DTiledX(blockOffset, blockWidth, texelPitch);
						int y = XGAddress2DTiledY(blockOffset, blockWidth, texelPitch);

						int srcOffset = j * blockWidth * texelPitch + i * texelPitch;
						int destOffset = y * blockWidth * texelPitch + x * texelPitch;

						Array.Copy(buffer, srcOffset, destData, destOffset, texelPitch);
					}
			}
			catch
			{ }

			return destData;
		}

		private static int XGAddress2DTiledX(int Offset, int Width, int TexelPitch)
		{
			int AlignedWidth = (Width + 31) & ~31;

			int LogBpp = (TexelPitch >> 2) + ((TexelPitch >> 1) >> (TexelPitch >> 2));
			int OffsetB = Offset << LogBpp;
			int OffsetT = ((OffsetB & ~4095) >> 3) + ((OffsetB & 1792) >> 2) + (OffsetB & 63);
			int OffsetM = OffsetT >> (7 + LogBpp);

			int MacroX = ((OffsetM % (AlignedWidth >> 5)) << 2);
			int Tile = ((((OffsetT >> (5 + LogBpp)) & 2) + (OffsetB >> 6)) & 3);
			int Macro = (MacroX + Tile) << 3;
			int Micro = ((((OffsetT >> 1) & ~15) + (OffsetT & 15)) & ((TexelPitch << 3) - 1)) >> LogBpp;

			return Macro + Micro;
		}

		private static int XGAddress2DTiledY(int Offset, int Width, int TexelPitch)
		{
			int AlignedWidth = (Width + 31) & ~31;

			int LogBpp = (TexelPitch >> 2) + ((TexelPitch >> 1) >> (TexelPitch >> 2));
			int OffsetB = Offset << LogBpp;
			int OffsetT = ((OffsetB & ~4095) >> 3) + ((OffsetB & 1792) >> 2) + (OffsetB & 63);
			int OffsetM = OffsetT >> (7 + LogBpp);

			int MacroY = ((OffsetM / (AlignedWidth >> 5)) << 2);
			int Tile = ((OffsetT >> (6 + LogBpp)) & 1) + (((OffsetB & 2048) >> 10));
			int Macro = (MacroY + Tile) << 3;
			int Micro = ((((OffsetT & (((TexelPitch << 6) - 1) & ~31)) + ((OffsetT & 15) << 1)) >> (3 + LogBpp)) & ~1);

			return Macro + Micro + ((OffsetT & 16) >> 4);
		}

	}

}
