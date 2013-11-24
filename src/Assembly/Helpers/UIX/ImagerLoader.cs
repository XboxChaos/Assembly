using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Assembly.Helpers.UIX
{
	public class ImageLoader
	{
		public event EventHandler Loaded;
		public event EventHandler<ExceptionEventArgs> LoadFailed;

		public void LoadImage(Image imageControl, Uri imageUri)
		{
			var bitmap = new BitmapImage();
			bitmap.BeginInit();
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.DownloadCompleted += (sender, e) => DownloadCompleted(sender, e, imageControl, bitmap);
			bitmap.DownloadFailed += DownloadFailed;
			bitmap.DecodeFailed += DownloadFailed;
			try
			{
				bitmap.UriSource = imageUri;
				bitmap.EndInit();
			}
			catch
			{
				DownloadFailed(this, null);
			}
			if (bitmap.IsDownloading || Loaded == null) return;

			imageControl.Source = bitmap;
			Loaded(this, EventArgs.Empty);
		}

		public static void LoadImageAndFade(Image imageControl, Uri imageUri, AnimationHelper animation)
		{
			var loader = new ImageLoader();
			loader.Loaded += (sender, e) => animation.FadeIn(imageControl);
			loader.LoadFailed += (obj, args) => animation.FadeIn(imageControl);
			loader.LoadImage(imageControl, imageUri);
		}

		private void DownloadCompleted(object sender, EventArgs e, Image imageControl, BitmapImage bitmap)
		{
			imageControl.Source = bitmap;
			if (Loaded != null)
				Loaded(this, e);
		}

		private void DownloadFailed(object sender, ExceptionEventArgs e)
		{
			if (LoadFailed != null)
				LoadFailed(this, e);
		}
	}
}