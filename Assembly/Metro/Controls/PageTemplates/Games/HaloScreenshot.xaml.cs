using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ExtryzeDLL.IO;
using Assembly.Backend;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Assembly.Metro.Dialogs;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
    /// <summary>
    /// Interaction logic for HaloScreenshot.xaml
    /// </summary>
    public partial class HaloScreenshot : UserControl
    {
        private TabItem _tabitem;
        private string datetime_long;
        private string datetime_shrt;
        private string imageID;

        public HaloScreenshot(string tempImageLocation, TabItem tabItem)
        {
            InitializeComponent();

            // TabItem Saving
            _tabitem = tabItem;

            // Convert DDS to BitmapImage
            BitmapSource bitmapImage = DDSConversion.Deswizzle(tempImageLocation);


            // DateTime Creation
            DateTime date = DateTime.Now;
            datetime_long = date.ToString("yyyy-MM-dd,hh-mm-ss");
            datetime_shrt = date.ToString("hh:mm.ss");

            // Set Tab Header
            tabItem.Header = "Screenshot {" + datetime_shrt + "}";

            // Set Image Name
            lblImageName.Text = datetime_long + ".png";

            // Set Image
            imageScreenshot.Source = bitmapImage;

            // Should I save the image?
            if (Settings.XDKAutoSave)
            {
                if (!Directory.Exists(Settings.XDKScreenshotPath))
                    Directory.CreateDirectory(Settings.XDKScreenshotPath);

                string filePath = Settings.XDKScreenshotPath + "\\" + datetime_long + ".png";
                SaveImage(filePath);
            }
        }

        private void SaveImage(string filePath)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imageScreenshot.Source));
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
                encoder.Save(stream);
        }

        /// <summary>
        /// Close stuff
        /// </summary>
        public bool Close() { return true; }

        private void btnSaveImg_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Title = "Select where do you want to save the Screenshot";
            sfd.Filter = "PNG Image (*.png)|*.png";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                SaveImage(sfd.FileName);
        }
        private void btnUploadImage_Click(object sender, RoutedEventArgs e)
        {
            if (imageID == null)
            {
                doingAction.Visibility = System.Windows.Visibility.Visible;

                BackgroundWorker imageUpload = new BackgroundWorker();
                imageUpload.RunWorkerCompleted += imageUpload_RunWorkerCompleted;
                imageUpload.DoWork += imageUpload_DoWork;
                imageUpload.RunWorkerAsync();
            }
            else
                MetroImgurUpload.Show(imageID);
        }

        void imageUpload_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                string filePath = VariousFunctions.CreateTemporaryFile(VariousFunctions.GetTemporaryImageLocation());
                Dispatcher.Invoke(new Action(delegate
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imageScreenshot.Source));
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                        encoder.Save(stream);
                }));

                string newImageID = ServerConnector.PostImageToImgur(filePath);

                Dispatcher.Invoke(new Action(delegate
                {
                    imageID = newImageID;
                }));
            }
            catch
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    MetroMessageBox.Show("Error", "Unable to upload Image to server. Try again later.");
                }));
            }
        }
        void imageUpload_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            doingAction.Visibility = System.Windows.Visibility.Collapsed;

            if (imageID == null || (imageID.Length < 5 && imageID.Length > 9))
            {
                if (imageID != null)
                    MetroMessageBox.Show("Error", imageID);
                else
                    MetroMessageBox.Show("Error", "Error uploading image.");

                imageID = null;
            }
            else
                MetroImgurUpload.Show(imageID);
        }
    }
}
