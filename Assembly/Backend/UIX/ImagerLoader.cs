using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Assembly.Backend.UIX
{
    public class ImageLoader
    {
        public event EventHandler Loaded;
        public event EventHandler<ExceptionEventArgs> LoadFailed;

        public void LoadImage(Image imageControl, Uri imageUri)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DownloadCompleted += new EventHandler((sender, e) => DownloadCompleted(sender, e, imageControl, bitmap));
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
            if (!bitmap.IsDownloading && Loaded != null)
            {
                imageControl.Source = bitmap;
                Loaded(this, EventArgs.Empty);
            }
        }

        public static void LoadImageAndFade(Image imageControl, Uri imageUri, AnimationHelper animation)
        {
            ImageLoader loader = new ImageLoader();
            loader.Loaded += new EventHandler((object sender, EventArgs e) =>
            {
                animation.FadeIn(imageControl);
            });
            loader.LoadFailed += new EventHandler<ExceptionEventArgs>((object obj, ExceptionEventArgs args) =>
            {
                animation.FadeIn(imageControl);
            });
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
