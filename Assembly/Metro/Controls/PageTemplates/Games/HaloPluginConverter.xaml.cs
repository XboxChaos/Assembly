using System.Linq;
using System.Xml;
using Assembly.Metro.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using ExtryzeDLL.Plugins;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
	/// <summary>
	/// Interaction logic for HaloPluginConverter.xaml
	/// </summary>
	public partial class HaloPluginConverter
	{
		public ObservableCollection<PluginEntry> SelectedPlugins = new ObservableCollection<PluginEntry>();

		public HaloPluginConverter()
        {
            InitializeComponent();
			DataContext = SelectedPlugins;
        }

        public class PluginEntry
        {
            public string PluginClassMagic { get; set; }
            public string LocalMapPath { get; set; }
            public bool IsSelected { get; set; }
        }
		public enum OutputFormats
		{
			Assembly,
			Alteration,
			Ascention
		}

		private bool _isWorking = false;
        public bool Close()
        {
	        return !_isWorking;
        }

		private void btnInputFolder_Click(object sender, RoutedEventArgs e)
		{
			var fbd = new System.Windows.Forms.FolderBrowserDialog
				          {
					          SelectedPath =
						          Directory.Exists(txtInputFolder.Text)
							          ? txtInputFolder.Text
							          : Helpers.VariousFunctions.GetApplicationLocation()
				          };
			if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

			SelectedPlugins.Clear();
			txtInputFolder.Text = fbd.SelectedPath;

			var di = new DirectoryInfo(txtInputFolder.Text);
			var fis = di.GetFiles();
			foreach (var fi in fis.Where(fi => (fi.Name.EndsWith(".asc") || fi.Name.EndsWith(".alt") || fi.Name.EndsWith(".ent") || fi.Name.EndsWith(".xml")) && Path.GetFileNameWithoutExtension(fi.Name).Length == 4))
			{
				SelectedPlugins.Add(new PluginEntry
					                    {
						                    IsSelected = true,
						                    LocalMapPath = fi.FullName,
						                    PluginClassMagic = fi.Name.Remove(fi.Name.Length - 3)
					                    });
			}
		}
		private void btnOutputFolder_Click(object sender, RoutedEventArgs e)
		{
			var fbd = new System.Windows.Forms.FolderBrowserDialog();
			if (Directory.Exists(txtOutputFolder.Text))
				fbd.SelectedPath = txtOutputFolder.Text;
			else
				fbd.SelectedPath = Helpers.VariousFunctions.GetApplicationLocation() + "\\plugins\\";

			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				txtOutputFolder.Text = fbd.SelectedPath;
		}
		private void btnConvertPlugins_Click(object sender, RoutedEventArgs e)
		{
			// Check that all needed info is loaded
			if (SelectedPlugins == null || ((String.IsNullOrEmpty(txtInputFolder.Text) || !Directory.Exists(txtInputFolder.Text)) ||
			                                (String.IsNullOrEmpty(txtOutputFolder.Text) || !Directory.Exists(txtOutputFolder.Text)) ||
			                                !(cbOutputFormat.SelectedIndex >= 0) ||
			                                !(cbTargetGame.SelectedIndex >= 0) ||
											(SelectedPlugins.Count(entry => !entry.IsSelected) == SelectedPlugins.Count)))
			{
				MetroMessageBox.Show("Missing required information", "Required information for Plugin Conversion is missing...");
				return;
			}

			StartConversion();
		}

		private void StartConversion()
		{
			_isWorking = true;

			btnConvertPlugins.IsEnabled = 
				btnInputFolder.IsEnabled = 
				btnOutputFolder.IsEnabled =
				cbOutputFormat.IsEnabled =
				cbTargetGame.IsEnabled = false;

			var worker = new BackgroundWorker();
			var gameName = (cbTargetGame.SelectedItem as ComboBoxItem).Tag.ToString();
			worker.DoWork += (o, args) => doConvert(worker, gameName);
			worker.WorkerReportsProgress = true;
			worker.ProgressChanged += worker_ProgressChanged;
			worker.RunWorkerCompleted += worker_RunWorkerCompleted;
			worker.RunWorkerAsync();

			MaskingPage.Visibility = Visibility.Visible;
		}
		private void EndConversion()
		{
			_isWorking = false;

			btnConvertPlugins.IsEnabled =
				btnInputFolder.IsEnabled =
				btnOutputFolder.IsEnabled =
				cbOutputFormat.IsEnabled =
				cbTargetGame.IsEnabled = true;

			MaskingPage.Visibility = Visibility.Collapsed;
		}

		private void doConvert(BackgroundWorker worker, string gameName)
		{
			var files = SelectedPlugins.Select(file => file.LocalMapPath).ToList();

			var progress = 0;
			foreach (var file in files)
			{
				var name = Path.GetFileName(file);
				worker.ReportProgress(progress, name);
				progress++;

				if ((!file.EndsWith(".asc") && !file.EndsWith(".alt") && !file.EndsWith(".ent") && !file.EndsWith(".xml")) ||
				    Path.GetFileNameWithoutExtension(file).Length != 4) continue;

				XmlReader reader = null;
				XmlWriter writer = null;
				try
				{

					var extension = "";
					Dispatcher.Invoke(new Action(() =>
						{
							switch ((OutputFormats)cbOutputFormat.SelectedIndex)
							{
								case OutputFormats.Ascention:
									extension = ".asc";
									break;
								case OutputFormats.Alteration:
									extension = ".alt";
									break;

								default:
									extension = ".xml";
									break;
							}
						}));
					var settings = new XmlWriterSettings
						               {
							               Indent = true, 
							               IndentChars = "\t"
						               };

					var outPath = "";
					Dispatcher.Invoke(new Action(() =>
						                             {
							                             outPath = Path.Combine(txtOutputFolder.Text,
							                                                        Path.ChangeExtension(name, extension));
						                             }));
					writer = XmlWriter.Create(outPath, settings);
					reader = XmlReader.Create(file);

					IPluginVisitor visitor;
					var format = OutputFormats.Assembly;
					Dispatcher.Invoke(new Action(() => { format = (OutputFormats) cbOutputFormat.SelectedIndex; }));
					switch (format)
					{
						case OutputFormats.Ascention:
							visitor = new AscensionPluginWriter(writer, Path.GetFileNameWithoutExtension(file));
							break;
						case OutputFormats.Assembly:
							visitor = new AssemblyPluginWriter(writer, gameName);
							break;

						default:
							throw new InvalidOperationException("Unsupported output format.");
					}

					UniversalPluginLoader.LoadPlugin(reader, visitor);
				}
				finally
				{
					if (reader != null)
						reader.Close();
					if (writer != null)
						writer.Close();
				}
			}
			worker.ReportProgress(progress, "Finished Converting Plugins!");
		}
		void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			lblProgressStatus.Text = string.Format("Converting Plugin '{0}' ({1}%)", e.UserState, e.ProgressPercentage);
		}
		void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				if (MetroMessageBox.Show("Plugin Conversion Failed!", "There was an error converting your plugins... Would you like to see it?", MetroMessageBox.MessageBoxButtons.YesNo) == MetroMessageBox.MessageBoxResult.Yes)
					MetroException.Show(e.Error);
			}
			else
			{
				MetroMessageBox.Show("Plugin conversion Complete!", "Your plugins have been successfully converted.");
			}

			EndConversion();
		}
	}
}
