using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Assembly.Backend;
using Assembly.Backend.Net;
using Assembly.Metro.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for Updater.xaml
    /// </summary>
    public partial class Updater : Window
    {
        private UpdateInfo _info;
        private string _currentVersion;

        public Updater(UpdateInfo info)
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);

            _info = info;

            LoadDataFromFormat();

            // Set up UI
            this.Width = 600;
            this.Height = 400;
            updateInfo.Visibility = System.Windows.Visibility.Visible;
            updateProgress.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void LoadDataFromFormat()
        {
            _currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            lblCurrentVersion.Text = _currentVersion;
            lblServerVersion.Text = _info.LatestVersion;

            BuildChangelog(_info);
        }

        private void headerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Left = Left + e.HorizontalChange;
            Top = Top + e.VerticalChange;
        }

        private void btnApplyUpdate_Click(object sender, RoutedEventArgs e)
        {
            updateInfo.Visibility = System.Windows.Visibility.Collapsed;
            updateProgress.Visibility = System.Windows.Visibility.Visible;
            lblTitle.Text = "Installing Update...";

            Storyboard storyboard = (Storyboard)this.Resources["ResizeWindowToUpdate"];
            storyboard.Begin();

            // Begin Update Downloading...
            DownloadUpdate();
        }

        private void btnIgnoreUpdate_Click(object sender, RoutedEventArgs e) { this.Close(); }

        private void BuildChangelog(UpdateInfo info)
        {
            for (int i = 0; i < info.Changelogs.Length; i++)
            {
                UpdateChangelog changelog = info.Changelogs[i];
                if (i == 0)
                    lblChangeLog.Inlines.Add(new Bold(new Run(string.Format("What's new in version {0} (latest):", changelog.Version))));
                else
                    lblChangeLog.Inlines.Add(new Bold(new Run(string.Format("Changes made in previous version {0}:", changelog.Version))));

                lblChangeLog.Inlines.Add(new Run(Environment.NewLine + Environment.NewLine));
                lblChangeLog.Inlines.Add(new Run(changelog.Changelog.TrimEnd('\r', '\n')));
                lblChangeLog.Inlines.Add(new Run(Environment.NewLine + Environment.NewLine));
            }
        }

        #region Update Installing
        private void DownloadUpdate()
        {
            WebClient wb = new WebClient();
            string tempFile = System.IO.Path.GetTempFileName();
            wb.DownloadFileCompleted += (o, args) =>
                {
                    if (args.Error != null)
                    {
                        File.Delete(tempFile);
                        throw args.Error;
                    }
                    pbDownloadProgress.IsIndeterminate = true;
                    ExtractUpdateManager(tempFile);
                };
            wb.DownloadProgressChanged += (o, args) =>
                {
                    lblDownloadProgress.Text = string.Format("Downloading Update -- ({0}%)", args.ProgressPercentage);
                    pbDownloadProgress.Value = args.ProgressPercentage;
                };
            
            wb.DownloadFileAsync(new Uri(_info.DownloadLink), tempFile);
            pbDownloadProgress.Value = 0;
            pbDownloadProgress.IsIndeterminate = false;
        }

        private void ExtractUpdateManager(string updateZip)
        {
            Stream zipDLL = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Assembly.Update.ICSharpCode.SharpZipLib.dll");
            Stream exeUpd = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Assembly.Update.AssemblyUpdateManager.exe");

            string tempDir = System.IO.Path.GetTempPath();
            using (var zipFileStream = new FileStream(System.IO.Path.Combine(tempDir, "ICSharpCode.SharpZipLib.dll"), FileMode.Create))
                zipDLL.CopyTo(zipFileStream);

            string updaterPath = System.IO.Path.Combine(tempDir, "AssemblyUpdateManager.exe");
            using (var exeFileStream = new FileStream(updaterPath, FileMode.Create))
                exeUpd.CopyTo(exeFileStream);

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = System.IO.Path.GetDirectoryName(exePath);

            // Run the updater in a windowless setting and pass in the path to the .zip and the current .exe
            ProcessStartInfo updater = new ProcessStartInfo(updaterPath);
            updater.Arguments = string.Format("\"{0}\" \"{1}\"", updateZip, exePath);
            updater.CreateNoWindow = true;
            updater.WindowStyle = ProcessWindowStyle.Hidden;
            updater.WorkingDirectory = exeDir;
            Process.Start(updater);

            Application.Current.Shutdown();
        }
        #endregion
    }
}
