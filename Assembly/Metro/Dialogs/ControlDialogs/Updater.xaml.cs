using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using Assembly.Helpers.Net;
using Assembly.Metro.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for Updater.xaml
    /// </summary>
    public partial class Updater
    {
        private readonly UpdateInfo _info;
        private string _currentVersion;

        public Updater(UpdateInfo info)
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);

            _info = info;

            LoadDataFromFormat();

            // Set up UI
            Width = 600;
            Height = 400;
            updateInfo.Visibility = Visibility.Visible;
            updateProgress.Visibility = Visibility.Collapsed;
        }

        private void LoadDataFromFormat()
        {
            _currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            lblCurrentVersion.Text = _currentVersion;
            lblServerVersion.Text = _info.LatestVersion;

            BuildChangelog(_info);
        }

        private void btnApplyUpdate_Click(object sender, RoutedEventArgs e)
        {
            updateInfo.Visibility = Visibility.Collapsed;
            updateProgress.Visibility = Visibility.Visible;
            lblTitle.Text = "Installing Update...";

			var storyboard = (Storyboard)Resources["ResizeWindowToUpdate"];
            storyboard.Begin();

            // Begin Update Downloading...
            DownloadUpdate();
        }

        private void btnIgnoreUpdate_Click(object sender, RoutedEventArgs e) { Close(); }

        private void BuildChangelog(UpdateInfo info)
        {
			for (var i = 0; i < info.Changelogs.Length; i++)
            {
				var changelog = info.Changelogs[i];
	            lblChangeLog.Inlines.Add(i == 0
		                                     ? new Bold(
			                                       new Run(string.Format("What's new in version {0} (latest):", changelog.Version)))
		                                     : new Bold(
			                                       new Run(string.Format("Changes made in previous version {0}:",
			                                                             changelog.Version))));

	            lblChangeLog.Inlines.Add(new Run(Environment.NewLine + Environment.NewLine));
                lblChangeLog.Inlines.Add(new Run(changelog.Changelog.TrimEnd('\r', '\n')));
                lblChangeLog.Inlines.Add(new Run(Environment.NewLine + Environment.NewLine));
            }
        }

        #region Update Installing
        private void DownloadUpdate()
        {
			var wb = new WebClient();
			var tempFile = Path.GetTempFileName();
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

        private static void ExtractUpdateManager(string updateZip)
        {
			var tempDir = Path.GetTempPath();

            // Extract SharpZipLib
			var zipDLL = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Assembly.Update.ICSharpCode.SharpZipLib.dll");
            using (var zipFileStream = new FileStream(Path.Combine(tempDir, "ICSharpCode.SharpZipLib.dll"), FileMode.Create))
	            if (zipDLL != null) zipDLL.CopyTo(zipFileStream);
	        if (zipDLL != null) zipDLL.Close();

	        // Extract AssemblyUpdateManager.exe
			var exeUpd = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Assembly.Update.AssemblyUpdateManager.exe");
			var updaterPath = Path.Combine(tempDir, "AssemblyUpdateManager.exe");
            using (var exeFileStream = new FileStream(updaterPath, FileMode.Create))
	            if (exeUpd != null) exeUpd.CopyTo(exeFileStream);
	        if (exeUpd != null) exeUpd.Close();

	        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var exeDir = Path.GetDirectoryName(exePath);

            // Run the updater in a windowless setting and pass in the path to the .zip and the current .exe
	        if (exeDir != null)
	        {
		        var updater = new ProcessStartInfo(updaterPath)
			                      {
				                      Arguments = string.Format("\"{0}\" \"{1}\"", updateZip, exePath),
				                      CreateNoWindow = true,
				                      WindowStyle = ProcessWindowStyle.Hidden,
				                      WorkingDirectory = exeDir
			                      };
		        Process.Start(updater);
	        }

	        Application.Current.Shutdown();
        }
        #endregion
    }
}
