using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Assembly.Helpers.Native;
using Assembly.Helpers.Net;
using Assembly.Helpers.PostGeneration;
using Assembly.Metro.Dialogs;
using Blamite.RTE.Console.Native;

namespace Assembly.Metro.Controls.PageTemplates.Tools
{
	/// <summary>
	///     Interaction logic for PostGenerator.xaml
	/// </summary>
	public partial class PostGenerator
	{
		private readonly ModPostInfo _generatorViewModel;

		public PostGenerator()
		{
			InitializeComponent();

			_generatorViewModel = new ModPostInfo();

			DataContext = _generatorViewModel;
		}

		// Header Stuff
		private void btnLaunchPatcher_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.AddPatchTabModule();
		}

		private void btnGrabPreviewImageFromXbox_Click(object sender, RoutedEventArgs e)
		{
			// Show Mask
			HeaderGrabMask.Visibility = Visibility.Visible;
			btnGrabPreviewImageFromXbox.IsEnabled = false;

			GrabImage(true); //isPreview
		}

		// Image Stuff
		private void btnDeleteImage_Click(object sender, RoutedEventArgs e)
		{
			var dataContext = (ModPostInfo.Image) ((Button) sender).DataContext;
			_generatorViewModel.Images.Remove(dataContext);
		}

		private void btnAddImage_Click(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(txtImageToAdd.Text.Trim()) ||
			    !Uri.IsWellFormedUriString(txtImageToAdd.Text, UriKind.RelativeOrAbsolute))
			{
				MetroMessageBox.Show("Invalid Image", "You didn't type a valid image url.");
				return;
			}

			IEnumerable<ModPostInfo.Image> existingImages =
				_generatorViewModel.Images.Where(
					image => image.Url.ToLowerInvariant().Trim() == txtImageToAdd.Text.ToLowerInvariant().Trim());

			if (!existingImages.Any())
				_generatorViewModel.Images.Add(new ModPostInfo.Image
				{
					Url = txtImageToAdd.Text.Trim()
				});
			txtImageToAdd.Text = "";
		}

		private void btnGrabImageFromXbox_Click(object sender, RoutedEventArgs e)
		{
			// Show Mask
			ImageGrabMask.Visibility = Visibility.Visible;
			btnAddImage.IsEnabled = btnGrabImageFromXbox.IsEnabled = false;

			GrabImage(false); //Not isPreview
		}

		private void GrabImage(bool isPreview)
		{
			var backgroundWorker = new BackgroundWorker();
			backgroundWorker.DoWork += (o, args) =>
			{
				string screenshotFileName = null, screenshotPng = null;
				try
				{
					// Grab Screenshot from Xbox
					screenshotFileName = Path.GetTempFileName();
					screenshotPng = Path.GetTempFileName();

					Screenshot shot = App.AssemblyStorage.AssemblySettings.XenonConsole.GetScreenshot();
					Bitmap bitmap = shot.ConvertToBitmap(App.AssemblyStorage.AssemblySettings.XdkScreenshotGammaCorrect, App.AssemblyStorage.AssemblySettings.XdkResizeImages);

					if (bitmap == null)
					{
						Dispatcher.Invoke(
							new Action(
								() =>
									MetroMessageBox.Show("Not Connected", "You are not connected to a debug Xbox 360, unable to get screenshot")));
						return;
					}

					// do stuff
					BitmapSource bitmapSource = ScreenshotHelper.CreateBitmapSource(bitmap);

					// convert to png
					SaveImage(screenshotPng, bitmapSource);

					// upload
					string response = Imgur.UploadToImgur(File.ReadAllBytes(screenshotPng));

					string finalString = string.Format("http://i.imgur.com/{0}.png", response);

					if (isPreview)
						Dispatcher.Invoke(new Action(() => _generatorViewModel.ModPreviewImage = finalString));
					else
						Dispatcher.Invoke(new Action(() => _generatorViewModel.Images.Add(new ModPostInfo.Image
						{
							Url = finalString
						})));
				}
				catch (Exception ex)
				{
					Dispatcher.Invoke(new Action(() => MetroException.Show(ex)));
				}
				finally
				{
					if (screenshotFileName != null)
						File.Delete(screenshotFileName);
					if (screenshotPng != null)
						File.Delete(screenshotPng);
				}
			};
			backgroundWorker.RunWorkerCompleted += (o, args) =>
			{
				if (isPreview)
				{
					HeaderGrabMask.Visibility = Visibility.Collapsed;
					btnGrabPreviewImageFromXbox.IsEnabled = true;
				}
				else
				{
					ImageGrabMask.Visibility = Visibility.Collapsed;
					btnAddImage.IsEnabled = btnGrabImageFromXbox.IsEnabled = true;
				}
			};
			backgroundWorker.RunWorkerAsync();
		}

		// Thank Stuff
		private void btnDeleteThank_Click(object sender, RoutedEventArgs e)
		{
			var dataContext = (ModPostInfo.Thank) ((Button) sender).DataContext;
			_generatorViewModel.Thanks.Remove(dataContext);
		}

		private void btnAddMention_Click(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(txtThankAlias.Text.Trim()) || String.IsNullOrEmpty(txtThankReason.Text.Trim()))
			{
				MetroMessageBox.Show("Invalid Mention Details", "The mention details you entered are invalid.");
				return;
			}

			IEnumerable<ModPostInfo.Thank> existingMentions =
				_generatorViewModel.Thanks.Where(
					thank =>
						thank.Alias.ToLowerInvariant().Trim() == txtThankAlias.Text.ToLowerInvariant().Trim() &&
						thank.Reason.ToLowerInvariant().Trim() == txtThankReason.Text.ToLowerInvariant().Trim());

			if (!existingMentions.Any())
				_generatorViewModel.Thanks.Add(new ModPostInfo.Thank
				{
					Alias = txtThankAlias.Text.Trim(),
					Reason = txtThankReason.Text.Trim(),
				});

			txtThankAlias.Text = "";
			txtThankReason.Text = "";
		}

		// Generate
		private void btnParse_Click(object sender, RoutedEventArgs e)
		{
			var postGenerator = new BlamitePostGenerator(_generatorViewModel);
			string generatedPost = postGenerator.Parse();

			MetroPostGeneratorViewer.Show(generatedPost, _generatorViewModel.ModAuthor);
		}

		/// <summary>
		///     Close dis mang
		/// </summary>
		public bool Close()
		{
			return true;
		}

		private void SaveImage(string filePath, BitmapSource bitmapSource)
		{
			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
			using (var stream = new FileStream(filePath, FileMode.Create))
				encoder.Save(stream);
		}
	}
}