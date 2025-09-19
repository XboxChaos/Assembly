using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;
using Blamite.Blam;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;

namespace Assembly.Metro.Controls.PageTemplates.Tools
{
	/// <summary>
	/// Interaction logic for TagListings.xaml
	/// </summary>
	public partial class TagListings
	{
		public ObservableCollection<MapEntry> GeneratorMaps = new ObservableCollection<MapEntry>();

		private string[] mapFilter = new string[] //maps that arent proper cache files.
		{
			"bitmaps.map",
			"loc.map",
			"sounds.map",
		};

		public TagListings()
		{
			InitializeComponent();
			DataContext = GeneratorMaps;
		}

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

			GeneratorMaps.Clear();
			txtInputFolder.Text = fbd.SelectedPath;

			var di = new DirectoryInfo(txtInputFolder.Text);
			FileInfo[] fis = di.GetFiles("*.map");
			foreach (
				FileInfo fi in
					fis.Where(
						fi => !mapFilter.Contains(fi.Name.ToLower())))
			{
				GeneratorMaps.Add(new MapEntry
				{
					IsSelected = true,
					LocalMapPath = fi.FullName,
					MapName = fi.Name
				});
			}
		}

		private void btnOutputFolder_Click(object sender, RoutedEventArgs e)
		{
			var fbd = new FolderBrowserDialog();
			if (Directory.Exists(txtOutputFolder.Text))
				fbd.SelectedPath = txtOutputFolder.Text;

			if (fbd.ShowDialog() == DialogResult.OK)
			{
				txtOutputFolder.Text = fbd.SelectedPath;
			}
		}

		private void btnGeneratePlugins_Click(object sender, RoutedEventArgs e)
		{
			// Check that all needed info is loaded
			if (GeneratorMaps == null ||
				((string.IsNullOrEmpty(txtInputFolder.Text) || !Directory.Exists(txtInputFolder.Text)) ||
				 (string.IsNullOrEmpty(txtOutputFolder.Text) || !Directory.Exists(txtOutputFolder.Text)) ||
				 (GeneratorMaps.Count(entry => !entry.IsSelected) == GeneratorMaps.Count)))
			{
				MetroMessageBox.Show("Missing required information", "Required information for list generation is missing...");
				return;
			}

			StartGeneration();
		}

		private void StartGeneration()
		{
			btnInputFolder.IsEnabled =
				btnOutputFolder.IsEnabled =
					btnGeneratePlugins.IsEnabled = false;

			MaskingPage.Visibility = Visibility.Visible;

			List<MapEntry> generatorMaps = GeneratorMaps.Where(m => m.IsSelected).ToList();
			string outputPath = txtOutputFolder.Text;

			EngineDescription picked = null;
			string firstMap = generatorMaps.First().LocalMapPath;
			using (FileStream fs = File.OpenRead(firstMap))
			{
				using (EndianReader reader = new EndianReader(fs, Endian.BigEndian))
				{
					var matches = CacheFileLoader.FindEngineDescriptions(reader, App.AssemblyStorage.AssemblySettings.DefaultDatabase);

					if (matches.Count > 1)
					{
						picked = MetroEnginePicker.Show(firstMap, matches);
					}
				}
			}

			var worker = new BackgroundWorker();
			worker.DoWork += (o, args) => worker_DoWork(o, args, generatorMaps, outputPath, worker, picked);
			worker.WorkerReportsProgress = true;
			worker.ProgressChanged += worker_ProgressChanged;
			worker.RunWorkerCompleted += worker_RunWorkerCompleted;
			worker.RunWorkerAsync();
		}

		private void EndGeneration()
		{
			btnInputFolder.IsEnabled =
				btnOutputFolder.IsEnabled =
					btnGeneratePlugins.IsEnabled = true;

			MaskingPage.Visibility = Visibility.Collapsed;
		}

		private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error == null)
				MetroMessageBox.Show("List Generation Complete!", "List generation completed successfully in " +
				Math.Round(((TimeSpan)e.Result).TotalSeconds, 3) + " seconds.");
			else
				MetroMessageBox.Show("Error Generating Lists!", e.Error.ToString());

			EndGeneration();
		}

		private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			lblProgressStatus.Text = string.Format("Generating Lists... ({0}%)", e.ProgressPercentage);
		}

		private void worker_DoWork(object sender, DoWorkEventArgs e, IList<MapEntry> generatorMaps, string outputPath,
			BackgroundWorker worker, EngineDescription pickedEngine)
		{
			DateTime startTime = DateTime.Now;
			worker.ReportProgress(0);

			for (int i = 0; i < generatorMaps.Count; i++)
			{
				IReader reader;
				KeyValuePair<ICacheFile, EngineDescription> cacheData = LoadMap(generatorMaps[i].LocalMapPath, pickedEngine, out reader);
				ICacheFile cacheFile = cacheData.Key;

				if (cacheFile.MetaArea == null || cacheFile.Tags.Count == 0)
					continue;

				using (StringWriter sw = new StringWriter())
				{
					foreach (ITag tag in cacheFile.Tags)
					{
						if (tag.MetaLocation == null)
							continue;

						sw.WriteLine($"{CharConstant.ToString(tag.Group.Magic)},{cacheFile.FileNames.GetTagName(tag.Index)}");
					}

					string filename = $"{cacheFile.InternalName}.txt";
					string path = Path.Combine(outputPath, filename);

					File.WriteAllText(path, sw.ToString());
				}
			}

			DateTime endTime = DateTime.Now;
			e.Result = endTime.Subtract(startTime);
		}


		private KeyValuePair<ICacheFile, EngineDescription> LoadMap(string path, EngineDescription pickedEngine, out IReader reader)
		{
			reader = new EndianReader(File.OpenRead(path), Endian.BigEndian);

			if (pickedEngine != null)
			{
				var cacheFile = CacheFileLoader.LoadCacheFileWithEngineDescription(reader, path, pickedEngine);
				return
					new KeyValuePair<ICacheFile, EngineDescription>(cacheFile, pickedEngine);
			}
			else
			{
				var cacheFile = CacheFileLoader.LoadCacheFile(reader, path, App.AssemblyStorage.AssemblySettings.DefaultDatabase, out EngineDescription buildInfo);
				return
					new KeyValuePair<ICacheFile, EngineDescription>(cacheFile, buildInfo);
			}
		}

		public class MapEntry
		{
			public string MapName { get; set; }
			public string LocalMapPath { get; set; }
			public bool IsSelected { get; set; }
		}

		public class TagItem
		{
			public string Name { get; set; }
			public string Group { get; set; }

			public List<string> Maps = new List<string>();
		}
	}
}
