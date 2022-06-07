using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Assembly.Helpers.Native
{
	public static class ScreenshotHelper
	{
		public static BitmapSource CreateBitmapSource(Bitmap bitmap)
		{
			if (bitmap == null)
				return null;

			IntPtr bp = bitmap.GetHbitmap();
			BitmapSource bs;
			try
			{
				bs = Imaging.CreateBitmapSourceFromHBitmap(bp,
					IntPtr.Zero, Int32Rect.Empty,
					BitmapSizeOptions.FromEmptyOptions());
			}
			finally
			{
				DeleteObject(bp);
			}

			return bs;
		}

		#region Native Functions
		[DllImport("gdi32")]
		private static extern int DeleteObject(IntPtr o);
		#endregion
	}
}