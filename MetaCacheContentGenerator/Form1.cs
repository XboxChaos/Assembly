using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.ThirdGen.BLF;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MetaCacheContentGenerator
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private readonly string[] blfBlockList = new[]
												{
													"mainmenu",
													"term_",
													"m70_a"
												};
		private void btnAddEntry_Click(object sender, EventArgs e)
		{
			var lvi = new ListViewItem { Text = txtTargets.Text };
			lvi.SubItems.Add(new ListViewItem.ListViewSubItem { Text = txtMapInfoPath.Text });
			lvi.SubItems.Add(new ListViewItem.ListViewSubItem { Text = txtBlfPath.Text });
			lvi.Tag = new MetaItem
				          {
							  Targets = txtTargets.Text,
							  MapInfoPath = txtMapInfoPath.Text,
							  BlfPath = txtBlfPath.Text
				          };

			lvList.Items.Add(lvi);
		}
		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var selectedItem = lvList.SelectedItems[0];
			if (selectedItem != null)
			{
				lvList.Items.Remove(selectedItem);
			}
		}

		public class MetaContent
		{
			public MetaContent()
			{
				Games = new List<GameEntry>();
			}

			public string Type { get; set; }
			public double GeneratedTimestamp { get; set; }
			public List<GameEntry> Games { get; set; }

			public class GameEntry
			{
				public GameEntry()
				{
					MetaData = new List<MetaDataEntry>();
				}

				public string Targets { get; set; }
				public List<MetaDataEntry> MetaData { get; set; }

				public class MetaDataEntry
				{
					public string English_Name { get; set; }
					public string English_Desc { get; set; }
					public string InternalName { get; set; }
					public string PhysicalName { get; set; }
					public int MapId { get; set; }
					public ImageMetaDataEntry ImageMetaData { get; set; }

					public class ImageMetaDataEntry
					{
						public string Large { get; set; }
						public string Small { get; set; }
					}
				}
			}
		}

		public class MetaItem
		{
			public string Targets { get; set; }
			public string MapInfoPath { get; set; }
			public string BlfPath { get; set; }
		}

		private void btnGenerate_Click(object sender, EventArgs e)
		{
			var metaItems = (from ListViewItem item in lvList.Items select (MetaItem) item.Tag).ToList();

			var worker = new BackgroundWorker();
			worker.DoWork += (o, args) => DoWork(worker, metaItems);
			worker.RunWorkerCompleted += worker_RunWorkerCompleted;
			worker.RunWorkerAsync();
		}

		private  void DoWork(BackgroundWorker worker, IEnumerable<MetaItem> items)
		{
			var rootFolder = Path.GetDirectoryName(Application.StartupPath);
			var metaOutput = new MetaContent()
				                 {
									 GeneratedTimestamp = ConvertToTimestamp(DateTime.Now),
									 Type = "cache_meta_content"
				                 };

			foreach(var item in items)
			{
				var gameEntry = new MetaContent.GameEntry { Targets = item.Targets };

				var gameTarget = item.Targets.Split('|')[0];
				var files = Directory.GetFiles(item.MapInfoPath, "*.mapinfo");
				foreach (var file in files)
				{
					var fi = new FileInfo(file);
					var continueProcessing = true;
					foreach (var block in blfBlockList.Where(block => fi.Name.StartsWith(block)))
						continueProcessing = false;

					if (!continueProcessing) continue;

					var mapInfo = new MapInfo(file);
					var metaEntry = new MetaContent.GameEntry.MetaDataEntry
						                {
							                English_Name = mapInfo.MapInformation.MapNames[0],
							                English_Desc = mapInfo.MapInformation.MapDescriptions[0],
							                InternalName = mapInfo.MapInformation.InternalName,
							                PhysicalName = mapInfo.MapInformation.PhysicalName,
							                MapId = mapInfo.MapInformation.MapID
						                };

					var extraSuffix = "";
					if (gameTarget.StartsWith("Halo4"))
						extraSuffix = "_card";

					var blfFile1 = new PureBLF(string.Format("{0}\\{1}{2}.blf", item.BlfPath, mapInfo.MapInformation.PhysicalName, extraSuffix));
					var blfFile = new List<byte>(blfFile1.BLFChunks[1].ChunkData);
					blfFile1.Close();
					blfFile.RemoveRange(0, 0x08);

					var blfFileSmall1 =
						new PureBLF(string.Format("{0}\\{1}_sm.blf", item.BlfPath, mapInfo.MapInformation.PhysicalName));
					var blfFileSmall = new List<byte>(blfFileSmall1.BLFChunks[1].ChunkData);
					blfFileSmall1.Close();
					blfFileSmall.RemoveRange(0, 0x08);

					if (!Directory.Exists(string.Format("{0}\\{1}\\maps\\", rootFolder, gameTarget)))
						Directory.CreateDirectory(string.Format("{0}\\{1}\\maps\\", rootFolder, gameTarget));

					File.WriteAllBytes(
						string.Format("{0}\\{1}\\maps\\{2}.jpg", rootFolder, gameTarget, mapInfo.MapInformation.PhysicalName),
						blfFile.ToArray<byte>());
					File.WriteAllBytes(
						string.Format("{0}\\{1}\\maps\\{2}_small.jpg", rootFolder, gameTarget, mapInfo.MapInformation.PhysicalName),
						blfFileSmall.ToArray<byte>());
					metaEntry.ImageMetaData = new MetaContent.GameEntry.MetaDataEntry.ImageMetaDataEntry
						                          {
							                          Large =
								                          string.Format("{0}/maps/{1}.jpg", gameTarget, mapInfo.MapInformation.PhysicalName),
							                          Small =
								                          string.Format("{0}/maps/{1}_small.jpg", gameTarget,
								                                        mapInfo.MapInformation.PhysicalName)
						                          };
					gameEntry.MetaData.Add(metaEntry);
				}
				metaOutput.Games.Add(gameEntry);
			}

			File.WriteAllText(string.Format("{0}/content.aidf", rootFolder), JsonConvert.SerializeObject(metaOutput));
		}
		void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			MessageBox.Show("Done!");
		}

		private double ConvertToTimestamp(DateTime value)
		{
			var span = (value - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
			return span.TotalSeconds;
		}
	}
}