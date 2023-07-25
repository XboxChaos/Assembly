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

		public Screenshot(FormattedResponse formatted)
		{
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

		public Bitmap ConvertToBitmap(bool resize)
		{
			//convert pixels according to the format
			bool tiled = (Format & 0x100) > 0;
			int format = (int)(Format & 0x3F);

			int realWidth = Pitch / 4;
			byte[] data = Data;

			if (tiled)
				data = ConvertTiledToLinear(data, realWidth, Height);

			if (format == 0x6)
				data = FlipA8R8G8B8(data, realWidth, Height);
			else if (format == 0x36)
				data = ConvertA2R10G10B10AS16ToA8R8G8B8(data, realWidth, Height);

			//are other formats used? I hope not!
			//OG xbox uses 0x1E which is plain little endian 8888 so no work is needed unlike 360

			data = PostProcA8R8G8B8(data);

			Bitmap image = PixelsToBitmap(data, Pitch, Width, Height);

			//cant resize to screen res if we dont have that info
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

			return newImg;
		}

		public static Bitmap PixelsToBitmap(byte[] buffer, int pitch, int width, int height)
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


					buffer[index++] = Convert10BitColor(bExt, 0x3FF, 2.2);
					buffer[index++] = Convert10BitColor(gExt, 0x3FF, 2.2);
					buffer[index++] = Convert10BitColor(rExt, 0x3FF, 2.2);
					buffer[index++] = 255; //Set the alpha to 255
				}
			}

			return buffer;
		}

		private static byte Convert10BitColor(uint col, int mask, double gamma)
		{
			double val = Math.Pow((double)col / (double)mask, gamma);
			return (byte)(val * 255d + 0.5d);
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
				buffer[i + 3] = 255; //Set the alpha as 255
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
