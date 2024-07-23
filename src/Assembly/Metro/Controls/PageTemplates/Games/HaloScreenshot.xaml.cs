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
		private BitmapSource _bitmapImage;
		private readonly string _datetime_long;
		private readonly string _datetime_shrt;
		private string _imageID;
		private readonly Screenshot _shot;

		public HaloScreenshot(Screenshot shot, LayoutDocument tabItem, string source)
		{
			InitializeComponent();

			_shot = shot;
			bool resize = App.AssemblyStorage.AssemblySettings.XdkResizeImages;
			bool gamma = App.AssemblyStorage.AssemblySettings.XdkScreenshotGammaCorrect;

			cbResize.IsChecked = resize;
			cbGamma.IsChecked = gamma;

			SetBitmap(resize, gamma);

			DateTime date = DateTime.Now;
			_datetime_long = date.ToString("yyyy-MM-dd,hh-mm-ss");
			_datetime_shrt = date.ToString("hh:mm.ss");

			tabItem.Title = source + " Screenshot {" + _datetime_shrt + "}";

			lblImageName.Text = _datetime_long + ".png";

			if (!App.AssemblyStorage.AssemblySettings.XdkAutoSave) return;

			if (!Directory.Exists(App.AssemblyStorage.AssemblySettings.XdkScreenshotPath))
				Directory.CreateDirectory(App.AssemblyStorage.AssemblySettings.XdkScreenshotPath);

			string filePath = App.AssemblyStorage.AssemblySettings.XdkScreenshotPath + "\\" + _datetime_long + ".png";
			SaveImage(filePath);
		}

		private void SetBitmap(bool resize, bool gamma, bool forceA2 = false)
		{
			using (Bitmap bitm = _shot.ConvertToBitmap(gamma, _shot.ScreenWidth != -1 ? resize : false, forceA2))
			{
				_bitmapImage = ScreenshotHelper.CreateBitmapSource(bitm);
			}

			imageScreenshot.Source = _bitmapImage;

			if (resize && _shot.ScreenWidth != -1 && (_shot.Width != _shot.ScreenWidth || _shot.Height != _shot.ScreenHeight))
				lblRes.Text = $"{_shot.ScreenWidth}x{_shot.ScreenHeight} [{_shot.Width}x{_shot.Height}]";
			else
				lblRes.Text = $"{_shot.Width}x{_shot.Height}";
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
				Filter = "PNG Image (*.png)|*.png",
				FileName = _datetime_long
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

		private void Reconvert_Click(object sender, RoutedEventArgs e)
		{
			bool resize = (bool)cbResize.IsChecked;
			bool gamma = (bool)cbGamma.IsChecked;
			bool format = (bool)cbFormat.IsChecked;

			SetBitmap(resize, gamma, format);

			if (!App.AssemblyStorage.AssemblySettings.XdkAutoSave) return;

			if (!Directory.Exists(App.AssemblyStorage.AssemblySettings.XdkScreenshotPath))
				Directory.CreateDirectory(App.AssemblyStorage.AssemblySettings.XdkScreenshotPath);

			string filePath = App.AssemblyStorage.AssemblySettings.XdkScreenshotPath + "\\" + _datetime_long + ".png";
			SaveImage(filePath);
		}

		private void CopyDebug_Click(object sender, RoutedEventArgs e)
		{
			using (StringWriter sw = new StringWriter())
			{
				foreach (string s in _shot.OriginalResponse.DumpValues())
				{
					sw.WriteLine(s);
				}

				Clipboard.SetText(sw.ToString());
			}
		}
	}
}