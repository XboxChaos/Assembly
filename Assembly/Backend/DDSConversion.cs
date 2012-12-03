/* Copyright 2010 - -DeToX-/XeNoN (Xenomega).
 * 
 * This file is attapted from Ascention.
 * 
 * Thanks to DeToX for the origial deswizzling, gamma and resizing code. Adapted for Assembly.
 * yolo
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using ExtryzeDLL.IO;

namespace Assembly.Backend
{
    public class DDSConversion
    {
        public static unsafe void GammaCorrect(double gamma, BitmapData imageData)
        {
            gamma = Math.Max(0.1, Math.Min(5.0, gamma));
            double y = 1.0 / gamma;
            byte[] table = new byte[0x100]; 
            for (int x = 0; x < 0x100; x++)
            {
                table[x] = (byte)Math.Min(0xff,
                    (int)((Math.Pow(((double)x) / 255.0, y) * 255.0) + 0.5));
            }

            int width = imageData.Width;
            int height = imageData.Height;
            int num3 = width * ((imageData.PixelFormat == PixelFormat.Format8bppIndexed) ? 1 : 3);
            int num4 = imageData.Stride - num3;
            byte* numPtr = (byte*)imageData.Scan0.ToPointer();
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
        public static Bitmap ResizeImage(Bitmap Orig)
        {
            // Create a new image
            Bitmap newImg = new Bitmap(Settings.XDKResizeScreenshotWidth,
                Settings.XDKResizeScreenshotHeight);

            // Draw our new image
            using (Graphics g = Graphics.FromImage((Image)newImg))
                g.DrawImage(Orig, 0, 0, Settings.XDKResizeScreenshotWidth,
                    Settings.XDKResizeScreenshotHeight);

            // Now return our image
            return newImg;
        }

        public static BitmapSource Deswizzle(string FilePath)
        {
            //Open the temp dds
            FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
            EndianStream es = new EndianStream(fs, Endian.LittleEndian);

            //Read the dds header
            es.SeekTo(0x0C);
            int height = es.ReadInt32();
            int width = es.ReadInt32();

            //Read our random bytes
            es.SeekTo(0x5C);
            string randomBuf = BitConverter.ToString(es.ReadBlock(12)).Replace("-", "");

            //Read the buffer
            es.SeekTo(0x80);
            int size = width * height * 4;
            byte[] buffer = es.ReadBlock(size);
            es.Close();

            Bitmap bitmap = null;

            //A2R10G10B10
            if (randomBuf == "FF03000000FC0F000000F03F")
            {
                bitmap = DeswizzleA2R10G10B10(buffer, width, height);

                // Adjust Gamma (Halo is much lighter for some reason..)
                if (Settings.XDKScreenshotGammaCorrect)
                {
                    BitmapData imageData = (bitmap).LockBits(
                        new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                    GammaCorrect(Settings.XDKScreenshotGammaModifier, imageData);
                    bitmap.UnlockBits(imageData);
                }
            }
            //A8R8G8B8?
            else if (randomBuf == "0000FF0000FF0000FF000000")
                bitmap = DeswizzleA8R8G8B8(buffer, width, height);

            if (bitmap != null)
            {
                // Resize
                if (Settings.XDKResizeImages)
                    bitmap = ResizeImage(bitmap);

                return loadBitmap(bitmap);
            }
            else
                return null;
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

                    buffer[index++] = (byte)b;
                    buffer[index++] = (byte)g;
                    buffer[index++] = (byte)r;
                    buffer[index++] = 255; //Set the alpha as 255
                }
            }

            //Now create a image from the buffer
            IntPtr ptr = new IntPtr();
            Marshal.FreeHGlobal(ptr);
            ptr = Marshal.AllocHGlobal(buffer.Length);
            RtlMoveMemory(ptr, buffer, buffer.Length);

            //Create the final image
            Bitmap final = new Bitmap(width, height, width * 4,
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
                    uint argb = BitConverter.ToUInt32(buffer, index);

                    uint b = buffer[index];
                    uint g = buffer[index + 1];
                    uint r = buffer[index + 2];

                    buffer[index++] = (byte)b;
                    buffer[index++] = (byte)g;
                    buffer[index++] = (byte)r;
                    buffer[index++] = (byte)255; //Set the alpha as 255
                }
            }

            //Now create a image from the buffer
            IntPtr ptr = new IntPtr();
            Marshal.FreeHGlobal(ptr);
            ptr = Marshal.AllocHGlobal(buffer.Length);
            RtlMoveMemory(ptr, buffer, buffer.Length);

            //Create the final image
            Bitmap final = new Bitmap(width, height, width * 4,
                                      PixelFormat.Format32bppArgb, ptr);

            //Return our done image
            return final;
        }

        [DllImport("kernel32.dll")]
        private static extern void RtlMoveMemory(IntPtr src, byte[] temp, int cb);
        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);
        public static BitmapSource loadBitmap(System.Drawing.Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs = null;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }
    }
}
