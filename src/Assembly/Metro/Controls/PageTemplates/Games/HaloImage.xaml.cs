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
                imgChunkData.RemoveRange(0, 0x08);

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

	            // Check if it's the right size
	            var image = new BitmapImage();
	            image.BeginInit();
	            image.StreamSource = new MemoryStream(newImage);
	            image.EndInit();

	            if (image.PixelWidth != ((BitmapImage)imgBLF.Source).PixelWidth || image.PixelHeight != ((BitmapImage)imgBLF.Source).PixelHeight)
		            throw new Exception(string.Format("Image isn't the right size. It must be {0}x{1}", ((BitmapImage)imgBLF.Source).PixelWidth, ((BitmapImage)imgBLF.Source).PixelHeight));

	            // It's the right everything! Let's inject


	            var newImageChunkData = new List<byte>();
	            newImageChunkData.AddRange(new byte[] { 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 });
	            var imageLength = BitConverter.GetBytes(newImage.Length);
	            Array.Reverse(imageLength);
	            newImageChunkData.AddRange(imageLength);
	            newImageChunkData.AddRange(newImage);

	            // Write data to chunk file
	            _blf.BLFChunks[1].ChunkData = newImageChunkData.ToArray<byte>();
                    
	            _blf.RefreshRelativeChunkData();
	            _blf.UpdateChunkTable();

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
	            imageToExtract.RemoveRange(0, 0x08);

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
