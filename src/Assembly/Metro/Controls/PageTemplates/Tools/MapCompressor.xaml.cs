using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;
using Blamite.Compression;

namespace Assembly.Metro.Controls.PageTemplates.Tools
{
    /// <summary>
    /// Interaction logic for MapCompressor.xaml
    /// </summary>
    public partial class MapCompressor
    {
		private bool _doingWork;

		public MapCompressor()
        {
            InitializeComponent();
        }

		public bool Close()
		{
			return !_doingWork;
		}

		private void SetControls(bool enabled)
		{
			btnInputFile.IsEnabled = enabled;
			btnDoSingleCompression.IsEnabled = enabled;

			btnInputFolder.IsEnabled = enabled;
			batchButtons.IsEnabled = enabled;
		}

		private void EndWork()
		{
			_doingWork = false;

			SetControls(true);

			MaskingPage.Visibility = Visibility.Collapsed;
		}

		#region Single
		private void btnInputFile_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a map file",
				Filter = "Blam Cache Files|*.map"
			};
			if (ofd.ShowDialog() != DialogResult.OK) return;

			txtInputFile.Text = ofd.FileName;
		}

		private void btnDoSingleCompression_Click(object sender, RoutedEventArgs e)
		{
			if (!File.Exists(txtInputFile.Text))
			{
				MetroMessageBox.Show("Invalid Path", "The specified file provided does not exist.");
				return;
			}

			if (App.AssemblyStorage.AssemblySettings.HomeWindow.ContentModuleExists(txtInputFile.Text))
			{
				MetroMessageBox.Show("Map Open", "The selected map is currently open. Please close it before compressing it.");
				return;
			}

			StartSingleCompression();
		}

		private void StartSingleCompression()
		{
			_doingWork = true;

			SetControls(false);

			string filePath = txtInputFile.Text;
			var worker = new BackgroundWorker();
			worker.DoWork += DoSingleWork;
			worker.RunWorkerCompleted += workerSingle_RunWorkerCompleted;
			worker.RunWorkerAsync(argument: filePath);

			lblProgressStatus.Text = "Please wait...";
			MaskingPage.Visibility = Visibility.Visible;
		}

		private void DoSingleWork(object sender, DoWorkEventArgs e)
		{
			e.Result = CacheCompressor.HandleCompression((string)e.Argument, App.AssemblyStorage.AssemblySettings.DefaultDatabase);
		}

		private void workerSingle_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			CompressionState result = (CompressionState)e.Result;

			switch (result)
			{
				case CompressionState.Null:
					MetroMessageBox.Show("Compression Not Required", "The provided map does not require compression. It has not been modified.");
					break;
				case CompressionState.ReadOnly:
					MetroMessageBox.Show("Map is Read Only", "The provided map is readonly and cannot be modified. Check the file's properties and try again.");
					break;
				default:
					MetroMessageBox.Show("Action Complete", string.Format("The provided map was {0} successfully.", result.ToString().ToLowerInvariant()));
					break;
			}

			EndWork();
		}

		private void btnOpenMap_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.ProcessContentFile(txtInputFile.Text);
		}

		#endregion

		#region Batch
		private void btnInputFolder_Click(object sender, RoutedEventArgs e)
		{
			var fbd = new FolderBrowserDialog
			{
				SelectedPath =
					Directory.Exists(txtInputFolder.Text)
						? txtInputFolder.Text
						: VariousFunctions.GetApplicationLocation()
			};
			if (fbd.ShowDialog() != DialogResult.OK) return;

			txtInputFolder.Text = fbd.SelectedPath;
		}

		private void btnDoBatchComp_Click(object sender, RoutedEventArgs e)
		{
			if (!VerifyPath(txtInputFolder.Text))
				return;

			StartBatchCompression(CompressionState.Compressed);
		}

		private void btnDoBatchDecomp_Click(object sender, RoutedEventArgs e)
		{
			if (!VerifyPath(txtInputFolder.Text))
				return;

			StartBatchCompression(CompressionState.Decompressed);
		}

		private bool VerifyPath(string path)
		{
			if (!Directory.Exists(txtInputFolder.Text))
			{
				MetroMessageBox.Show("Invalid Path", "The specified path provided for batch does not exist.");
				return false;
			}

			return true;
		}

		private void StartBatchCompression(CompressionState state)
		{
			if (state == CompressionState.Null)
				return;

			_doingWork = true;

			SetControls(false);

			string[] files = Directory.GetFiles(txtInputFolder.Text, "*.map");

			List<string> closedfiles = new List<string>();
			foreach (string f in files)
			{
				if (App.AssemblyStorage.AssemblySettings.HomeWindow.ContentModuleExists(f, false))
					continue;

				closedfiles.Add(f);
			}

			var worker = new BackgroundWorker();

			worker.DoWork += (o, args) => DoBatchWork(worker, closedfiles, state);
			worker.WorkerReportsProgress = true;
			worker.ProgressChanged += worker_ProgressChanged;
			worker.RunWorkerCompleted += (o, args) => workerBatch_RunWorkerCompleted(state);

			worker.RunWorkerAsync();
			
			MaskingPage.Visibility = Visibility.Visible;
		}

		private void DoBatchWork(BackgroundWorker worker, List<string> files, CompressionState state)
		{
			for (int i = 0; i < files.Count; i++)
			{
				CacheCompressor.HandleCompression(files[i], App.AssemblyStorage.AssemblySettings.DefaultDatabase, state);

				worker.ReportProgress((i + 1) * 100 / files.Count);
			}
		}

		private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			lblProgressStatus.Text = string.Format("Performing batch action, {0}% complete.", e.ProgressPercentage);
		}

		private void workerBatch_RunWorkerCompleted(CompressionState state)
		{
			MetroMessageBox.Show("Batch Action Complete", string.Format("All applicable files have been {0}.", state.ToString().ToLowerInvariant()));

			EndWork();
		}
		
		#endregion
	}
}
