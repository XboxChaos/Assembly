/* Copyright 2010 - -DeToX-/XeNoN (Xenomega).
 * 
 * This file is attapted from Ascention.
 * 
 * Thanks to DeToX for the origial deswizzling, gamma and resizing code. Adapted for Assembly.
 * yolo
 */

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Blamite.IO;

namespace Assembly.Helpers
{
	public static class DDSConversion
	{
		public static unsafe void GammaCorrect(double gamma, BitmapData imageData)
		{
			gamma = Math.Max(0.1, Math.Min(5.0, gamma));
			double y = 1.0/gamma;
			var table = new byte[0x100];
			for (int x = 0; x < 0x100; x++)
			{
				table[x] = (byte) Math.Min(0xff,
					(int) ((Math.Pow((x)/255.0, y)*255.0) + 0.5));
			}

			int width = imageData.Width;
			int height = imageData.Height;
			int num3 = width*((imageData.PixelFormat == PixelFormat.Format8bppIndexed) ? 1 : 3);
			int num4 = imageData.Stride - num3;
			var numPtr = (byte*) imageData.Scan0.ToPointer();
			for (int i = 0; i < height; i++)
			{
				int num6 = 0;
				while (num6 < num3)
				{
					numPtr[0] = table[numPtr[0]];
					num6++;
					numPtr++;
				}
				numPtr += num4;
			}
		}

		public static Bitmap ResizeImage(Bitmap Orig, int width = -1, int height = -1)
		{
			// store
			if (width == -1 || height == -1)
			{
				width = App.AssemblyStorage.AssemblySettings.XdkResizeScreenshotWidth;
				height = App.AssemblyStorage.AssemblySettings.XdkResizeScreenshotHeight;
			}

			// Create a new image
			var newImg = new Bitmap(width, height);

			// Draw our new image
			using (Graphics g = Graphics.FromImage(newImg))
				g.DrawImage(Orig, 0, 0, width, height);

			// Now return our image
			return newImg;
		}

		public static BitmapSource Deswizzle(string FilePath, int resizeWidth = -1, int resizeHeight = -1,
			bool onlyResizeIfGreater = false, bool dontSettingsResize = false)
		{
			//Open the temp dds
			var fs = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite);
			var es = new EndianStream(fs, Endian.LittleEndian);

			//Read the dds header
			es.SeekTo(0x0C);
			int height = es.ReadInt32();
			int width = es.ReadInt32();

			//Read our random bytes
			es.SeekTo(0x5C);
			string randomBuf = BitConverter.ToString(es.ReadBlock(12)).Replace("-", "");

			//Read the buffer
			es.SeekTo(0x80);
			int size = width*height*4;
			byte[] buffer = es.ReadBlock(size);
			es.Dispose();

			Bitmap bitmap = null;

			//A2R10G10B10
			switch (randomBuf)
			{
				case "FF03000000FC0F000000F03F":
					bitmap = DeswizzleA2R10G10B10(buffer, width, height);
					if (App.AssemblyStorage.AssemblySettings.XdkScreenshotGammaCorrect)
					{
						BitmapData imageData = (bitmap).LockBits(
							new Rectangle(0, 0, bitmap.Width, bitmap.Height),
							ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
						GammaCorrect(App.AssemblyStorage.AssemblySettings.XdkScreenshotGammaModifier, imageData);
						bitmap.UnlockBits(imageData);
					}
					break;
				case "0000FF0000FF0000FF000000":
					bitmap = DeswizzleA8R8G8B8(buffer, width, height);
					break;
			}

			if (bitmap == null)
				return null;

			// Resize
			if (App.AssemblyStorage.AssemblySettings.XdkResizeImages && !dontSettingsResize)
				bitmap = ResizeImage(bitmap);

			return loadBitmap(bitmap);
		}

		private static Bitmap DeswizzleA2R10G10B10(byte[] buffer, int width, int height)
		{
			//Loop through and convert each pixle
			int index = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					uint argb = BitConverter.ToUInt32(buffer, index);

					uint r = ((argb & 0x3FF00000) >> 22) /*<< 16*/;
					uint g = ((argb & 0xFFC00) >> 12) /*<< 8*/;
					uint b = (argb & 0x3FF) >> 2;
					//uint final = a | r | g | b;

					buffer[index++] = (byte) b;
					buffer[index++] = (byte) g;
					buffer[index++] = (byte) r;
					buffer[index++] = 255; //Set the alpha as 255
				}
			}

			//Now create a image from the buffer
			var ptr = new IntPtr();
			Marshal.FreeHGlobal(ptr);
			ptr = Marshal.AllocHGlobal(buffer.Length);
			RtlMoveMemory(ptr, buffer, buffer.Length);

			//Create the final image
			var final = new Bitmap(width, height, width*4,
				PixelFormat.Format32bppArgb, ptr);

			//Return our done image
			return final;
		}

		private static Bitmap DeswizzleA8R8G8B8(byte[] buffer, int width, int height)
		{
			//Loop through and convert each pixle
			int index = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					BitConverter.ToUInt32(buffer, index);

					uint b = buffer[index];
					uint g = buffer[index + 1];
					uint r = buffer[index + 2];

					buffer[index++] = (byte) b;
					buffer[index++] = (byte) g;
					buffer[index++] = (byte) r;
					buffer[index++] = 255; //Set the alpha as 255
				}
			}

			//Now create a image from the buffer
			var ptr = new IntPtr();
			Marshal.FreeHGlobal(ptr);
			ptr = Marshal.AllocHGlobal(buffer.Length);
			RtlMoveMemory(ptr, buffer, buffer.Length);

			//Create the final image
			var final = new Bitmap(width, height, width*4,
				PixelFormat.Format32bppArgb, ptr);

			//Return our done image
			return final;
		}

		[DllImport("kernel32.dll")]
		private static extern void RtlMoveMemory(IntPtr src, byte[] temp, int cb);

		[DllImport("gdi32")]
		private static extern int DeleteObject(IntPtr o);

		public static BitmapSource loadBitmap(Bitmap source)
		{
			IntPtr ip = source.GetHbitmap();
			BitmapSource bs;
			try
			{
				bs = Imaging.CreateBitmapSourceFromHBitmap(ip,
					IntPtr.Zero, Int32Rect.Empty,
					BitmapSizeOptions.FromEmptyOptions());
			}
			finally
			{
				DeleteObject(ip);
			}

			return bs;
		}
	}
}