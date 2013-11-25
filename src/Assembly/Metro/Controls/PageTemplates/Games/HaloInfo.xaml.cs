using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components;
using Assembly.Metro.Dialogs;
using Assembly.Windows;
using AvalonDock.Layout;
using Blamite.Blam.ThirdGen;
using Blamite.Blam.ThirdGen.BLF;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
	/// <summary>
	///     Interaction logic for HaloInfo.xaml
	/// </summary>
	public partial class HaloInfo
    {
        private readonly LayoutDocument _tab;
		private readonly string _blfLocation;

        private ObservableCollection<LanguageEntry> _languageset;

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
            new LanguageEntry { Index = 8, Language = "Chinese (Traditional)", LanguageShort = "zh-CHS" },
            new LanguageEntry { Index = 9, Language = "Chinese (Simplified)", LanguageShort = "zh-CHS" },
            new LanguageEntry { Index = 10, Language = "Portuguese", LanguageShort = "pt" },
            new LanguageEntry { Index = 11, Language = "Polish", LanguageShort = "pl" },
        };

        private readonly ObservableCollection<LanguageEntry> _halo4languages = new ObservableCollection<LanguageEntry>
        {
            new LanguageEntry { Index = 0, Language = "English", LanguageShort = "en" },
            new LanguageEntry { Index = 1, Language = "Japanese", LanguageShort = "ja" },
            new LanguageEntry { Index = 2, Language = "German", LanguageShort = "de" },
            new LanguageEntry { Index = 3, Language = "French", LanguageShort = "fr" },
            new LanguageEntry { Index = 4, Language = "Spanish", LanguageShort = "es" },
            new LanguageEntry { Index = 5, Language = "Spanish (Latin American)", LanguageShort = "es" },
            new LanguageEntry { Index = 6, Language = "Italian", LanguageShort = "it" },
            new LanguageEntry { Index = 7, Language = "Korean", LanguageShort = "ko" },
            new LanguageEntry { Index = 8, Language = "Chinese (Traditional)", LanguageShort = "zh-CHS" },
            new LanguageEntry { Index = 9, Language = "Chinese (Simplified)", LanguageShort = "zh-CHS" },
            new LanguageEntry { Index = 10, Language = "Portuguese", LanguageShort = "pt" },
            new LanguageEntry { Index = 11, Language = "Polish", LanguageShort = "pl" },
            new LanguageEntry { Index = 12, Language = "Russian", LanguageShort = "ru" },
            new LanguageEntry { Index = 13, Language = "Danish", LanguageShort = "da" },
            new LanguageEntry { Index = 14, Language = "Finnish", LanguageShort = "fi" },
            new LanguageEntry { Index = 15, Language = "Dutch", LanguageShort = "nl" },
            new LanguageEntry { Index = 16, Language = "Norwegian", LanguageShort = "nb" }
        };

		private PureBLF _blf;
		private MapInfo _mapInfo;
		private bool _startEditing;
		private int oldLanguage = -1;

		public HaloInfo(string infoLocation, LayoutDocument tab)
		{
			InitializeComponent();
			_blfLocation = infoLocation;

            var fi = new FileInfo(_blfLocation);
            _tab = tab;
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
					paneBLFInfo.Children.Insert(0, new MapHeaderEntry("BLF Length:", "0x" + _mapInfo.Stream.Length.ToString("X8")));
					paneBLFInfo.Children.Insert(1,
						new MapHeaderEntry("BLF Chunks:", _blf.BLFChunks.Count.ToString(CultureInfo.InvariantCulture)));

                    // Load Languages
                    LoadLanguages();

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

                    // Set up the Type combo box
                    switch (_mapInfo.MapInformation.Game)
                    {
                        case MapInfo.GameIdentifier.Halo3:
                        case MapInfo.GameIdentifier.Halo3ODST:
                            cbType_Cine.Visibility = System.Windows.Visibility.Collapsed;
                            cbType_Cine.IsEnabled = false;
                            cbType_FF.Visibility = System.Windows.Visibility.Collapsed;
                            cbType_FF.IsEnabled = false;
                            break;
                        case MapInfo.GameIdentifier.HaloReach:
                        case MapInfo.GameIdentifier.HaloReachBetas:
                            cbType_Cine.Visibility = System.Windows.Visibility.Visible;
                            cbType_Cine.IsEnabled = true;
                            cbType_FF.Visibility = System.Windows.Visibility.Visible;
                            cbType_FF.IsEnabled = true;
                            cbType_FF.Content = "Firefight";
                            break;
                        case MapInfo.GameIdentifier.Halo4:
                            cbType_Cine.Visibility = System.Windows.Visibility.Visible;
                            cbType_Cine.IsEnabled = true;
                            cbType_FF.Visibility = System.Windows.Visibility.Visible;
                            cbType_FF.IsEnabled = true;
                            cbType_FF.Content = "Spartan Ops";
                            break;
                    }
                    if (_mapInfo.MapInformation.Flags.HasFlag(LevelFlags.IsMainMenu))
                        cbType.SelectedIndex = 0;
                    if (_mapInfo.MapInformation.Flags.HasFlag(LevelFlags.IsMultiplayer))
                        cbType.SelectedIndex = 1;
                    if (_mapInfo.MapInformation.Flags.HasFlag(LevelFlags.IsCampaign))
                        cbType.SelectedIndex = 2;
                    if (_mapInfo.MapInformation.Flags.HasFlag(LevelFlags.IsCinematic))
                        cbType.SelectedIndex = 3;
                    if (_mapInfo.MapInformation.Flags.HasFlag(LevelFlags.IsFirefight))
                        cbType.SelectedIndex = 4;

                    // Set up the Checkboxes
                    switch (_mapInfo.MapInformation.Game)
                    {
                        case MapInfo.GameIdentifier.Halo4:
                            cbDLC.Margin = new Thickness(112, 0, 0, 0);
                            cbForgeOnly.Visibility = System.Windows.Visibility.Visible;
                            break;
                        default:
                            cbDLC.Margin = new Thickness(150, 0, 0, 0);
                            cbForgeOnly.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                    }
                    if (_mapInfo.MapInformation.Flags.HasFlag(LevelFlags.Visible))
                        cbVisible.IsChecked = true;
                    if (_mapInfo.MapInformation.Flags.HasFlag(LevelFlags.GeneratesFilm))
                        cbGeneratesFilm.IsChecked = true;
                    if (_mapInfo.MapInformation.Flags.HasFlag(LevelFlags.IsDLC))
                        cbDLC.IsChecked = true;
                    if (_mapInfo.MapInformation.Flags.HasFlag(LevelFlags.IsForgeOnly))
                        cbForgeOnly.IsChecked = true;

					// Update UI
					_startEditing = true;
					cbLanguages.SelectedIndex = 0;

					if (App.AssemblyStorage.AssemblySettings.StartpageHideOnLaunch)
						App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(Home.TabGenre.StartPage);

					RecentFiles.AddNewEntry(new FileInfo(_blfLocation).Name, _blfLocation, "Map Info", Settings.RecentFileType.MapInfo);
                    Close();
				}));
			}
			catch (Exception ex)
			{
				Dispatcher.Invoke(new Action(delegate
				{
                    MetroMessageBox.Show("Unable to open MapInfo", ex.Message);
                    App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(_tab);
                    Close();
				}));
			}
		}

		/// <summary>
		///     Close stuff
		/// </summary>
		public bool Close()
		{
			try
			{
				_mapInfo.Close();
			}
			catch
			{
			}
			return true;
		}

		// Validate MapID
		private void txtMapID_TextChanged(object sender, TextChangedEventArgs e)
		{
			Int32 tmp32;
			if (Int32.TryParse(txtMapID.Text, out tmp32))
				txtMapID.BorderBrush = (Brush) new BrushConverter().ConvertFromString("#FF595959");
			else
				txtMapID.BorderBrush = (Brush) FindResource("ExtryzeAccentBrush");
		}

		// Update UI from textbox
		private void txtMapInternalName_TextChanged(object sender, TextChangedEventArgs e)
		{
			lblBLFNameFooter.Text = lblBLFname.Text = txtMapInternalName.Text;
		}

		// Update Languages

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
						_mapInfo.MapInformation.MapDescriptions[oldLanguage] =
							_mapInfo.MapInformation.MapDescriptions[oldLanguage].Remove(126);
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
            _mapInfo = new MapInfo(_blfLocation);
			// Update MapID
			if (!Equals(txtMapID.BorderBrush, FindResource("ExtryzeAccentBrush")))
				_mapInfo.MapInformation.MapID = Int32.Parse(txtMapID.Text);

            // Check if MapID was invalid, if so tell user.
            if (Equals(txtMapID.BorderBrush, FindResource("ExtryzeAccentBrush")))
            {
                MetroMessageBox.Show("MapID Not Saved",
                    "The MapID was not saved into the MapInfo. Change the MapID to a valid number, then save again.");
                return;
            }
			// Update Internal Name
			_mapInfo.MapInformation.InternalName = txtMapInternalName.Text;

			// Update Physical Name
			_mapInfo.MapInformation.PhysicalName = txtMapPhysicalName.Text;

			// Update Current Map Name/Descrption Language Selection
			_mapInfo.MapInformation.MapNames[cbLanguages.SelectedIndex] = txtMapName.Text;
			_mapInfo.MapInformation.MapDescriptions[cbLanguages.SelectedIndex] = txtMapDesc.Text;

            // Update Flags
            _mapInfo.MapInformation.Flags = LevelFlags.None;
            if (cbType.SelectedIndex == 0)
                _mapInfo.MapInformation.Flags |= LevelFlags.IsMainMenu;
            if (cbType.SelectedIndex == 1)
                _mapInfo.MapInformation.Flags |= LevelFlags.IsMultiplayer;
            if (cbType.SelectedIndex == 2)
                _mapInfo.MapInformation.Flags |= LevelFlags.IsCampaign;
            if (cbType.SelectedIndex == 4)
                _mapInfo.MapInformation.Flags |= LevelFlags.IsFirefight;
            if (cbType.SelectedIndex == 3)
                _mapInfo.MapInformation.Flags |= LevelFlags.IsCinematic;
            if (cbVisible.IsChecked == true)
                _mapInfo.MapInformation.Flags |= LevelFlags.Visible;
            if (cbGeneratesFilm.IsChecked == true)
                _mapInfo.MapInformation.Flags |= LevelFlags.GeneratesFilm;
            if (cbDLC.IsChecked == true)
                _mapInfo.MapInformation.Flags |= LevelFlags.IsDLC;
            if (cbForgeOnly.IsChecked == true)
                _mapInfo.MapInformation.Flags |= LevelFlags.IsForgeOnly;

			// Write all changes to file
            _mapInfo.UpdateMapInfo();
            Close();
            MetroMessageBox.Show("Save Successful", "Your MapInfo has been saved.");
            App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(_tab);
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
        private void LoadLanguages()
        {
            // If the game is Halo 4, use that set of languages, if not, use the default set.
            switch (_mapInfo.MapInformation.Game)
            {
                case MapInfo.GameIdentifier.Halo4:
                    _languageset = _halo4languages;
                    break;
                default:
                    _languageset = _languages;
                    break;
            }
            cbLanguages.DataContext = _languageset;
        }

		public class LanguageEntry
		{
			public string Language { get; set; }
			public int Index { get; set; }
			public string LanguageShort { get; set; }
		}
	}
}