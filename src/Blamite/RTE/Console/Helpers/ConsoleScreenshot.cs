using Blamite.RTE.Console.Native;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Blamite.RTE.Console.Helpers
{
	/// <summary>
	/// Container for information about a screenshot taken from a console.
	/// </summary>
	public class ConsoleScreenshot
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

		public ConsoleScreenshot(ConsoleFormattedResponse formatted)
		{
			var pitch = formatted.FindNumberValue("pitch");
			Pitch = pitch.HasValue ? (int)pitch.Value : -1;

			var width = formatted.FindNumberValue("width");
			Width = width.HasValue ? (int)width.Value : -1;

			var height = formatted.FindNumberValue("height");
			Height = height.HasValue ? (int)height.Value : -1;

			var format = formatted.FindNumberValue("format");
			Format = format.HasValue ? format.Value : 0;

			var offsetx = formatted.FindNumberValue("offsetx");
			OffsetX = offsetx.HasValue ? (int)offsetx.Value : -1;

			var offsety = formatted.FindNumberValue("offsety");
			OffsetY = offsety.HasValue ? (int)offsety.Value : -1;

			var buffersize = formatted.FindNumberValue("framebuffersize");
			FrameBufferSize = buffersize.HasValue ? (int)buffersize.Value : -1;

			var screenwidth = formatted.FindNumberValue("sw");
			ScreenWidth = screenwidth.HasValue ? (int)screenwidth.Value : -1;

			var screenheight = formatted.FindNumberValue("sh");
			ScreenHeight = screenheight.HasValue ? (int)screenheight.Value : -1;

			var colorspace = formatted.FindNumberValue("colorspace");
			ColorSpace = colorspace.HasValue ? (int)colorspace.Value : -1;
		}

		public Bitmap ConvertToBitmap(bool resize)
		{
			//convert pixels according to the format
			uint format = Format & 0xFFF;
			int realWidth = Pitch / 4;
			byte[] data = Data;

			if ((format & 0x100) > 0)
			{
				data = ImageTools.ConvertTiledToLinear(data, realWidth, Height);

				if (format == 0x1B6)
				{
					data = ImageTools.ConvertA2R10G10B10ToA8R8G8B8(data, realWidth, Height);
				}
				else if (format == 0x186)
				{
					data = ImageTools.FlipA8R8G8B8(data, realWidth, Height);
				}
				else
				{
					//do nothing? og uses 0x1E that doesn't require any postproc
				}
			}
			else
				data = ImageTools.PostProcA8R8G8B8(data);

			Bitmap image = PixelsToBitmap.Convert(data, Pitch, Width, Height);

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
				width = ScreenWidth;
				height = ScreenHeight;
			}

			if (width == Width && height == Height)
				return orig;

			var newImg = new Bitmap(width, height);
			var destRect = new Rectangle(0, 0, width, height);

			using (Graphics g = Graphics.FromImage(newImg))
			{
				g.CompositingMode = CompositingMode.SourceCopy;
				g.CompositingQuality = CompositingQuality.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;

				using (ImageAttributes attr = new ImageAttributes())
				{
					attr.SetWrapMode(WrapMode.TileFlipXY);
					g.DrawImage(orig, destRect, 0, 0, width, height, GraphicsUnit.Pixel, attr);
				}

				g.DrawImage(orig, 0, 0, width, height);
			}

			return newImg;
		}

	}
}
