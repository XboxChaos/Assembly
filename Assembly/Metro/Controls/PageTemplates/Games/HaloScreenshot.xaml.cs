using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Assembly.Helpers;
using System.ComponentModel;
using Assembly.Metro.Dialogs;
using Assembly.Helpers.Net;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
    /// <summary>
    /// Interaction logic for HaloScreenshot.xaml
    /// </summary>
    public partial class HaloScreenshot
    {
        private readonly string _datetime_long;
        private readonly string _datetime_shrt;
	    private readonly BitmapSource _bitmapImage;
        private string _imageID;

        public HaloScreenshot(string tempImageLocation, TabItem tabItem)
        {
            InitializeComponent();

            // Convert DDS to BitmapImage
			_bitmapImage = DDSConversion.Deswizzle(tempImageLocation);

            // DateTime Creation
			var date = DateTime.Now;
            _datetime_long = date.ToString("yyyy-MM-dd,hh-mm-ss");
            _datetime_shrt = date.ToString("hh:mm.ss");

            // Set Tab Header
            tabItem.Header = "Screenshot {" + _datetime_shrt + "}";

            // Set Image Name
            lblImageName.Text = _datetime_long + ".png";

            // Set Image
            imageScreenshot.Source = _bitmapImage;

            // Should I save the image?
	        if (!Settings.XDKAutoSave) return;

	        if (!Directory.Exists(Settings.XDKScreenshotPath))
		        Directory.CreateDirectory(Settings.XDKScreenshotPath);

			var filePath = Settings.XDKScreenshotPath + "\\" + _datetime_long + ".png";
	        SaveImage(filePath);
        }

        private void SaveImage(string filePath)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imageScreenshot.Source));
			using (var stream = new FileStream(filePath, FileMode.Create))
                encoder.Save(stream);
        }

        /// <summary>
        /// Close stuff
        /// </summary>
        public bool Close() { return true; }

        private void btnSaveImg_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new System.Windows.Forms.SaveFileDialog
	                      {
		                      Title = "Select where do you want to save the Screenshot", 
							  Filter = "PNG Image (*.png)|*.png"
	                      };
	        if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                SaveImage(sfd.FileName);
        }
        private void btnUploadImage_Click(object sender, RoutedEventArgs e)
        {
            if (_imageID == null)
            {
                doingAction.Visibility = Visibility.Visible;

                var imageUpload = new BackgroundWorker();
                imageUpload.RunWorkerCompleted += imageUpload_RunWorkerCompleted;
                imageUpload.DoWork += imageUpload_DoWork;
                imageUpload.RunWorkerAsync();
            }
            else
                MetroImgurUpload.Show(_imageID);
        }
		private void btnClipboardImage_Click(object sender, RoutedEventArgs e)
		{
			Clipboard.SetImage(_bitmapImage);
		}

        void imageUpload_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var filePath = VariousFunctions.CreateTemporaryFile(VariousFunctions.GetTemporaryImageLocation());
                Dispatcher.Invoke(new Action(delegate
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imageScreenshot.Source));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        encoder.Save(stream);
                }));

	            var newImageId = Imgur.UploadToImgur(File.ReadAllBytes(filePath)).Result;
				if (newImageId == null)
					new Exception("Failed to Upload.");

                Dispatcher.Invoke(new Action(delegate
                {
                    _imageID = newImageId;
                }));
            }
            catch
            {
                Dispatcher.Invoke(new Action(
	                                  () =>
	                                  MetroMessageBox.Show("Error", "Unable to upload Image to server. Try again later.")));
            }
        }
        void imageUpload_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            doingAction.Visibility = Visibility.Collapsed;

            if (_imageID == null || (_imageID.Length < 5 && _imageID.Length > 9))
            {
	            MetroMessageBox.Show("Error", _imageID ?? "Error uploading image.");

	            _imageID = null;
            }
            else
                MetroImgurUpload.Show(_imageID);
        }
    }
}
