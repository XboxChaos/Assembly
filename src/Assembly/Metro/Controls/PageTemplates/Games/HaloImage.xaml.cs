using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components;
using Assembly.Metro.Dialogs;
using Assembly.Windows;
using Xceed.Wpf.AvalonDock.Layout;
using Blamite.Blam.ThirdGen;
using Blamite.IO;
using Microsoft.Win32;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
	/// <summary>
	///     Interaction logic for HaloImage.xaml
	/// </summary>
	public partial class HaloImage
	{
		private readonly LayoutDocument _tab;
		private readonly string _blfLocation;
		private PureBLF _blf;
		private string blfImageFormat;

		public HaloImage(string imageLocation, LayoutDocument tab)
		{
			InitializeComponent();

			_blfLocation = imageLocation;

			var fi = new FileInfo(_blfLocation);
			tab.Title = fi.Name;

			_tab = tab;
			
			lblBLFname.Text = fi.Name;

			var thrd = new Thread(loadBLF);
			thrd.Start();
		}

		private void loadBLF()
		{
			try
			{
				_blf = new PureBLF(_blfLocation);

				var imgChunkData = new List<byte>(_blf.BLFChunks[1].ChunkData);
				imgChunkData.RemoveRange(0, 0x08);

				Dispatcher.Invoke(new Action(delegate
				{
					var image = new BitmapImage();
					image.BeginInit();
					image.StreamSource = new MemoryStream(imgChunkData.ToArray());
					image.EndInit();

					imgBLF.Source = image;

					var stream = new EndianStream(new MemoryStream(imgChunkData.ToArray<byte>()), Endian.BigEndian);
					stream.SeekTo(0x0);
					ushort imageMagic = stream.ReadUInt16();

					switch (imageMagic)
					{
						case 0xFFD8:
							blfImageFormat = "JPEG";
							break;
						case 0x8950:
							blfImageFormat = "PNG";
							break;
						case 0x424D:
							blfImageFormat = "BMP";
							break;
						default:
							blfImageFormat = "Unknown";
							break;
					}

					// Add Image Info
					paneImageInfo.Children.Insert(0, new MapHeaderEntry("Image Format:", blfImageFormat));
					paneImageInfo.Children.Insert(1, new MapHeaderEntry("Image Width:", image.PixelWidth + "px"));
					paneImageInfo.Children.Insert(2, new MapHeaderEntry("Image Height", image.PixelHeight + "px"));

					// Add BLF Info
					paneBLFInfo.Children.Insert(0, new MapHeaderEntry("BLF Length:", "0x" + _blf.BLFStream.Length.ToString("X")));
					paneBLFInfo.Children.Insert(1,
						new MapHeaderEntry("BLF Chunks:", _blf.BLFChunks.Count.ToString(CultureInfo.InvariantCulture)));

					if (App.AssemblyStorage.AssemblySettings.StartpageHideOnLaunch)
						App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(Home.TabGenre.StartPage);

					RecentFiles.AddNewEntry(new FileInfo(_blfLocation).Name, _blfLocation, "BLF Image", Settings.RecentFileType.Blf);
					Close();
				}));
			}
			catch (Exception ex)
			{
				Close();
				Dispatcher.Invoke(new Action(delegate
				{
					MetroMessageBox.Show("Unable to open BLF", ex.Message.ToString(CultureInfo.InvariantCulture));
					App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose((LayoutDocument)Parent);
				}));
			}
		}

		/// <summary>
		///     Close stuff
		/// </summary>
		public bool Close()
		{
			try
			{
				_blf.Close();
			}
			catch (Exception)
			{
			}
			return true;
		}

		private void btnInjectImage_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_blf = new PureBLF(_blfLocation);
				var ofd = new OpenFileDialog
				{
					Title = "Open an image to be injected",
					Filter = "JPEG Image (*.jpg,*.jpeg,)|*.jpg;*.jpeg|PNG Image [H3/ODST]|*.png|BMP Image [H3/ODST]|*.bmp"
				};

				if (!((bool)ofd.ShowDialog()))
				{
					Close();
					return;
				}
				byte[] newImage = File.ReadAllBytes(ofd.FileName);
				var stream = new EndianStream(new MemoryStream(newImage), Endian.BigEndian);

				// Check if it's a supported image
				stream.SeekTo(0x0);
				ushort imageMagic = stream.ReadUInt16();
				if (imageMagic != 0xFFD8 && imageMagic != 0x8950 && imageMagic != 0x424D)
					throw new Exception("Invalid image type. Only JPEG, PNG, and BMP are supported.");

				// Check for size and dimension differences
				var imageSize = new FileInfo(ofd.FileName).Length;
				var image = new BitmapImage();
				image.BeginInit();
				image.StreamSource = new MemoryStream(newImage);
				image.EndInit();
				string sizeMessage = "";
				string dimensionMessage = "";

				if (new FileInfo(ofd.FileName).Length >= 0x1C000)
					sizeMessage = String.Format("- The size of the new image (0x{0}) exceeds Halo 3/ODST's modified limit of 0x1C000. This image will not display in those games as a result. Can be ignored otherwise.\n",
						imageSize.ToString("X"));

				if (image.PixelWidth != ((BitmapImage)imgBLF.Source).PixelWidth ||
					image.PixelHeight != ((BitmapImage)imgBLF.Source).PixelHeight)
					dimensionMessage = String.Format("- The dimensions of the new image ({0}x{1}) are not the same as the dimensions of the original image ({2}x{3}). This blf may appear stretched or not appear at all as a result.\n",
						image.PixelWidth, image.PixelHeight, ((BitmapImage)imgBLF.Source).PixelWidth, ((BitmapImage)imgBLF.Source).PixelHeight);

				if (dimensionMessage != "" || sizeMessage != "")
					if (MetroMessageBox.Show("Warning",
						"There were some potential issue(s) found with your new image;\n\n" + String.Format("{0}{1}",
						sizeMessage, dimensionMessage) + "\nInject anyway?",
						MetroMessageBox.MessageBoxButtons.OkCancel) != MetroMessageBox.MessageBoxResult.OK)
						{
							Close();
							return;
						}

				// It's the right everything! Let's inject

				var newImageChunkData = new List<byte>();
				newImageChunkData.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
				byte[] imageLength = BitConverter.GetBytes(newImage.Length);
				Array.Reverse(imageLength);
				newImageChunkData.AddRange(imageLength);
				newImageChunkData.AddRange(newImage);

				// Write data to chunk file
				_blf.BLFChunks[1].ChunkData = newImageChunkData.ToArray<byte>();

				_blf.RefreshRelativeChunkData();
				_blf.UpdateChunkTable();

				// Update eof offset value
				var eofstream = new EndianStream(new MemoryStream(_blf.BLFChunks[2].ChunkData), Endian.BigEndian);

				uint eofFixup = (uint)_blf.BLFStream.Length - 0x111; //real cheap but hey it works and is always the same in all games

				eofstream.SeekTo(0);
				eofstream.WriteUInt32(eofFixup);

				_blf.RefreshRelativeChunkData();
				_blf.UpdateChunkTable();

				Close();
				MetroMessageBox.Show("Injected!", "The BLF Image has been injected. This image tab will now close.");
				App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(_tab);
			}
			catch (Exception ex)
			{
				Close();
				MetroMessageBox.Show("Inject Failed!", "The BLF Image failed to be injected: \n " + ex.Message);
			}
		}

		private void btnExtractImage_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_blf = new PureBLF(_blfLocation);
				var sfd = new SaveFileDialog
				{
					Title = "Save the extracted BLF Image",
					FileName = lblBLFname.Text.Replace(".blf", "")
				};
				
				//Check if the blf image is a not JPG and set the filter accordingly
				switch (blfImageFormat)
				{
					case "PNG":
						sfd.Filter = "PNG Image (*.png)|*.png";
						break;
					case "BMP":
						sfd.Filter = "BMP Image (*.bmp)|*.bmp";
						break;
					default:
						sfd.Filter = "JPEG Image (*.jpg)|*.jpg";
						break;
				}

				if (!((bool)sfd.ShowDialog()))
				{
					Close();
					return;
				}
				var imageToExtract = new List<byte>(_blf.BLFChunks[1].ChunkData);
				imageToExtract.RemoveRange(0, 0x08);

				File.WriteAllBytes(sfd.FileName, imageToExtract.ToArray<byte>());

				MetroMessageBox.Show("Exracted!", "The BLF Image has been extracted.");
				Close();
			}
			catch (Exception ex)
			{
				Close();
				MetroMessageBox.Show("Extraction Failed!", "The BLF Image failed to be extracted: \n " + ex.Message);
			}
		}
	}
}