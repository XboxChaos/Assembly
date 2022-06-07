using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Blamite.RTE.Console.Native
{
	public static class PixelsToBitmap
	{
		public static Bitmap Convert(byte[] buffer, int pitch, int width, int height)
		{
			var ptr = new IntPtr();
			Marshal.FreeHGlobal(ptr);
			ptr = Marshal.AllocHGlobal(buffer.Length);
			RtlMoveMemory(ptr, buffer, buffer.Length);

			var final = new Bitmap(width, height, pitch,
				PixelFormat.Format32bppArgb, ptr);

			return final;
		}

		#region Native Functions
		[DllImport("kernel32.dll")]
		private static extern void RtlMoveMemory(IntPtr src, byte[] temp, int cb);

		[DllImport("gdi32")]
		private static extern int DeleteObject(IntPtr o);
		#endregion
	}
}
