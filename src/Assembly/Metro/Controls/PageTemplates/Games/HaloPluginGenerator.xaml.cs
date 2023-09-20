using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Xml;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;
using Blamite.Blam;
using Blamite.Blam.ThirdGen;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.Plugins.Generation;
using Blamite.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
	/// <summary>
	///     Interaction logic for PluginGenerator.xaml
	/// </summary>
	public partial class HaloPluginGenerator
	{
		public ObservableCollection<MapEntry> GeneratorMaps = new ObservableCollection<MapEntry>();
		private bool _isWorking;

		private string[] MapFilter = new string[] //maps that arent proper cache files.
			{
				"bitmaps.map",
				"loc.map",
				"sounds.map",
			};

		public HaloPluginGenerator()
		{
			InitializeComponent();
			DataContext = GeneratorMaps;
		}

		public bool Close()
		{
			return !_isWorking;
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
						fi => !MapFilter.Contains(fi.Name.ToLowerInvariant())))
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
			else
				fbd.SelectedPath = VariousFunctions.GetApplicationLocation() + "\\plugins\\";
			if (fbd.ShowDialog() == DialogResult.OK)
			{
				txtOutputFolder.Text = fbd.SelectedPath;
			}
		}


		private void btnGeneratePlugins_Click(object sender, RoutedEventArgs e)
		{
			// Check that all needed info is loaded
			if (GeneratorMaps == null ||
			    ((String.IsNullOrEmpty(txtInputFolder.Text) || !Directory.Exists(txtInputFolder.Text)) ||
			     (String.IsNullOrEmpty(txtOutputFolder.Text) || !Directory.Exists(txtOutputFolder.Text)) ||
			     (GeneratorMaps.Count(entry => !entry.IsSelected) == GeneratorMaps.Count)))
			{
				MetroMessageBox.Show("Missing required information", "Required information for plugin generation is missing...");
				return;
			}

			StartGeneration();
		}

		private void StartGeneration()
		{
			_isWorking = true;

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
			_isWorking = false;

			btnInputFolder.IsEnabled =
				btnOutputFolder.IsEnabled =
					btnGeneratePlugins.IsEnabled = true;

			MaskingPage.Visibility = Visibility.Collapsed;
		}

		private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error == null)
				MetroMessageBox.Show("Plugin Generation Complete!", "Plugin generation completed successfully in " +
				                                                    Math.Round(((TimeSpan) e.Result).TotalSeconds, 3) + " seconds.");
			else
				MetroMessageBox.Show("Error Generating Plugins!", e.Error.ToString());

			EndGeneration();
		}

		private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			lblProgressStatus.Text = string.Format("Generating Plugins... ({0}%)", e.ProgressPercentage);
		}

		private void worker_DoWork(object sender, DoWorkEventArgs e, IList<MapEntry> generatorMaps, string outputPath,
			BackgroundWorker worker, EngineDescription pickedEngine)
		{
			var globalMaps = new Dictionary<string, MetaMap>();
			DateTime startTime = DateTime.Now;
			string gameIdentifier = "";
			worker.ReportProgress(0);

			for (int i = 0; i < generatorMaps.Count; i++)
			{
				var tagMaps = new Dictionary<ITag, MetaMap>();

				IReader reader;
				KeyValuePair<ICacheFile, EngineDescription> cacheData = LoadMap(generatorMaps[i].LocalMapPath, pickedEngine, out reader);
				ICacheFile cacheFile = cacheData.Key;

				if (cacheFile.MetaArea == null || cacheFile.Tags.Count == 0)
					continue;

				var analyzer = new MetaAnalyzer(cacheFile);
				if (gameIdentifier == "")
					gameIdentifier = cacheData.Value.Settings.GetSetting<string>("shortName");

				var mapsToProcess = new Queue<MetaMap>();
				foreach (ITag tag in cacheFile.Tags)
				{
					if (tag.MetaLocation == null)
						continue;

					var map = new MetaMap();
					tagMaps[tag] = map;
					mapsToProcess.Enqueue(map);

					reader.SeekTo(tag.MetaLocation.AsOffset());
					analyzer.AnalyzeArea(reader, tag.MetaLocation.AsPointer(), map);
				}
				GenerateSubMaps(mapsToProcess, analyzer, reader, cacheFile);

				var groupMaps = new Dictionary<string, MetaMap>();
				foreach (ITag tag in cacheFile.Tags)
				{
					if (tag.MetaLocation == null)
						continue;

					MetaMap map = tagMaps[tag];
					EstimateMapSize(map, tag.MetaLocation.AsPointer(), analyzer.GeneratedMemoryMap, 1);

					string magicStr = CharConstant.ToString(tag.Group.Magic);
					MetaMap oldGroupMap;
					if (groupMaps.TryGetValue(magicStr, out oldGroupMap))
						oldGroupMap.MergeWith(map);
					else
						groupMaps[magicStr] = map;
				}

				foreach (var map in groupMaps)
				{
					MetaMap globalMap;
					if (globalMaps.TryGetValue(map.Key, out globalMap))
						globalMap.MergeWith(map.Value);
					else
						globalMaps[map.Key] = map.Value;
				}

				reader.Dispose();

				worker.ReportProgress(100*(i+1)/(generatorMaps.Count));
			}

			string badChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
			foreach (var map in globalMaps)
			{
				string filename = badChars.Aggregate(map.Key, (current, badChar) => current.Replace(badChar, '_'));
				filename = filename.Replace(" ", "");
				filename += ".xml";
				string path = Path.Combine(outputPath, filename);

				var settings = new XmlWriterSettings
				{
					Indent = true,
					IndentChars = "\t"
				};
				using (XmlWriter writer = XmlWriter.Create(path, settings))
				{
					var pluginWriter = new AssemblyPluginWriter(writer, gameIdentifier);

					int size = map.Value.GetBestSizeEstimate();
					FoldSubMaps(map.Value);

					pluginWriter.EnterPlugin(size);

					pluginWriter.EnterRevisions();
					pluginWriter.VisitRevision(new PluginRevision("Assembly", 1, "Generated plugin from scratch."));
					pluginWriter.LeaveRevisions();

					WritePlugin(map.Value, size, pluginWriter);
					pluginWriter.LeavePlugin();
				}
			}

			DateTime endTime = DateTime.Now;
			e.Result = endTime.Subtract(startTime);
		}

		private void WritePlugin(MetaMap map, int size, IPluginVisitor writer)
		{
			for (int offset = 0; offset < size; offset += 4)
			{
				MetaValueGuess guess = map.GetGuess(offset);
				if (guess != null)
				{
					switch (guess.Type)
					{
						case MetaValueType.DataReference:
							if (offset <= size - 0x14)
							{
								writer.VisitDataReference("Unknown", (uint) offset, "bytes", false, 4, 0, "");
								offset += 0x10;
								continue;
							}
							break;

						case MetaValueType.TagReference:
							if (offset <= size - 0x10)
							{
								writer.VisitTagReference("Unknown", (uint) offset, false, true, 0, "");
								offset += 0xC;
								continue;
							}
							break;

						case MetaValueType.TagBlock:
							if (offset <= size - 0xC)
							{
								MetaMap subMap = map.GetSubMap(offset);
								if (subMap != null)
								{
									int subMapSize = subMap.GetBestSizeEstimate();
									writer.EnterTagBlock("Unknown", (uint) offset, false, (uint)subMapSize, 4, false, 0, "");
									WritePlugin(subMap, subMapSize, writer);
									writer.LeaveTagBlock();
									offset += 0x8;
									continue;
								}
							}
							break;
					}
				}

				// Just write an unknown value depending upon how much space we have left
				if (offset <= size - 4)
					writer.VisitUndefined("Unknown", (uint) offset, false, 0, "");
				else if (offset <= size - 2)
					writer.VisitInt16("Unknown", (uint) offset, false, 0, "");
				else
					writer.VisitInt8("Unknown", (uint) offset, false, 0, "");
			}
		}

		private void GenerateSubMaps(Queue<MetaMap> maps, MetaAnalyzer analyzer, IReader reader, ICacheFile cacheFile)
		{
			var generatedMaps = new Dictionary<long, MetaMap>();
			while (maps.Count > 0)
			{
				MetaMap map = maps.Dequeue();
				foreach (MetaValueGuess guess in map.Guesses.Where(guess => guess.Type == MetaValueType.TagBlock))
				{
					MetaMap subMap;
					if (!generatedMaps.TryGetValue(guess.Pointer, out subMap))
					{
						subMap = new MetaMap();
						reader.SeekTo(cacheFile.MetaArea.PointerToOffset(guess.Pointer));
						analyzer.AnalyzeArea(reader, guess.Pointer, subMap);
						maps.Enqueue(subMap);
						generatedMaps[guess.Pointer] = subMap;
					}
					map.AssociateSubMap(guess.Offset, subMap);
				}
			}
		}

		private void EstimateMapSize(MetaMap map, long mapAddress, MemoryMap memMap, int entryCount)
		{
			bool alreadyVisited = map.HasSizeEstimates;

			int newSize = memMap.EstimateBlockSize(mapAddress);
			map.EstimateSize(newSize/entryCount);
			map.Truncate(newSize);

			if (alreadyVisited) return;

			foreach (MetaValueGuess guess in map.Guesses)
			{
				if (guess.Type != MetaValueType.TagBlock) continue;

				MetaMap subMap = map.GetSubMap(guess.Offset);
				if (subMap != null)
					EstimateMapSize(subMap, guess.Pointer, memMap, (int) guess.Data1);
			}
		}

		private void FoldSubMaps(MetaMap map)
		{
			foreach (MetaValueGuess guess in map.Guesses)
			{
				if (guess.Type != MetaValueType.TagBlock) continue;

				MetaMap subMap = map.GetSubMap(guess.Offset);
				if (subMap == null) continue;

				//var entryCount = (int)guess.Data1;
				int firstBlockSize = subMap.GetBestSizeEstimate();
				//if (firstBlockSize > 0 && !subMap.IsFolded(firstBlockSize))
				//{
				subMap.Fold(firstBlockSize);
				FoldSubMaps(subMap);
				//}
			}
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
	}
}