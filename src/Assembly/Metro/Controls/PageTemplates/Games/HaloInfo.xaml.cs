using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;
using Blamite.Blam.ThirdGen;
using Blamite.Blam.ThirdGen.BLF;
using AvalonDock.Layout;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
	/// <summary>
	/// Interaction logic for HaloInfo.xaml
	/// </summary>
	public partial class HaloInfo
	{
		private readonly string _blfLocation;
		private PureBLF _blf;
		private MapInfo _mapInfo;
		private bool _startEditing;
		private readonly ObservableCollection<LanguageEntry> _languages = new ObservableCollection<LanguageEntry>
        {
            new LanguageEntry { Index = 0, Language = "English", LanguageShort = "en" },
            new LanguageEntry { Index = 1, Language = "Japanese", LanguageShort = "ja" },
            new LanguageEntry { Index = 2, Language = "German", LanguageShort = "de" },
            new LanguageEntry { Index = 3, Language = "French", LanguageShort = "fr" },
            new LanguageEntry { Index = 4, Language = "Spanish", LanguageShort = "es" },
            new LanguageEntry { Index = 5, Language = "Spanish (Latin American)", LanguageShort = "es" },
            new LanguageEntry { Index = 6, Language = "Italian", LanguageShort = "it" },
            new LanguageEntry { Index = 7, Language = "Korean", LanguageShort = "ko" },
            new LanguageEntry { Index = 8, Language = "Chinese", LanguageShort = "zh-CHS" },
            new LanguageEntry { Index = 9, Language = "Portuguese", LanguageShort = "pt" }
        };

		public HaloInfo(string infoLocation, LayoutDocument tab)
		{
			InitializeComponent();
			_blfLocation = infoLocation;

			var fi = new FileInfo(_blfLocation);
			tab.Title = fi.Name;
			lblBLFname.Text = fi.Name;

			var thrd = new Thread(LoadMapInfo);
			thrd.SetApartmentState(ApartmentState.STA);
			thrd.Start();
		}

		public void LoadMapInfo()
		{
			try
			{
				// Just a lazy way to validate the BLF file
				_blf = new PureBLF(_blfLocation);
				if (_blf.BLFChunks[1].ChunkMagic != "levl")
					throw new Exception("The selected Map Info BLF is not a valid Map Info BLF file.");
				_blf.Close();

				_mapInfo = new MapInfo(_blfLocation);

				Dispatcher.Invoke(new Action(delegate
				{
					// Add BLF Info
					paneBLFInfo.Children.Insert(0, new Components.MapHeaderEntry("BLF Length:", "0x" + _mapInfo.Stream.Length.ToString("X8")));
					paneBLFInfo.Children.Insert(1, new Components.MapHeaderEntry("BLF Chunks:", _blf.BLFChunks.Count.ToString(CultureInfo.InvariantCulture)));

					// Load Languages
					LoadLanguages(_mapInfo.MapInformation.Game);

					// Add Map Info
					switch (_mapInfo.MapInformation.Game)
					{
						case MapInfo.GameIdentifier.Halo3:
							txtGameName.Text = "Halo 3";
							break;
						case MapInfo.GameIdentifier.Halo3ODST:
							txtGameName.Text = "Halo 3: ODST";
							break;
						case MapInfo.GameIdentifier.HaloReach:
							txtGameName.Text = "Halo Reach";
							break;
						case MapInfo.GameIdentifier.HaloReachBetas:
							txtGameName.Text = "Halo Reach Pre/Beta";
							break;
						case MapInfo.GameIdentifier.Halo4:
							txtGameName.Text = "Halo 4";
							break;
					}
					txtMapID.Text = _mapInfo.MapInformation.MapID.ToString(CultureInfo.InvariantCulture);
					lblBLFNameFooter.Text = lblBLFname.Text = txtMapInternalName.Text = _mapInfo.MapInformation.InternalName;
					txtMapPhysicalName.Text = _mapInfo.MapInformation.PhysicalName;

					// Update UI
					_startEditing = true;
					cbLanguages.SelectedIndex = 0;

					if (App.AssemblyStorage.AssemblySettings.StartpageHideOnLaunch)
						App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(Windows.Home.TabGenre.StartPage);

					RecentFiles.AddNewEntry(new FileInfo(_blfLocation).Name, _blfLocation, "Map Info", Settings.RecentFileType.MapInfo);
				}));
			}
			catch (Exception ex)
			{
				Dispatcher.Invoke(new Action(delegate
				{
					MetroMessageBox.Show("Unable to open MapInfo", ex.Message);
					App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose((LayoutDocument)Parent);
				}));
			}
		}

		/// <summary>
		/// Close stuff
		/// </summary>
		public bool Close() { try { _mapInfo.Close(); } catch { } return true; }

		// Validate MapID
		private void txtMapID_TextChanged(object sender, TextChangedEventArgs e)
		{
			Int32 tmp32;
			if (Int32.TryParse(txtMapID.Text, out tmp32))
				txtMapID.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF595959");
			else
				txtMapID.BorderBrush = (Brush)FindResource("ExtryzeAccentBrush");
		}

		// Update UI form textbox
		private void txtMapInternalName_TextChanged(object sender, TextChangedEventArgs e)
		{
			lblBLFNameFooter.Text = lblBLFname.Text = txtMapInternalName.Text;
		}

		// Update Languages
		private int oldLanguage = -1;
		private void cbLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_mapInfo != null && _startEditing)
			{
				if (oldLanguage != -1)
				{
					// Save values to memory
					_mapInfo.MapInformation.MapNames[oldLanguage] = txtMapName.Text.Trim();
					_mapInfo.MapInformation.MapDescriptions[oldLanguage] = txtMapDesc.Text.Trim();

					// Make sure values arn't too long, kiddo
					if (_mapInfo.MapInformation.MapNames[oldLanguage].Length > 30)
						_mapInfo.MapInformation.MapNames[oldLanguage] = _mapInfo.MapInformation.MapNames[oldLanguage].Remove(30);
					if (_mapInfo.MapInformation.MapDescriptions[oldLanguage].Length > 126)
						_mapInfo.MapInformation.MapDescriptions[oldLanguage] = _mapInfo.MapInformation.MapDescriptions[oldLanguage].Remove(126);
				}

				// Update oldLanguage int
				oldLanguage = cbLanguages.SelectedIndex;

				// Update UI
				txtMapName.Text = _mapInfo.MapInformation.MapNames[oldLanguage];
				txtMapDesc.Text = _mapInfo.MapInformation.MapDescriptions[oldLanguage];
			}
		}

		// Update MapInfo file
		private void btnUpdate_Click(object sender, RoutedEventArgs e)
		{
			// Update MapID
			if (!Equals(txtMapID.BorderBrush, FindResource("ExtryzeAccentBrush")))
				_mapInfo.MapInformation.MapID = Int32.Parse(txtMapID.Text);

			// Update Internal Name
			_mapInfo.MapInformation.InternalName = txtMapInternalName.Text;

			// Update Physical Name
			_mapInfo.MapInformation.PhysicalName = txtMapPhysicalName.Text;

			// Update Current Map Name/Descrption Language Selection
			_mapInfo.MapInformation.MapNames[cbLanguages.SelectedIndex] = txtMapName.Text;
			_mapInfo.MapInformation.MapDescriptions[cbLanguages.SelectedIndex] = txtMapDesc.Text;

			// Write all changes to file
			_mapInfo.UpdateMapInfo();

			// Check if MapID was invalid, if so tell user.
			if (Equals(txtMapID.BorderBrush, FindResource("ExtryzeAccentBrush")))
				MetroMessageBox.Show("MapID Not Saved", "The MapID was not saved into the MapInfo. Change the MapID to a valid number, then save again.");
		}

		private void btnTranslateAllOthers_Click(object sender, RoutedEventArgs e)
		{
			//if (MetroMessageBox.Show("Are you sure?", "This will overide all other entries with this Map Name and Description, in the corrosponding language.", MetroMessageBox.MessageBoxButtons.YesNo) == MetroMessageBox.MessageBoxResult.Yes)
			//{
			//	foreach (LanguageEntry entry in cbLanguages.Items)
			//	{

			//	}
			//}
		}

		// Load Languages
		private void LoadLanguages(MapInfo.GameIdentifier gameIdent)
		{
			if (gameIdent == MapInfo.GameIdentifier.Halo4)
			{
				// TODO: Add the new Halo 4 Languages
			}

			cbLanguages.DataContext = _languages;
		}

		public class LanguageEntry
		{
			public string Language { get; set; }
			public int Index { get; set; }
			public string LanguageShort { get; set; }
		}
	}
}