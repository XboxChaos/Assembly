using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Assembly.Helpers;
using Assembly.Helpers.Native;
using Assembly.Helpers.Net;
using Assembly.Metro.Dialogs;
using Blamite.RTE.Console.Native;
using Xceed.Wpf.AvalonDock.Layout;
using Clipboard = System.Windows.Clipboard;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
	/// <summary>
	///     Interaction logic for HaloScreenshot.xaml
	/// </summary>
	public partial class HaloScreenshot
	{
		private readonly BitmapSource _bitmapImage;
		private readonly string _datetime_long;
		private readonly string _datetime_shrt;
		private string _imageID;

		public HaloScreenshot(Screenshot shot, LayoutDocument tabItem, string source)
		{
			InitializeComponent();

			bool resize = App.AssemblyStorage.AssemblySettings.XdkResizeImages;

			using (Bitmap bitm = shot.ConvertToBitmap(App.AssemblyStorage.AssemblySettings.XdkScreenshotGammaCorrect, shot.ScreenWidth != -1 ? resize : false))
			{
				_bitmapImage = ScreenshotHelper.CreateBitmapSource(bitm);
			}

			// DateTime Creation
			DateTime date = DateTime.Now;
			_datetime_long = date.ToString("yyyy-MM-dd,hh-mm-ss");
			_datetime_shrt = date.ToString("hh:mm.ss");

			// Set Tab Header
			tabItem.Title = source + " Screenshot {" + _datetime_shrt + "}";

			// Set Image Name
			lblImageName.Text = _datetime_long + ".png";

			// Set Image
			imageScreenshot.Source = _bitmapImage;

			// Should I save the image?
			if (!App.AssemblyStorage.AssemblySettings.XdkAutoSave) return;

			if (!Directory.Exists(App.AssemblyStorage.AssemblySettings.XdkScreenshotPath))
				Directory.CreateDirectory(App.AssemblyStorage.AssemblySettings.XdkScreenshotPath);

			string filePath = App.AssemblyStorage.AssemblySettings.XdkScreenshotPath + "\\" + _datetime_long + ".png";
			SaveImage(filePath);
		}

		private void SaveImage(string filePath)
		{
			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create((BitmapSource) imageScreenshot.Source));
			using (var stream = new FileStream(filePath, FileMode.Create))
				encoder.Save(stream);
		}

		/// <summary>
		///     Close stuff
		/// </summary>
		public bool Close()
		{
			return true;
		}

		private void btnSaveImg_Click(object sender, RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog
			{
				Title = "Select where do you want to save the Screenshot",
				Filter = "PNG Image (*.png)|*.png"
			};
			if (sfd.ShowDialog() == DialogResult.OK)
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

		private void imageUpload_DoWork(object sender, DoWorkEventArgs e)
		{
			var filePath = Path.GetTempFileName();
			try
			{
				Dispatcher.Invoke(new Action(delegate
				{
					var encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create((BitmapSource) imageScreenshot.Source));
					using (var stream = new FileStream(filePath, FileMode.Create))
						encoder.Save(stream);
				}));

				string newImageId = Imgur.UploadToImgur(File.ReadAllBytes(filePath));
				if (newImageId == null)
					new Exception("Failed to Upload.");

				Dispatcher.Invoke(new Action(delegate { _imageID = newImageId; }));
			}
			catch
			{
				Dispatcher.Invoke(new Action(
					() =>
						MetroMessageBox.Show("Error", "Unable to upload Image to server. Try again later.")));
			}
			finally
			{
				File.Delete(filePath);
			}
		}

		private void imageUpload_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			doingAction.Visibility = Visibility.Collapsed;

			if (_imageID == null || (_imageID.Length < 5 && _imageID.Length > 9))
			{
				MetroMessageBox.Show("Error", _imageID ?? "Error uploading image.");

				_imageID = null;
			}
			else
			{
				MetroImgurUpload.Show(_imageID);

				string _thumburl = string.Format("http://i.imgur.com/{0}b.jpg", _imageID);
				string _url = string.Format("http://i.imgur.com/{0}.jpg", _imageID);

				Dispatcher.Invoke(new Action(delegate
				{
					ImgurHistory.AddNewEntry(_datetime_long, _thumburl, _url);
				}));
			}
		}
	}
}