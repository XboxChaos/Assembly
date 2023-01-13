/*
 * Thanks to -DeToX-/XeNoN (Xenomega) for the original code.
 * Adapted from the Ascension Source for Blamite/Assembly
 */
using System;

namespace Blamite.RTE.Console.Helpers
{
	public static class ImageTools
	{
		public static byte[] ConvertA2R10G10B10AS16ToA8R8G8B8(byte[] buffer, int width, int height)
		{
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

					buffer[index++] = sRGBGammaTable[b];
					buffer[index++] = sRGBGammaTable[g];
					buffer[index++] = sRGBGammaTable[r];
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

		private static byte[] sRGBGammaTable = new byte[]
		{
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
			0x01, 0x01, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
			0x04, 0x04, 0x04, 0x04, 0x04, 0x05, 0x05, 0x05, 0x05, 0x06, 0x06, 0x06, 0x06, 0x07, 0x07, 0x07,
			0x08, 0x08, 0x08, 0x08, 0x09, 0x09, 0x09, 0x0A, 0x0A, 0x0A, 0x0B, 0x0B, 0x0C, 0x0C, 0x0C, 0x0D,
			0x0D, 0x0D, 0x0E, 0x0E, 0x0F, 0x0F, 0x10, 0x10, 0x11, 0x11, 0x11, 0x12, 0x12, 0x13, 0x13, 0x14,
			0x14, 0x15, 0x16, 0x16, 0x17, 0x17, 0x18, 0x18, 0x19, 0x19, 0x1A, 0x1B, 0x1B, 0x1C, 0x1D, 0x1D,
			0x1E, 0x1E, 0x1F, 0x20, 0x20, 0x21, 0x22, 0x23, 0x23, 0x24, 0x25, 0x25, 0x26, 0x27, 0x28, 0x29,
			0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2D, 0x2E, 0x2F, 0x30, 0x31, 0x32, 0x33, 0x33, 0x34, 0x35, 0x36,
			0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46,
			0x47, 0x48, 0x49, 0x4A, 0x4C, 0x4D, 0x4E, 0x4F, 0x50, 0x51, 0x52, 0x54, 0x55, 0x56, 0x57, 0x58,
			0x5A, 0x5B, 0x5C, 0x5D, 0x5F, 0x60, 0x61, 0x63, 0x64, 0x65, 0x67, 0x68, 0x69, 0x6B, 0x6C, 0x6D,
			0x6F, 0x70, 0x72, 0x73, 0x74, 0x76, 0x77, 0x79, 0x7A, 0x7C, 0x7D, 0x7F, 0x80, 0x82, 0x83, 0x85,
			0x86, 0x88, 0x8A, 0x8B, 0x8D, 0x8E, 0x90, 0x92, 0x93, 0x95, 0x97, 0x98, 0x9A, 0x9C, 0x9D, 0x9F,
			0xA1, 0xA3, 0xA4, 0xA6, 0xA8, 0xAA, 0xAB, 0xAD, 0xAF, 0xB1, 0xB3, 0xB5, 0xB7, 0xB8, 0xBA, 0xBC,
			0xBE, 0xC0, 0xC2, 0xC4, 0xC6, 0xC8, 0xCA, 0xCC, 0xCE, 0xD0, 0xD2, 0xD4, 0xD6, 0xD8, 0xDA, 0xDC,
			0xDE, 0xE0, 0xE2, 0xE5, 0xE7, 0xE9, 0xEB, 0xED, 0xEF, 0xF2, 0xF4, 0xF6, 0xF8, 0xFA, 0xFD, 0xFF
		};

	}

}
