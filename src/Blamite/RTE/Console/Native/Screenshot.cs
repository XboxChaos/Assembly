using Blamite.RTE.Console;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Blamite.RTE.Console.Native
{
	/// <summary>
	/// Container for data about a screenshot taken from a console.
	/// </summary>
	public class Screenshot
	{
		public int Pitch { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public uint Format { get; set; }

		//360 only:
		public int OffsetX { get; set; }
		public int OffsetY { get; set; }
		public int FrameBufferSize { get; set; }
		public int ScreenWidth { get; set; }
		public int ScreenHeight { get; set; }
		public int ColorSpace { get; set; }

		public byte[] Data { get; set; }

		public FormattedResponse OriginalResponse { get; set; }

		public Screenshot(FormattedResponse formatted)
		{
			OriginalResponse = formatted;

			var pitch = formatted.FindNumberValue("pitch");
			Pitch = pitch.HasValue ? (int)pitch.Value : 0;

			var width = formatted.FindNumberValue("width");
			Width = width.HasValue ? (int)width.Value : 0;

			var height = formatted.FindNumberValue("height");
			Height = height.HasValue ? (int)height.Value : 0;

			var format = formatted.FindNumberValue("format");
			Format = format.HasValue ? format.Value : 0;

			var offsetx = formatted.FindNumberValue("offsetx");
			OffsetX = offsetx.HasValue ? (int)offsetx.Value : 0;

			var offsety = formatted.FindNumberValue("offsety");
			OffsetY = offsety.HasValue ? (int)offsety.Value : 0;

			var buffersize = formatted.FindNumberValue("framebuffersize");
			FrameBufferSize = buffersize.HasValue ? (int)buffersize.Value : 0;

			var screenwidth = formatted.FindNumberValue("sw");
			ScreenWidth = screenwidth.HasValue ? (int)screenwidth.Value : -1;

			var screenheight = formatted.FindNumberValue("sh");
			ScreenHeight = screenheight.HasValue ? (int)screenheight.Value : -1;

			var colorspace = formatted.FindNumberValue("colorspace");
			ColorSpace = colorspace.HasValue ? (int)colorspace.Value : 0;
		}

		/// <summary>
		/// Converts the raw pixel data into a Bitmap.
		/// </summary>
		/// <param name="gamma">Apply gamma correction to the A2R10G10B10 format.</param>
		/// <param name="resize">Resize to the console's screen resolution, if available.</param>
		/// <param name="forceA2">Ignore the format value and force the format to be A2R10G10B10. Default is false.</param>
		/// <returns>The screenshot as a Bitmap.</returns>
		public Bitmap ConvertToBitmap(bool gamma, bool resize, bool forceA2 = false)
		{
			bool tiled = (Format & 0x100) > 0;
			int format = (int)(Format & 0x3F);

			int realWidth = Pitch / 4;
			byte[] data = Data;

			if (tiled)
				data = ConvertTiledToLinear(data, realWidth, Height);

			//are other formats used? I hope not!
			//OG xbox uses 0x1E which is plain little endian 8888 so no work is needed unlike 360

			if (format == 0x36 || forceA2)
				data = ConvertA2B10G10R10ToA8R8G8B8(data, realWidth, Height, gamma);
			else //0x6, 0x1E
				data = PostProcA8R8G8B8(data);

			Bitmap image = PixelsToBitmap(data, Pitch, Width, Height);

			if (resize && ScreenWidth != -1)
				return ResizeImage(image);
			else
				return image;
		}

		private Bitmap ResizeImage(Bitmap orig, int width = -1, int height = -1)
		{
			if (width == -1 || height == -1)
			{
				if (ScreenWidth == -1)
					return orig;

				width = ScreenWidth;
				height = ScreenHeight;
			}

			if (width == Width && height == Height)
				return orig;

			var newImg = new Bitmap(width, height, PixelFormat.Format32bppRgb);
			var destRect = new Rectangle(0, 0, width, height);

			using (Graphics g = Graphics.FromImage(newImg))
			{
				g.CompositingMode = CompositingMode.SourceCopy;
				g.CompositingQuality = CompositingQuality.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBilinear;
				g.PixelOffsetMode = PixelOffsetMode.None;

				using (ImageAttributes attr = new ImageAttributes())
				{
					attr.SetWrapMode(WrapMode.TileFlipXY);
					g.DrawImage(orig, destRect, 0, 0, width, height, GraphicsUnit.Pixel, attr);
				}

				g.DrawImage(orig, 0, 0, width, height);
			}

			orig.Dispose();
			return newImg;
		}

		private static Bitmap PixelsToBitmap(byte[] buffer, int pitch, int width, int height)
		{
			var ptr = new IntPtr();
			Marshal.FreeHGlobal(ptr);
			ptr = Marshal.AllocHGlobal(buffer.Length);
			RtlMoveMemory(ptr, buffer, buffer.Length);

			var final = new Bitmap(width, height, pitch,
				PixelFormat.Format32bppArgb, ptr);

			DeleteObject(ptr);
			return final;
		}

		#region Native Functions
		[DllImport("kernel32.dll")]
		private static extern void RtlMoveMemory(IntPtr src, byte[] temp, int cb);

		[DllImport("gdi32")]
		private static extern int DeleteObject(IntPtr o);
		#endregion

		private static byte[] ConvertA2B10G10R10ToA8R8G8B8(byte[] buffer, int width, int height, bool gamma)
		{
			for (int i = 0; i < buffer.Length; i += 4)
			{
				uint argb = BitConverter.ToUInt32(buffer, i);

				uint rExt = (argb & 0x3FF00000) >> 20;
				uint gExt = (argb & 0xFFC00) >> 10;
				uint bExt = argb & 0x3FF;

				buffer[i] = ConvertTo8Bit(bExt, 0x3FF, gamma);
				buffer[i + 1] = ConvertTo8Bit(gExt, 0x3FF, gamma);
				buffer[i + 2] = ConvertTo8Bit(rExt, 0x3FF, gamma);

				buffer[i + 3] = 255;
			}

			return buffer;
		}

		private static byte ConvertTo8Bit(uint color, int mask, bool useGamma)
		{
			double val = color / (double)mask;
			byte result = (byte)(val * 255d + 0.5d);

			if (useGamma)
				return GammaTable[result];

			return result;
		}

		private static byte[] PostProcA8R8G8B8(byte[] buffer)
		{
			for (int i = 0; i < buffer.Length; i += 4)
			{
				buffer[i + 3] = 255;
			}
			return buffer;
		}

		private static byte[] ConvertTiledToLinear(byte[] buffer, int width, int height)
		{
			byte[] destData = new byte[buffer.Length];

			//these are redundant but they actually depend on the format, which for screenshots doesn't change (yet?)
			int blockSizeRow = 1;
			int blockSizeColumn = 1;
			int texelPitch = 4;

			int blockWidth = width / blockSizeRow;
			int blockHeight = height / blockSizeColumn;

			for (int j = 0; j < blockHeight; j++)
				for (int i = 0; i < blockWidth; i++)
				{
					int srcOffset = j * blockWidth * texelPitch + i * texelPitch;
					int destOffset = XGAddress2DTiledOffset(i, j, blockWidth, texelPitch) << 2;
					Array.Copy(buffer, destOffset, destData, srcOffset, texelPitch);
				}

			return destData;
		}

		private static int XGAddress2DTiledOffset(int x, int y, int Width, int TexelPitch)
		{
			int AlignedWidth = (Width + 31) & ~31;

			int LogBpp = (TexelPitch >> 2) + ((TexelPitch >> 1) >> (TexelPitch >> 2));
			int Macro = ((x >> 5) + (y >> 5) * (AlignedWidth >> 5)) << (LogBpp + 7);
			int Micro = (((x & 7) + ((y & 6) << 2)) << LogBpp);
			int Offset = Macro + ((Micro & ~15) << 1) + (Micro & 15) + ((y & 8) << (3 + LogBpp)) + ((y & 1) << 4);

			return (((Offset & ~511) << 3) + ((Offset & 448) << 2) + (Offset & 63) +
					((y & 16) << 7) + (((((y & 8) >> 2) + (x >> 3)) & 3) << 6)) >> LogBpp;
		}

		private static byte[] GammaTable = new byte[]
		{
			0x00, 0x02, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0a, 0x0b, 0x0b, 0x0c, 0x0d, 0x0d, 0x0e, 0x0e,
			0x0e, 0x0f, 0x0f, 0x10, 0x11, 0x11, 0x12, 0x12, 0x12, 0x13, 0x13, 0x13, 0x14, 0x14, 0x14, 0x15,
			0x15, 0x15, 0x16, 0x16, 0x16, 0x17, 0x17, 0x17, 0x18, 0x18, 0x18, 0x19, 0x19, 0x19, 0x19, 0x1a,
			0x1a, 0x1a, 0x1a, 0x1b, 0x1b, 0x1b, 0x1b, 0x1c, 0x1c, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1e, 0x1e,
			0x1e, 0x1f, 0x1f, 0x20, 0x20, 0x20, 0x21, 0x21, 0x22, 0x22, 0x23, 0x23, 0x23, 0x24, 0x24, 0x25,
			0x25, 0x25, 0x26, 0x26, 0x27, 0x27, 0x27, 0x28, 0x28, 0x28, 0x29, 0x29, 0x2a, 0x2a, 0x2a, 0x2b,
			0x2b, 0x2c, 0x2c, 0x2d, 0x2e, 0x2e, 0x2f, 0x30, 0x30, 0x31, 0x32, 0x32, 0x33, 0x33, 0x34, 0x34,
			0x35, 0x36, 0x36, 0x37, 0x37, 0x38, 0x38, 0x39, 0x39, 0x3a, 0x3a, 0x3b, 0x3c, 0x3c, 0x3d, 0x3d,
			0x3e, 0x3e, 0x3f, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x47, 0x48, 0x49, 0x4a, 0x4b,
			0x4c, 0x4c, 0x4d, 0x4e, 0x4f, 0x50, 0x50, 0x51, 0x52, 0x53, 0x53, 0x54, 0x55, 0x56, 0x56, 0x57,
			0x58, 0x59, 0x5a, 0x5c, 0x5d, 0x5e, 0x60, 0x61, 0x62, 0x64, 0x65, 0x66, 0x67, 0x68, 0x6a, 0x6b,
			0x6c, 0x6d, 0x6e, 0x6f, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x7b, 0x7c,
			0x7d, 0x7f, 0x81, 0x83, 0x85, 0x87, 0x89, 0x8a, 0x8c, 0x8e, 0x90, 0x92, 0x93, 0x95, 0x97, 0x98,
			0x9a, 0x9c, 0x9d, 0x9f, 0xa1, 0xa2, 0xa4, 0xa5, 0xa7, 0xa8, 0xaa, 0xab, 0xad, 0xae, 0xb0, 0xb1,
			0xb3, 0xb5, 0xb8, 0xbb, 0xbe, 0xc0, 0xc3, 0xc6, 0xc8, 0xcb, 0xcd, 0xd0, 0xd2, 0xd5, 0xd7, 0xda,
			0xdc, 0xde, 0xe1, 0xe3, 0xe5, 0xe7, 0xea, 0xec, 0xee, 0xf0, 0xf2, 0xf4, 0xf7, 0xf9, 0xfb, 0xfd,
		};

	}
}
