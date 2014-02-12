using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows;
using Atlas.Dialogs;
using Atlas.Models;
using Atlas.Models.BLF;
using Atlas.Views.BLF;
using Blamite.Blam.ThirdGen;
using Blamite.IO;

namespace Atlas.ViewModels.BLF
{
	public class MapImagePageViewModel : Base
	{
		public MapImagePage MapImagePage { get; private set; }

		public MapImagePageViewModel(MapImagePage mapInfoPage)
		{
			MapImagePage = mapInfoPage;
		}

		#region Properties

		public string MapImageLocation
		{
			get { return _mapInfoLocation; }
			private set { SetField(ref _mapInfoLocation, value); }
		}
		private string _mapInfoLocation;

		public MapImageBLF MapImageBLF
		{
			get { return _mapImageBLF; }
			private set { SetField(ref _mapImageBLF, value); }
		}
		private MapImageBLF _mapImageBLF;

		public List<byte> ImageBytes
		{
			get { return _imageBytes; }
			private set { SetField(ref _imageBytes, value); }
		}
		private List<byte> _imageBytes;

		public BitmapImage Image
		{
			get { return _image; }
			private set { SetField(ref _image, value); }
		}
		private BitmapImage _image;

		public string ImageFormat
		{
			get { return _imageFormat; }
			private set { SetField(ref _imageFormat, value); }
		}
		private string _imageFormat;

		#endregion

		public void LoadMapImage(string mapInfoLocation)
		{
			//App.Storage.HomeWindowViewModel.AssemblyPage = null;
			var dialog = MetroBusyAlertBox.Show();

			var thread = new Thread(() =>
			{
				MapImageLocation = mapInfoLocation;
				MapImageBLF = new MapImageBLF();

				var file = File.Open(mapInfoLocation, FileMode.Open, FileAccess.ReadWrite);
				var stream = new EndianStream(file, Endian.BigEndian);
				stream.BaseStream.Position = 0x0;

				// Extremely basic checks to ensure the file is indeed a BLF and Map Image file
				if (stream.ReadInt32() != 0x5F626C66)
					throw new Exception("The selected file is not a valid BLF file.");

				stream.BaseStream.Position = 0x30;
				if (stream.ReadInt32() != 0x6D617069)
					throw new Exception("The selected BLF is not a valid Map Image file.");

				// Since our checks tell us it's okay, an exception thrown by validation will likely be due to incorrect mapi and image lengths
				try
				{
					ValidateBLF(file);
				}
				// So if we get an exception, let's offer to fix that
				catch
				{
					// TODO: Update this to MetroMessageBox (Error was thrown about needing to be some other thread type)
					var answer = MessageBox.Show("The BLF file you selected had some errors. Would you like me to try to fix it for you?", "Invalid BLF Chunks", MessageBoxButton.YesNo);

					if (answer == MessageBoxResult.Yes)
					{
						// This assumes the file was made via the old image replacing technique, are there any other potential problems?
						stream.BaseStream.Position = 0x34;
						int newMapiLength = (int)stream.Length - 0x30;
						stream.WriteInt32(newMapiLength);

						stream.BaseStream.Position = 0x40;
						stream.WriteInt32(newMapiLength - 0x14);

						stream.BaseStream.Position = newMapiLength + 0x30;
						stream.WriteAscii("_eof");
						stream.BaseStream.Position = newMapiLength + 0x34;
						stream.WriteInt32(0x111);
						stream.WriteInt16(1);
						stream.WriteInt16(1);
						stream.WriteInt32(newMapiLength + 0x30);
						stream.WriteByte(3);
						stream.WriteBlock(new byte[0x100]);

						try
						{
							ValidateBLF(file);
						}
						catch
						{
							// TODO: Also update thos to MetroMessageBox
							MessageBox.Show("The problem could not be fixed. Sorry.", "Problem Persists", MessageBoxButton.OK);
						}
					}

					if (answer == MessageBoxResult.No)
					//	Close();
						return;
				}

				var fileInfo = new FileInfo(MapImageLocation);

				Application.Current.Dispatcher.Invoke(delegate
				{
					var image = new BitmapImage();
					image.BeginInit();
					image.StreamSource = new MemoryStream(ImageBytes.ToArray());
					image.EndInit();

					Image = image;

					switch (Image.PixelWidth)
					{
						case 182:
						case 256:
						case 463:
							MapImageBLF.Game = "Halo 3/ODST";
							break;
						case 229:
						case 400:
							MapImageBLF.Game = "Halo Reach";
							break;
						case 254:
						case 768:
						case 512:
						case 1280:
							MapImageBLF.Game = "Halo 4";
							break;

						default:
							MapImageBLF.Game = "Unknown";
							break;
					}

					App.Storage.HomeWindowViewModel.UpdateStatus(
						String.Format("{0}", fileInfo.Name));

					dialog.ViewModel.CanClose = true;
					dialog.Close();
					App.Storage.HomeWindowViewModel.HideDialog();
					//App.Storage.HomeWindowViewModel.AssemblyPage = MapImagePage;
				});
			});
			thread.Start();
		}

		private void ValidateBLF(Stream file)
		{
			PureBLF _blf = new PureBLF(file);

			if (_blf.BLFChunks[1].ChunkMagic != "mapi")
				throw new Exception("The selected BLF is not a valid Map Image file.");

			var imgChunkData = new List<byte>(_blf.BLFChunks[1].ChunkData);
			imgChunkData.RemoveRange(0, 0x08);
			ImageBytes = imgChunkData;

			UpdateBLF(_blf);

			_blf.Close();

			// Find out whether the file is a PNG or JPEG
			if (imgChunkData[0] == 0xFF && imgChunkData[1] == 0xD8)
				ImageFormat = "JPEG";
			else if (imgChunkData[0] == 0x89 && imgChunkData[1] == 0x50 & imgChunkData[2] == 0x4E && imgChunkData[3] == 0x47)
				ImageFormat = "PNG";
			else
				ImageFormat = "Unrecognized";
		}

		public void ReplaceImage()
		{
			if (ImageFormat != "JPEG" && ImageFormat != "PNG")
				return;

			PureBLF _blf = new PureBLF(MapImageLocation);

			string filter = "All Files (*.*)|*.*";
			switch (ImageFormat)
			{
				case "JPEG":
					filter = "JPEG Image (*.jpg)|*.jpg";
					break;
				case "PNG":
					filter = "PNG Image (*.png)|*.png";
					break;
			}

			var ofd = new OpenFileDialog
			{
				Title = "Open an image to inject",
				Filter = filter
			};

			if (!((bool)ofd.ShowDialog()))
			{
				return;
			}

			byte[] newImage = File.ReadAllBytes(ofd.FileName);
			var stream = new EndianStream(new MemoryStream(newImage), Endian.BigEndian);
			stream.SeekTo(0x0);

			// Make sure the file is the correct type
			if (ImageFormat == "JPEG" && stream.ReadUInt16() != 0xFFD8)
				throw new Exception("Invalid image type, it has to be a JPEG.");
			if (ImageFormat == "PNG" && stream.ReadUInt32() != 0x89504E47)
				throw new Exception("Invalid image type, it has to be a PNG.");

			var image = new BitmapImage();
			image.BeginInit();
			image.StreamSource = new MemoryStream(newImage);
			image.EndInit();

			// Make sure the file is the correct dimensions
			if (image.PixelWidth != Image.PixelWidth || image.PixelHeight != Image.PixelHeight)
				throw new Exception(string.Format("Image isn't the right size. It must be {0}x{1}", Image.PixelWidth, Image.PixelHeight));

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

			Image = image;
			UpdateBLF(_blf);
			_blf.Close();

			MetroMessageBox.Show("Injected!", "The BLF Image has been injected.");
		}

		public void ExtractImage()
		{
			var fileInfo = new FileInfo(MapImageLocation);

			string filter;
			string fileName;
			switch (ImageFormat)
			{
				case "JPEG":
				filter = "JPEG Image (*.jpg)|*.jpg";
				fileName = fileInfo.Name.Replace(".blf", ".jpg");
					break;
				case "PNG":
					filter = "PNG Image (*.png)|*.png";
					fileName = fileInfo.Name.Replace(".blf", ".png");
					break;

				default:
					filter = "All Files (*.*)|*.*";
					fileName = fileInfo.Name.Replace(".blf", "");
					break;
			}

			var sfd = new SaveFileDialog
			{
				Title = "Save the extracted BLF Image",
				Filter = filter,
				FileName = fileName
			};

			if (!((bool)sfd.ShowDialog()))
			{
				return;
			}
			File.WriteAllBytes(sfd.FileName, ImageBytes.ToArray());

			MetroMessageBox.Show("Exracted!", "The BLF Image has been extracted.");
		}

		private void UpdateBLF(PureBLF _blf)
		{
			MapImageBLF.ChunkCount = _blf.BLFChunks.Count.ToString();
			MapImageBLF.Length = "0x" + _blf.BLFStream.Length.ToString("X8");
			MapImageBLF.MapiLength = "0x" + _blf.BLFChunks[1].ChunkLength.ToString("X8");
			MapImageBLF.MapiVersion = BitConverter.ToInt16(BitConverter.GetBytes(_blf.BLFChunks[1].ChunkFlags), 2).ToString();
		}
	}
}
