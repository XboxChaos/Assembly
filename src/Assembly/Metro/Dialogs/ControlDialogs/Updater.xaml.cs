using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using Assembly.Helpers;
using Assembly.Helpers.Native;
using Assembly.Helpers.Net;
using XboxChaos.Models;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for Updater.xaml
	/// </summary>
	public partial class Updater
	{
		private readonly ApplicationBranchResponse _info;
		private string _currentVersion;

		public Updater(ApplicationBranchResponse info, bool available)
		{
			InitializeComponent();
			DwmDropShadow.DropShadowToWindow(this);

			_info = info;
			/*if (!available)
			{
				lblAvailable.Text = "Your version of Assembly is up-to-date.";
				lblAvailable.FontWeight = FontWeights.Normal;
				updateButtons.Visibility = Visibility.Collapsed;
				noUpdate.Visibility = Visibility.Visible;
			}*/

			LoadDataFromFormat();

			// Set up UI
			Width = 600;
			Height = 400;
			updateInfo.Visibility = Visibility.Visible;
			updateProgress.Visibility = Visibility.Collapsed;
		}

		private void LoadDataFromFormat()
		{
			var friendlyVersion = VersionInfo.GetUserFriendlyVersion();
			_currentVersion = (friendlyVersion != null) ? friendlyVersion.ToString() : "(unknown)";
			lblCurrentVersion.Text = _currentVersion;
			lblServerVersion.Text = (_info.Version != null) ? _info.Version.Friendly.ToString() : "(unknown)";

			BuildChangelog();
		}

		private void btnApplyUpdate_Click(object sender, RoutedEventArgs e)
		{
			updateInfo.Visibility = Visibility.Collapsed;
			btnActionClose.Visibility = Visibility.Collapsed;
			updateProgress.Visibility = Visibility.Visible;
			lblTitle.Text = "Installing Update...";

			var storyboard = (Storyboard) Resources["ResizeWindowToUpdate"];
			storyboard.Begin();

			// Begin Update Downloading...
			DownloadUpdate();
		}

		private void btnIgnoreUpdate_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void BuildChangelog()
		{
			// Loop through the change list sorted in descending order by version
			var first = true;
			foreach (var change in _info.Changes.OrderByDescending(c => c.Version))
			{
				lblChangeLog.Inlines.Add(first
					? new Bold(
						new Run(string.Format("What's new in version {0} (latest):", change.Version.Friendly)))
					: new Bold(
						new Run(string.Format("Changes made in previous version {0}:",
							change.Version.Friendly))));
				first = false;

				lblChangeLog.Inlines.Add(new Run(Environment.NewLine + Environment.NewLine));
				lblChangeLog.Inlines.Add(new Run(change.Change.TrimEnd('\r', '\n')));
				lblChangeLog.Inlines.Add(new Run(Environment.NewLine + Environment.NewLine));
			}
		}

		#region Update Installing

		private void DownloadUpdate()
		{
			var wb = new WebClient();
			var buildZipPath = Path.GetTempFileName();
			string updateZipPath = null;
			var currentFile = 1;
			wb.DownloadFileCompleted += (o, args) =>
			{
				if (args.Error != null)
				{
					if (File.Exists(buildZipPath))
						File.Delete(buildZipPath);
					if (updateZipPath != null && File.Exists(updateZipPath))
						File.Delete(updateZipPath);
					throw args.Error;
				}
				if (updateZipPath == null)
				{
					// Download the update zip
					updateZipPath = Path.GetTempFileName();
					pbDownloadProgress.Value = 0;
					currentFile++;
					wb.DownloadFileAsync(new Uri(_info.UpdaterDownload), updateZipPath);
					return;
				}
				pbDownloadProgress.IsIndeterminate = true;
				try
				{
					ExtractUpdateManager(buildZipPath, updateZipPath);
				}
				catch
				{
					if (File.Exists(buildZipPath))
						File.Delete(buildZipPath);
					if (File.Exists(updateZipPath))
						File.Delete(updateZipPath);
					throw;
				}
			};
			wb.DownloadProgressChanged += (o, args) =>
			{
				lblDownloadProgress.Text = string.Format("Downloading Update -- ({0}/2, {1}%)", currentFile, args.ProgressPercentage);
				pbDownloadProgress.Value = args.ProgressPercentage;
			};

			wb.DownloadFileAsync(new Uri(_info.BuildDownload), buildZipPath);
			pbDownloadProgress.Value = 0;
			pbDownloadProgress.IsIndeterminate = false;
		}

		private static void ExtractUpdateManager(string buildZip, string updateZip)
		{
			// Extract the update zip to the temp directory
			var updaterDir = Path.GetTempPath();
			try
			{
				using (var file = File.OpenRead(updateZip))
				{
					using (var archive = new ZipArchive(file, ZipArchiveMode.Read, true))
						ExtractArchive(archive, updaterDir);
				}
			}
			finally
			{
				if (File.Exists(updateZip))
					File.Delete(updateZip);
			}

			var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			var exeDir = Path.GetDirectoryName(exePath);
			var pid = Process.GetCurrentProcess().Id;

			// Run the updater in a windowless setting and pass in the path to the .zip and the current .exe
			if (exeDir != null)
			{
				var updater = new ProcessStartInfo(Path.Combine(updaterDir, "update.exe"))
				{
					Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", buildZip, exePath, pid),
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					WorkingDirectory = exeDir
				};
				Process.Start(updater);
			}

			Application.Current.Shutdown();
		}

		private static void ExtractArchive(ZipArchive archive, string outDir)
		{
			foreach (var entry in archive.Entries)
			{
				var outPath = Path.Combine(outDir, entry.FullName);
				if (outPath.EndsWith("\\") || outPath.EndsWith("/"))
					Directory.CreateDirectory(outPath);
				else
					entry.ExtractToFile(outPath, true);
			}
		}

		#endregion
	}
}