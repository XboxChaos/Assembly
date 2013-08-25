using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;
using Blamite.Blam.ThirdGen;
using Blamite.IO;
using Blamite.Util;
using Microsoft.Win32;
using AvalonDock.Layout;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
    /// <summary>
    /// Interaction logic for HaloImage.xaml
    /// </summary>
    public partial class HaloImage
    {
        private readonly string _blfLocation;
        private PureBLF _blf;

        public HaloImage(string imageLocation, LayoutDocument tab)
        {
            InitializeComponent();

            _blfLocation = imageLocation;

            var fi = new FileInfo(_blfLocation);
	        tab.Title = fi.Name;

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

				// determine png vs jpg and then locate the header of the image
				// strip out all content prior to that for viewing
				int location = -1;
				if (_blf.BLFChunks[1].ImageType == "jpg")
				{
					location = ByteListArray.Locate(_blf.BLFChunks[1].ChunkData, _blf.JpgHeader, 100);
				}
				else if (_blf.BLFChunks[1].ImageType == "png")
				{
					location = ByteListArray.Locate(_blf.BLFChunks[1].ChunkData, _blf.PngHeader, 100);
				}

				if (location != -1)
					imgChunkData.RemoveRange(0, location);

                Dispatcher.Invoke(new Action(delegate
                {
					var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = new MemoryStream(imgChunkData.ToArray());
                    image.EndInit();

                    imgBLF.Source = image;

                    // Add Image Info
                    paneImageInfo.Children.Insert(0, new Components.MapHeaderEntry("Image Width:", image.PixelWidth + "px"));
                    paneImageInfo.Children.Insert(1, new Components.MapHeaderEntry("Image Height", image.PixelHeight + "px"));

                    // Add BLF Info
                    paneBLFInfo.Children.Insert(0, new Components.MapHeaderEntry("BLF Length:", "0x" + _blf.BLFStream.Length.ToString("X")));
                    paneBLFInfo.Children.Insert(1, new Components.MapHeaderEntry("BLF Chunks:", _blf.BLFChunks.Count.ToString(CultureInfo.InvariantCulture)));

                    if (Settings.startpageHideOnLaunch)
                        Settings.homeWindow.ExternalTabClose(Windows.Home.TabGenre.StartPage);

                    RecentFiles.AddNewEntry(new FileInfo(_blfLocation).Name, _blfLocation, "BLF Image", Settings.RecentFileType.BLF);
                }));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    MetroMessageBox.Show("Unable to open BLF", ex.Message.ToString(CultureInfo.InvariantCulture));
                    Settings.homeWindow.ExternalTabClose((LayoutDocument) Parent);
                }));
            }
        }

		private EndianStream skipExifData(EndianStream image)
        {
            byte[] _header = new byte[2];
            _header[0] = (byte)image.ReadByte();
            _header[1] = (byte)image.ReadByte();

            while(_header[0] == 0xFF && (_header[1] >= 0xE0 && _header[1] <= 0xEF))
            {
                int _length = image.ReadByte();
                _length = _length << 8;
                _length |= image.ReadByte();

                for (int i = 0; i < _length -2 ; i++) 
                {
                    image.ReadByte();
                }
                _header[0] = (byte) image.ReadByte();
                _header[1] = (byte) image.ReadByte();
            }
            return image;
        }


        /// <summary>
        /// Close stuff
        /// </summary>
        public bool Close() { try { _blf.Close(); } catch (Exception)
        { } return true; }

        private void btnInjectImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
				var ofd = new OpenFileDialog
					          {
								  Title = "Opem an image to be injected", 
								  Filter = "JPEG Image (*.jpg)|*.jpg|JPEG Image (*.jpeg)|*.jpeg"
							  };

	            if (!((bool) ofd.ShowDialog())) return;

	            var newImage = File.ReadAllBytes(ofd.FileName);
	            var stream = new EndianStream(new MemoryStream(newImage), Endian.BigEndian);

                // Check if it's a JFIF
                var soi = stream.ReadByte();
                var marker = stream.ReadByte();

                if (soi != 0xFF || marker != 0xD8)
		            throw new Exception("Invalid image type, it has to be a JPEG (JFIF in the header).");

				// remove exlif data
				stream = skipExifData(stream);

	            // Check if it's the right size
	            var image = new BitmapImage();
	            image.BeginInit();
				stream.SeekTo(0x00);
				image.StreamSource = new MemoryStream(stream.ReadBlock((int)stream.Length));
	            image.EndInit();

	            if (image.PixelWidth != ((BitmapImage)imgBLF.Source).PixelWidth || image.PixelHeight != ((BitmapImage)imgBLF.Source).PixelHeight)
		            throw new Exception(string.Format("Image isn't the right size. It must be {0}x{1}", ((BitmapImage)imgBLF.Source).PixelWidth, ((BitmapImage)imgBLF.Source).PixelHeight));

	            // It's the right everything! Let's inject
	            var newImageChunkData = new List<byte>();
	            newImageChunkData.AddRange(new byte[] { 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 });
				var imageLength = BitConverter.GetBytes(stream.Length);
	            Array.Reverse(imageLength);
	            newImageChunkData.AddRange(imageLength);
				stream.SeekTo(0x00);
				newImageChunkData.AddRange(stream.ReadBlock((int)stream.Length));

	            // Write data to chunk file
				_blf.BLFChunks[1].ChunkLength = (int)stream.Length;
	            _blf.BLFChunks[1].ChunkData = newImageChunkData.ToArray<byte>();
                    
				// update blf, and set image
	            _blf.UpdateChunkTable();
				_blf.Close();

	            imgBLF.Source = image;

	            MetroMessageBox.Show("Injected!", "The BLF Image has been injected.");
            }
            catch (Exception ex)
            {
                MetroMessageBox.Show("Inject Failed!", "The BLF Image failed to be injected: \n " + ex.Message);
            }
        }

        private void btnExtractImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
				var sfd = new SaveFileDialog
					          {
						          Title = "Save the extracted BLF Image",
						          Filter = "JPEG Image (*.jpg)|*.jpg",
						          FileName = lblBLFname.Text.Replace(".blf", "")
					          };

	            if (!((bool) sfd.ShowDialog())) return;
				var imageToExtract = new List<byte>(_blf.BLFChunks[1].ChunkData);

				// determine png vs jpg and then locate the header of the image
				// strip out all content prior to that for extraction
				int location = -1;
				if (_blf.BLFChunks[1].ImageType == "jpg")
				{
					location = ByteListArray.Locate(_blf.BLFChunks[1].ChunkData, _blf.JpgHeader, 100);
				}
				else if (_blf.BLFChunks[1].ImageType == "png")
				{
					location = ByteListArray.Locate(_blf.BLFChunks[1].ChunkData, _blf.PngHeader, 100);
				}
				
				if (location != -1)
                    imageToExtract.RemoveRange(0, location);

	            File.WriteAllBytes(sfd.FileName, imageToExtract.ToArray<byte>());

	            MetroMessageBox.Show("Exracted!", "The BLF Image has been extracted.");
            }
            catch (Exception ex)
            {
                MetroMessageBox.Show("Extraction Failed!", "The BLF Image failed to be extracted: \n " + ex.Message);
            }
        }
    }
}
