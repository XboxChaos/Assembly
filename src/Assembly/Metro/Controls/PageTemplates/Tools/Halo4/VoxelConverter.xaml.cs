using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml;
using Assembly.Metro.Dialogs;
using Newtonsoft.Json;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Tools.Halo4
{
	/// <summary>
	/// Interaction logic for VoxelConverter.xaml
	/// </summary>
	public partial class VoxelConverter
	{
		public VoxelConverter()
		{
			InitializeComponent();
		}

		private bool _doingWork;
		public bool Close()
		{
			return !_doingWork;
		}

		private void btnInputFile_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
				          {
					          Title = "Assembly - Select a XML Voxel Definition File",
					          Filter = "Xml File (*.xml)|*.xml"
				          };
			if (ofd.ShowDialog() != DialogResult.OK) return;

			txtInputFile.Text = ofd.FileName;
		}
		private void btnConvertVoxel_Click(object sender, RoutedEventArgs e)
		{
			// Check that all needed info is loaded
			if (!File.Exists(txtInputFile.Text))
			{
				MetroMessageBox.Show("Missing required information", "Required information for Voxel Conversion is missing...");
				return;
			}

			StartConversion();
		}

		private void StartConversion()
		{
			_doingWork = true;

			btnInputFile.IsEnabled =
				btnConvertVoxel.IsEnabled = false;

			var filePath = txtInputFile.Text;
			var worker = new BackgroundWorker();
			worker.DoWork += (o, args) => doConvert(worker, filePath);
			worker.WorkerReportsProgress = true;
			worker.ProgressChanged += worker_ProgressChanged;
			worker.RunWorkerCompleted += worker_RunWorkerCompleted;
			worker.RunWorkerAsync();

			MaskingPage.Visibility = Visibility.Visible;
		}
		private void EndConversion()
		{
			_doingWork = false;

			btnInputFile.IsEnabled =
				btnConvertVoxel.IsEnabled = true;

			MaskingPage.Visibility = Visibility.Collapsed;
		}

		public class VoxelDataContainer
		{
			public VoxelData Voxel_Data { get; set; }

			public class VoxelData
			{
				public IList<PositionData> Position { get; set; }

				public class PositionData
				{
					public string Object { get; set; }
					public string Material { get; set; }
					public float x { get; set; }
					public float y { get; set; }
					public float z { get; set; }
				}
			}
		}
		private void doConvert(BackgroundWorker worker, string filePath)
		{
			var doc = new XmlDocument();
			doc.LoadXml(File.ReadAllText(filePath));
			var xmlNodes = doc.SelectNodes("voxel_data/position");
			var index = 0;
			var outputBuilder = new StringBuilder();

			if (xmlNodes != null)
				foreach (XmlNode voxelData in xmlNodes)
				{
					// Calculate Percentage Done
					float a = xmlNodes.Count;
					float b = index;
					a = a <= 0 ? 1 : a;
					b = b <= 0 ? 1 : b;
					b = ((a - b) / a) * 100;
					worker.ReportProgress((int)((100 - b) * 2.5));

					try
					{
						var x = float.Parse(voxelData.SelectSingleNode("x/text()").Value);
						var y = float.Parse(voxelData.SelectSingleNode("y/text()").Value);
						var z = float.Parse(voxelData.SelectSingleNode("z/text()").Value);

						outputBuilder.Append(string.Format("{0}{1}{2}",
						                                   FloatToHexString(x),
						                                   FloatToHexString(y),
						                                   FloatToHexString(z)));
					}
					finally { }

					index++;
				}
			Dispatcher.Invoke(new Action(() =>
			{
				txtOutputData.Text = outputBuilder.ToString();
			}));

			worker.ReportProgress(100);
		}
		void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			lblProgressStatus.Text = string.Format("Converting Voxel Data... ({0}%)", e.ProgressPercentage);
		}
		void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				if (MetroMessageBox.Show("Voxel Conversion Failed!", "There was an error converting your voxel data... Would you like to see it?", MetroMessageBox.MessageBoxButtons.YesNo) == MetroMessageBox.MessageBoxResult.Yes)
					MetroException.Show(e.Error);
			}
			else
			{
				MetroMessageBox.Show("Voxel conversion Complete!", "Your voxel data has been successfully converted.");
			}

			EndConversion();
		}


		private string FloatToHexString(float val)
		{
			var floatBytes = BitConverter.GetBytes(val);
			Array.Reverse(floatBytes);
			return BitConverter.ToString(floatBytes).Replace("-", string.Empty);
		}
	}
}
