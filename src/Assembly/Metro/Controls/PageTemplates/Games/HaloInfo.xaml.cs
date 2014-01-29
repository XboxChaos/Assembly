using System;
using System.Collections.Generic;
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

		public class InsertionPoint
		{
			public string ZoneName { get; set; }
			public byte ZoneIndex { get; set; }
			public bool IPEnabled { get; set; }
			public bool IPVisible { get; set; }
			public IList<string> IPNames { get; set; }
			public IList<string> IPDescriptions { get; set; }
		}

		private PureBLF _blf;
		private MapInfo _mapInfo;
		private bool _startEditing;
		public int insertionOldLanguage = -1;
		public int oldLanguage = -1;
		public int oldIndex = -1;
		public int insertionPointCount = 4;
		public string culpritTextString;
		public List<InsertionPoint> InsertionPointList = new List<InsertionPoint>();

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
					paneBLFInfo.Children.Insert(0, new MapHeaderEntry("BLF Length:", "0x" + _mapInfo.Stream.Length.ToString("X")));
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
						case MapInfo.GameIdentifier.Halo4NetworkTest:
							txtGameName.Text = "Halo 4 Network Test";
							break;
						case MapInfo.GameIdentifier.Halo4:
							txtGameName.Text = "Halo 4";
							break;
					}
					txtMapID.Text = _mapInfo.MapInformation.MapID.ToString(CultureInfo.InvariantCulture);
					txtMapInternalName.Text = _mapInfo.MapInformation.InternalName;
					txtMapPhysicalName.Text = _mapInfo.MapInformation.PhysicalName;

					// Set up the Game-Specific UI
					switch (_mapInfo.MapInformation.Game)
					{
						case MapInfo.GameIdentifier.Halo3:
						case MapInfo.GameIdentifier.Halo3ODST:
							cbType_Cine.Visibility = System.Windows.Visibility.Collapsed;
							cbType_Cine.IsEnabled = false;
							cbType_FF.Visibility = System.Windows.Visibility.Collapsed;
							cbType_FF.IsEnabled = false;
							lblDefaultAuthor.Visibility = System.Windows.Visibility.Collapsed;
							txtDefaultAuthor.Visibility = System.Windows.Visibility.Collapsed;
							tiMPObjects.Visibility = System.Windows.Visibility.Collapsed;
							break;
						case MapInfo.GameIdentifier.HaloReachBetas:
							lblDefaultAuthor.Visibility = System.Windows.Visibility.Collapsed;
							txtDefaultAuthor.Visibility = System.Windows.Visibility.Collapsed;
							tiMaxTeams.Visibility = System.Windows.Visibility.Collapsed;
							break;
						case MapInfo.GameIdentifier.HaloReach:
						case MapInfo.GameIdentifier.Halo4NetworkTest:
							lblDefaultAuthor.Margin = new Thickness(0, 37, 0, 3);
							tiMaxTeams.Visibility = System.Windows.Visibility.Collapsed;
							break;
						case MapInfo.GameIdentifier.Halo4:
							cbType_FF.Content = "Spartan Ops";
							tiMaxTeams.Visibility = System.Windows.Visibility.Collapsed;
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
							cbForgeOnly.Visibility = System.Windows.Visibility.Visible;
							break;
						default:
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

					txtDefaultAuthor.Text = _mapInfo.MapInformation.DefaultAuthor;

					// Load Max Teams
					txtTeamsNone.Text = _mapInfo.MapInformation.MaxTeamsNone.ToString();
					txtTeamsCTF.Text = _mapInfo.MapInformation.MaxTeamsCTF.ToString();
					txtTeamsSlayer.Text = _mapInfo.MapInformation.MaxTeamsSlayer.ToString();
					txtTeamsOddball.Text = _mapInfo.MapInformation.MaxTeamsOddball.ToString();
					txtTeamsKOTH.Text = _mapInfo.MapInformation.MaxTeamsKOTH.ToString();
					txtTeamsEditor.Text = _mapInfo.MapInformation.MaxTeamsEditor.ToString();
					txtTeamsVIP.Text = _mapInfo.MapInformation.MaxTeamsVIP.ToString();
					txtTeamsJuggernaut.Text = _mapInfo.MapInformation.MaxTeamsJuggernaut.ToString();
					txtTeamsTerritories.Text = _mapInfo.MapInformation.MaxTeamsTerritories.ToString();
					txtTeamsAssault.Text = _mapInfo.MapInformation.MaxTeamsAssault.ToString();
					txtTeamsInfection.Text = _mapInfo.MapInformation.MaxTeamsInfection.ToString();

					// Load Multiplayer Objects
					LoadMPObjects();

					// Load Insertion Points
					LoadInsertionPoints();

					// Update UI
					_startEditing = true;
					cbLanguages.SelectedIndex = 0;
					cbInsertIndex.SelectedIndex = 0;
					cbInsertLanguages.SelectedIndex = 0;

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
					MetroException.Show(ex);
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
				txtMapID.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF595959");
			else
				txtMapID.BorderBrush = (Brush)FindResource("ExtryzeAccentBrush");
		}

		// Halo 3/ODST: Validate Zone Index Max Teams
		private void CheckTextAsByte(TextBox textbox)
		{
			Byte tmp8;
			if (Byte.TryParse(textbox.Text, out tmp8))
				textbox.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF595959");
			else
				textbox.BorderBrush = (Brush)FindResource("ExtryzeAccentBrush");
		}

		private void txtZoneIndex_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtZoneIndex);
		}

		private void txtTeamsNone_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtTeamsNone);
		}

		private void txtTeamsCTF_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtTeamsCTF);
		}

		private void txtTeamsSlayer_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtTeamsSlayer);
		}

		private void txtTeamsOddball_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtTeamsOddball);
		}

		private void txtTeamsKOTH_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtTeamsKOTH);
		}

		private void txtTeamsEditor_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtTeamsEditor);
		}

		private void txtTeamsVIP_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtTeamsVIP);
		}

		private void txtTeamsJuggernaut_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtTeamsJuggernaut);
		}

		private void txtTeamsTerritories_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtTeamsTerritories);
		}

		private void txtTeamsAssault_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtTeamsAssault);
		}


		private void txtTeamsInfection_TextChanged(object sender, TextChangedEventArgs e)
		{
			CheckTextAsByte(txtTeamsInfection);
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
					if (_mapInfo.MapInformation.MapNames[oldLanguage].Length > 32)
						_mapInfo.MapInformation.MapNames[oldLanguage] = _mapInfo.MapInformation.MapNames[oldLanguage].Remove(32);
					if (_mapInfo.MapInformation.MapDescriptions[oldLanguage].Length > 128)
						_mapInfo.MapInformation.MapDescriptions[oldLanguage] =
							_mapInfo.MapInformation.MapDescriptions[oldLanguage].Remove(128);
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
			int txtErrorCount = 0;
			_mapInfo = new MapInfo(_blfLocation);
			// Check if the Map ID, Current Zone Index, and Max Team Counts are Valid
			txtErrorCount += CheckTextBoxValidity(txtMapID, "Map ID");
			txtErrorCount += CheckTextBoxValidity(txtZoneIndex, ("Insertion Point " + cbInsertIndex.SelectedIndex.ToString() + ": Zone Index"));
			txtErrorCount += CheckTextBoxValidity(txtTeamsNone, "No GameType Max Teams");
			txtErrorCount += CheckTextBoxValidity(txtTeamsCTF, "CTF Max Teams");
			txtErrorCount += CheckTextBoxValidity(txtTeamsSlayer, "Slayer Max Teams");
			txtErrorCount += CheckTextBoxValidity(txtTeamsOddball, "Oddball Max Teams");
			txtErrorCount += CheckTextBoxValidity(txtTeamsKOTH, "KOTH Max Teams");
			txtErrorCount += CheckTextBoxValidity(txtTeamsEditor, "Editor Max Teams");
			txtErrorCount += CheckTextBoxValidity(txtTeamsVIP, "VIP Max Teams");
			txtErrorCount += CheckTextBoxValidity(txtTeamsJuggernaut, "Juggernaut Max Teams");
			txtErrorCount += CheckTextBoxValidity(txtTeamsTerritories, "Territories Max Teams");
			txtErrorCount += CheckTextBoxValidity(txtTeamsAssault, "Assault Max Teams");
			txtErrorCount += CheckTextBoxValidity(txtTeamsInfection, "Infection Max Teams");

			if (txtErrorCount == 1)
			{
				Close();
				MetroMessageBox.Show("MapInfo Not Saved", (culpritTextString + " was either not a valid number or too large."));
				return;
			}

			if (txtErrorCount > 1)
			{
				Close();
				MetroMessageBox.Show("MapInfo Not Saved", "Multiple number inputs were either invalid numbers or too large.");
				return;
			}

			// If all valid, Update
			_mapInfo.MapInformation.MapID = Int32.Parse(txtMapID.Text);
			_mapInfo.MapInformation.MaxTeamsNone = Byte.Parse(txtTeamsNone.Text);
			_mapInfo.MapInformation.MaxTeamsCTF = Byte.Parse(txtTeamsCTF.Text);
			_mapInfo.MapInformation.MaxTeamsSlayer = Byte.Parse(txtTeamsSlayer.Text);
			_mapInfo.MapInformation.MaxTeamsOddball = Byte.Parse(txtTeamsOddball.Text);
			_mapInfo.MapInformation.MaxTeamsKOTH = Byte.Parse(txtTeamsKOTH.Text);
			_mapInfo.MapInformation.MaxTeamsEditor = Byte.Parse(txtTeamsEditor.Text);
			_mapInfo.MapInformation.MaxTeamsVIP = Byte.Parse(txtTeamsVIP.Text);
			_mapInfo.MapInformation.MaxTeamsJuggernaut = Byte.Parse(txtTeamsJuggernaut.Text);
			_mapInfo.MapInformation.MaxTeamsTerritories = Byte.Parse(txtTeamsTerritories.Text);
			_mapInfo.MapInformation.MaxTeamsAssault = Byte.Parse(txtTeamsAssault.Text);
			_mapInfo.MapInformation.MaxTeamsInfection = Byte.Parse(txtTeamsInfection.Text);

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

			// Update MP Objects
			if (_mapInfo.MapInformation.Game != MapInfo.GameIdentifier.Halo3 && _mapInfo.MapInformation.Game != MapInfo.GameIdentifier.Halo3ODST)
				UpdateMPObjects();

			// Update Insertion Points
			UpdateInsertionPoints();

			// Update Default Author
			_mapInfo.MapInformation.DefaultAuthor = txtDefaultAuthor.Text;

			// Write all changes to file
			_mapInfo.UpdateMapInfo();
			Close();
			MetroMessageBox.Show("Save Successful", "Your MapInfo has been saved.");
			App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(_tab);
		}

		private int CheckTextBoxValidity(TextBox textbox, string culprit)
		{
			int ValidityCheck = 0;

			// Check if the count was invalid, if so tell user.
			if (Equals(textbox.BorderBrush, FindResource("ExtryzeAccentBrush")))
			{
				culpritTextString = culprit;
				ValidityCheck = 1;
			}
			return ValidityCheck;
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
			cbInsertLanguages.DataContext = _languageset;
		}

		private void LoadMPObjects()
		{
			if (_mapInfo.MapInformation.Game != MapInfo.GameIdentifier.Halo3 && _mapInfo.MapInformation.Game != MapInfo.GameIdentifier.Halo3ODST)
			{
				// Create a checkbox for each item
				IList<CheckBox> CheckBoxes = new List<CheckBox>();
				CheckBox cb_spartan = new CheckBox();
				cb_spartan.Content = "spartan";
				CheckBoxes.Add(cb_spartan);
				CheckBox cb_elite = new CheckBox();
				cb_elite.Content = "elite";
				CheckBoxes.Add(cb_elite);
				CheckBox cb_monitor = new CheckBox();
				cb_monitor.Content = "monitor";
				CheckBoxes.Add(cb_monitor);
				CheckBox cb_flag = new CheckBox();
				cb_flag.Content = "flag";
				CheckBoxes.Add(cb_flag);
				CheckBox cb_bomb = new CheckBox();
				cb_bomb.Content = "bomb";
				CheckBoxes.Add(cb_bomb);
				CheckBox cb_ball = new CheckBox();
				cb_ball.Content = "ball";
				CheckBoxes.Add(cb_ball);
				CheckBox cb_area = new CheckBox();
				cb_area.Content = "area";
				CheckBoxes.Add(cb_area);
				CheckBox cb_stand = new CheckBox();
				cb_stand.Content = "stand";
				CheckBoxes.Add(cb_stand);
				CheckBox cb_destination = new CheckBox();
				cb_destination.Content = "destination";
				CheckBoxes.Add(cb_destination);
				CheckBox cb_frag_grenade = new CheckBox();
				cb_frag_grenade.Content = "frag__grenade";
				CheckBoxes.Add(cb_frag_grenade);
				CheckBox cb_plasma_grenade = new CheckBox();
				cb_plasma_grenade.Content = "plasma__grenade";
				CheckBoxes.Add(cb_plasma_grenade);
				CheckBox cb_spike_grenade = new CheckBox();
				cb_spike_grenade.Content = "spike__grenade";
				CheckBoxes.Add(cb_spike_grenade);
				CheckBox cb_firebomb_grenade = new CheckBox();
				cb_firebomb_grenade.Content = "firebomb__grenade";
				CheckBoxes.Add(cb_firebomb_grenade);
				CheckBox cb_dmr = new CheckBox();
				if (_mapInfo.MapInformation.Game == MapInfo.GameIdentifier.HaloReachBetas)
					cb_dmr.Content = "br";
				else
					cb_dmr.Content = "dmr";
				CheckBoxes.Add(cb_dmr);
				CheckBox cb_assault_rifle = new CheckBox();
				cb_assault_rifle.Content = "assault__rifle";
				CheckBoxes.Add(cb_assault_rifle);
				CheckBox cb_plasma_pistol = new CheckBox();
				cb_plasma_pistol.Content = "plasma__pistol";
				CheckBoxes.Add(cb_plasma_pistol);
				CheckBox cb_spike_rifle = new CheckBox();
				cb_spike_rifle.Content = "spike__rifle";
				CheckBoxes.Add(cb_spike_rifle);
				CheckBox cb_smg = new CheckBox();
				cb_smg.Content = "smg";
				CheckBoxes.Add(cb_smg);
				CheckBox cb_needle_rifle = new CheckBox();
				cb_needle_rifle.Content = "needle__rifle";
				CheckBoxes.Add(cb_needle_rifle);
				CheckBox cb_plasma_repeater = new CheckBox();
				cb_plasma_repeater.Content = "plasma__repeater";
				CheckBoxes.Add(cb_plasma_repeater);
				CheckBox cb_energy_sword = new CheckBox();
				cb_energy_sword.Content = "energy__sword";
				CheckBoxes.Add(cb_energy_sword);
				CheckBox cb_magnum = new CheckBox();
				cb_magnum.Content = "magnum";
				CheckBoxes.Add(cb_magnum);
				CheckBox cb_needler = new CheckBox();
				cb_needler.Content = "needler";
				CheckBoxes.Add(cb_needler);
				CheckBox cb_plasma_rifle = new CheckBox();
				cb_plasma_rifle.Content = "plasma__rifle";
				CheckBoxes.Add(cb_plasma_rifle);
				CheckBox cb_rocket_launcher = new CheckBox();
				cb_rocket_launcher.Content = "rocket__launcher";
				CheckBoxes.Add(cb_rocket_launcher);
				CheckBox cb_shotgun = new CheckBox();
				cb_shotgun.Content = "shotgun";
				CheckBoxes.Add(cb_shotgun);
				CheckBox cb_sniper_rifle = new CheckBox();
				cb_sniper_rifle.Content = "sniper__rifle";
				CheckBoxes.Add(cb_sniper_rifle);
				CheckBox cb_brute_shot = new CheckBox();
				cb_brute_shot.Content = "brute__shot";
				CheckBoxes.Add(cb_brute_shot);
				CheckBox cb_beam_rifle = new CheckBox();
				cb_beam_rifle.Content = "beam__rifle";
				CheckBoxes.Add(cb_beam_rifle);
				CheckBox cb_spartan_laser = new CheckBox();
				cb_spartan_laser.Content = "spartan__laser";
				CheckBoxes.Add(cb_spartan_laser);
				CheckBox cb_gravity_hammer = new CheckBox();
				cb_gravity_hammer.Content = "gravity__hammer";
				CheckBoxes.Add(cb_gravity_hammer);
				CheckBox cb_mauler = new CheckBox();
				cb_mauler.Content = "mauler";
				CheckBoxes.Add(cb_mauler);
				CheckBox cb_flamethrower = new CheckBox();
				cb_flamethrower.Content = "flamethrower";
				CheckBoxes.Add(cb_flamethrower);
				CheckBox cb_missle_pod = new CheckBox();
				cb_missle_pod.Content = "missle__pod";
				CheckBoxes.Add(cb_missle_pod);
				CheckBox cb_warthog = new CheckBox();
				cb_warthog.Content = "warthog";
				CheckBoxes.Add(cb_warthog);
				CheckBox cb_ghost = new CheckBox();
				cb_ghost.Content = "ghost";
				CheckBoxes.Add(cb_ghost);
				CheckBox cb_scorpion = new CheckBox();
				cb_scorpion.Content = "scorpion";
				CheckBoxes.Add(cb_scorpion);
				CheckBox cb_wraith = new CheckBox();
				cb_wraith.Content = "wraith";
				CheckBoxes.Add(cb_wraith);
				CheckBox cb_banshee = new CheckBox();
				cb_banshee.Content = "banshee";
				CheckBoxes.Add(cb_banshee);
				CheckBox cb_mongoose = new CheckBox();
				cb_mongoose.Content = "mongoose";
				CheckBoxes.Add(cb_mongoose);
				CheckBox cb_chopper = new CheckBox();
				cb_chopper.Content = "chopper";
				CheckBoxes.Add(cb_chopper);
				CheckBox cb_prowler = new CheckBox();
				cb_prowler.Content = "prowler";
				CheckBoxes.Add(cb_prowler);
				CheckBox cb_hornet = new CheckBox();
				cb_hornet.Content = "hornet";
				CheckBoxes.Add(cb_hornet);
				CheckBox cb_stingray = new CheckBox();
				cb_stingray.Content = "stingray";
				CheckBoxes.Add(cb_stingray);
				CheckBox cb_heavy_wraith = new CheckBox();
				cb_heavy_wraith.Content = "heavy__wraith";
				CheckBoxes.Add(cb_heavy_wraith);
				CheckBox cb_falcon = new CheckBox();
				cb_falcon.Content = "falcon";
				CheckBoxes.Add(cb_falcon);
				CheckBox cb_sabre = new CheckBox();
				cb_sabre.Content = "sabre";
				CheckBoxes.Add(cb_sabre);
				CheckBox cb_sprint_equipment = new CheckBox();
				cb_sprint_equipment.Content = "sprint__equipment";
				CheckBoxes.Add(cb_sprint_equipment);
				CheckBox cb_jet_pack_equipment = new CheckBox();
				cb_jet_pack_equipment.Content = "jet__pack__equipment";
				CheckBoxes.Add(cb_jet_pack_equipment);
				CheckBox cb_armor_lock_equipment = new CheckBox();
				cb_armor_lock_equipment.Content = "armor__lock__equipment";
				CheckBoxes.Add(cb_armor_lock_equipment);
				CheckBox cb_power_fist_equipment = new CheckBox();
				cb_power_fist_equipment.Content = "power__fist__equipment";
				CheckBoxes.Add(cb_power_fist_equipment);
				CheckBox cb_active_camo_equipment = new CheckBox();
				cb_active_camo_equipment.Content = "active__camo__equipment";
				CheckBoxes.Add(cb_active_camo_equipment);
				CheckBox cb_ammo_pack_equipment = new CheckBox();
				cb_ammo_pack_equipment.Content = "ammo__pack__equipment";
				CheckBoxes.Add(cb_ammo_pack_equipment);
				CheckBox cb_sensor_pack_equipment = new CheckBox();
				cb_sensor_pack_equipment.Content = "sensor__pack__equipment";
				CheckBoxes.Add(cb_sensor_pack_equipment);
				CheckBox cb_revenant = new CheckBox();
				cb_revenant.Content = "revenant";
				CheckBoxes.Add(cb_revenant);
				CheckBox cb_pickup = new CheckBox();
				cb_pickup.Content = "pickup";
				CheckBoxes.Add(cb_pickup);
				CheckBox cb_prototype_covey_sniper = new CheckBox();
				cb_prototype_covey_sniper.Content = "prototype__covey__sniper";
				CheckBoxes.Add(cb_prototype_covey_sniper);
				CheckBox cb_territory_static = new CheckBox();
				cb_territory_static.Content = "territory__static";
				CheckBoxes.Add(cb_territory_static);
				CheckBox cb_ctf_flag_return_area = new CheckBox();
				cb_ctf_flag_return_area.Content = "ctf__flag__return__area";
				CheckBoxes.Add(cb_ctf_flag_return_area);
				CheckBox cb_ctf_flag_spawn_point = new CheckBox();
				cb_ctf_flag_spawn_point.Content = "ctf__flag__spawn__point";
				CheckBoxes.Add(cb_ctf_flag_spawn_point);
				CheckBox cb_respawn_zone = new CheckBox();
				if (_mapInfo.MapInformation.Game == MapInfo.GameIdentifier.HaloReachBetas)
					cb_respawn_zone.Content = "territories__respawn__zone__bfg";
				else
					cb_respawn_zone.Content = "respawn__zone";
				CheckBoxes.Add(cb_respawn_zone);
				CheckBox cb_territories_respawn_zone = new CheckBox();
				cb_territories_respawn_zone.Content = "territories__respawn__zone";
				if (_mapInfo.MapInformation.Game == MapInfo.GameIdentifier.HaloReachBetas)
					CheckBoxes.Add(cb_territories_respawn_zone);
				CheckBox cb_invasion_elite_buy = new CheckBox();
				cb_invasion_elite_buy.Content = "invasion__elite__buy";
				CheckBoxes.Add(cb_invasion_elite_buy);
				CheckBox cb_invasion_elite_drop = new CheckBox();
				cb_invasion_elite_drop.Content = "invasion__elite__drop";
				CheckBoxes.Add(cb_invasion_elite_drop);
				CheckBox cb_invasion_slayer = new CheckBox();
				cb_invasion_slayer.Content = "invasion__slayer";
				CheckBoxes.Add(cb_invasion_slayer);
				CheckBox cb_invasion_spartan_buy = new CheckBox();
				cb_invasion_spartan_buy.Content = "invasion__spartan__buy";
				CheckBoxes.Add(cb_invasion_spartan_buy);
				CheckBox cb_invasion_spartan_drop = new CheckBox();
				cb_invasion_spartan_drop.Content = "invasion__spartan__drop";
				CheckBoxes.Add(cb_invasion_spartan_drop);
				CheckBox cb_invasion_spawn_controller = new CheckBox();
				cb_invasion_spawn_controller.Content = "invasion__spawn__controller";
				CheckBoxes.Add(cb_invasion_spawn_controller);
				CheckBox cb_oddball_ball_spawn_point = new CheckBox();
				cb_oddball_ball_spawn_point.Content = "oddball__ball__spawn__point";
				CheckBoxes.Add(cb_oddball_ball_spawn_point);
				CheckBox cb_plasma_launcher = new CheckBox();
				cb_plasma_launcher.Content = "plasma__launcher";
				CheckBoxes.Add(cb_plasma_launcher);
				CheckBox cb_fusion_coil = new CheckBox();
				cb_fusion_coil.Content = "fusion__coil";
				CheckBoxes.Add(cb_fusion_coil);
				CheckBox cb_unsc_shield_generator = new CheckBox();
				cb_unsc_shield_generator.Content = "unsc__shield__generator";
				CheckBoxes.Add(cb_unsc_shield_generator);
				CheckBox cb_cov_shield_generator = new CheckBox();
				cb_cov_shield_generator.Content = "cov__shield__generator";
				CheckBoxes.Add(cb_cov_shield_generator);
				CheckBox cb_initial_spawn_point = new CheckBox();
				cb_initial_spawn_point.Content = "initial__spawn__point";
				CheckBoxes.Add(cb_initial_spawn_point);
				CheckBox cb_invasion_vehicle_req = new CheckBox();
				cb_invasion_vehicle_req.Content = "invasion__vehicle__req";
				CheckBoxes.Add(cb_invasion_vehicle_req);
				CheckBox cb_vehicle_req_floor = new CheckBox();
				cb_vehicle_req_floor.Content = "vehicle__req__floor";
				CheckBoxes.Add(cb_vehicle_req_floor);
				CheckBox cb_wall_switch = new CheckBox();
				cb_wall_switch.Content = "wall__switch";
				CheckBoxes.Add(cb_wall_switch);
				CheckBox cb_health_station = new CheckBox();
				cb_health_station.Content = "health__station";
				CheckBoxes.Add(cb_health_station);
				CheckBox cb_req_unsc_laser = new CheckBox();
				cb_req_unsc_laser.Content = "req__unsc__laser";
				CheckBoxes.Add(cb_req_unsc_laser);
				CheckBox cb_req_unsc_dmr = new CheckBox();
				cb_req_unsc_dmr.Content = "req__unsc__dmr";
				CheckBoxes.Add(cb_req_unsc_dmr);
				CheckBox cb_req_unsc_rocket = new CheckBox();
				cb_req_unsc_rocket.Content = "req__unsc__rocket";
				CheckBoxes.Add(cb_req_unsc_rocket);
				CheckBox cb_req_unsc_shotgun = new CheckBox();
				cb_req_unsc_shotgun.Content = "req__unsc__shotgun";
				CheckBoxes.Add(cb_req_unsc_shotgun);
				CheckBox cb_req_unsc_sniper = new CheckBox();
				cb_req_unsc_sniper.Content = "req__unsc__sniper";
				CheckBoxes.Add(cb_req_unsc_sniper);
				CheckBox cb_req_covy_launcher = new CheckBox();
				cb_req_covy_launcher.Content = "req__covy__launcher";
				CheckBoxes.Add(cb_req_covy_launcher);
				CheckBox cb_req_covy_needler = new CheckBox();
				cb_req_covy_needler.Content = "req__covy__needler";
				CheckBoxes.Add(cb_req_covy_needler);
				CheckBox cb_req_covy_sniper = new CheckBox();
				cb_req_covy_sniper.Content = "req__covy__sniper";
				CheckBoxes.Add(cb_req_covy_sniper);
				CheckBox cb_req_covy_sword = new CheckBox();
				cb_req_covy_sword.Content = "req__covy__sword";
				CheckBoxes.Add(cb_req_covy_sword);
				CheckBox cb_shock_loadout = new CheckBox();
				cb_shock_loadout.Content = "shock__loadout";
				CheckBoxes.Add(cb_shock_loadout);
				CheckBox cb_specialist_loadout = new CheckBox();
				cb_specialist_loadout.Content = "specialist__loadout";
				CheckBoxes.Add(cb_specialist_loadout);
				CheckBox cb_assassin_loadout = new CheckBox();
				cb_assassin_loadout.Content = "assassin__loadout";
				CheckBoxes.Add(cb_assassin_loadout);
				CheckBox cb_infiltrator_loadout = new CheckBox();
				cb_infiltrator_loadout.Content = "infiltrator__loadout";
				CheckBoxes.Add(cb_infiltrator_loadout);
				CheckBox cb_warrior_loadout = new CheckBox();
				cb_warrior_loadout.Content = "warrior__loadout";
				CheckBoxes.Add(cb_warrior_loadout);
				CheckBox cb_combatant_loadout = new CheckBox();
				cb_combatant_loadout.Content = "combatant__loadout";
				CheckBoxes.Add(cb_combatant_loadout);
				CheckBox cb_engineer_loadout = new CheckBox();
				cb_engineer_loadout.Content = "engineer__loadout";
				CheckBoxes.Add(cb_engineer_loadout);
				CheckBox cb_infantry_loadout = new CheckBox();
				cb_infantry_loadout.Content = "infantry__loadout";
				CheckBoxes.Add(cb_infantry_loadout);
				CheckBox cb_operator_loadout = new CheckBox();
				cb_operator_loadout.Content = "operator__loadout";
				CheckBoxes.Add(cb_operator_loadout);
				CheckBox cb_recon_loadout = new CheckBox();
				cb_recon_loadout.Content = "recon__loadout";
				CheckBoxes.Add(cb_recon_loadout);
				CheckBox cb_scout_loadout = new CheckBox();
				cb_scout_loadout.Content = "scout__loadout";
				CheckBoxes.Add(cb_scout_loadout);
				CheckBox cb_seeker_loadout = new CheckBox();
				cb_seeker_loadout.Content = "seeker__loadout";
				CheckBoxes.Add(cb_seeker_loadout);
				CheckBox cb_airborne_loadout = new CheckBox();
				cb_airborne_loadout.Content = "airborne__loadout";
				CheckBoxes.Add(cb_airborne_loadout);
				CheckBox cb_ranger_loadout = new CheckBox();
				cb_ranger_loadout.Content = "ranger__loadout";
				CheckBoxes.Add(cb_ranger_loadout);
				CheckBox cb_req_buy_banshee = new CheckBox();
				cb_req_buy_banshee.Content = "req__buy__banshee";
				CheckBoxes.Add(cb_req_buy_banshee);
				CheckBox cb_req_buy_falcon = new CheckBox();
				cb_req_buy_falcon.Content = "req__buy__falcon";
				CheckBoxes.Add(cb_req_buy_falcon);
				CheckBox cb_req_buy_ghost = new CheckBox();
				cb_req_buy_ghost.Content = "req__buy__ghost";
				CheckBoxes.Add(cb_req_buy_ghost);
				CheckBox cb_req_buy_mongoose = new CheckBox();
				cb_req_buy_mongoose.Content = "req__buy__mongoose";
				CheckBoxes.Add(cb_req_buy_mongoose);
				CheckBox cb_req_buy_revenant = new CheckBox();
				cb_req_buy_revenant.Content = "req__buy__revenant";
				CheckBoxes.Add(cb_req_buy_revenant);
				CheckBox cb_req_buy_scorpion = new CheckBox();
				cb_req_buy_scorpion.Content = "req__buy__scorpion";
				CheckBoxes.Add(cb_req_buy_scorpion);
				CheckBox cb_req_buy_warthog = new CheckBox();
				cb_req_buy_warthog.Content = "req__buy__warthog";
				CheckBoxes.Add(cb_req_buy_warthog);
				CheckBox cb_req_buy_wraith = new CheckBox();
				cb_req_buy_wraith.Content = "req__buy__wraith";
				CheckBoxes.Add(cb_req_buy_wraith);
				CheckBox cb_fireteam_1_respawn_zone = new CheckBox();
				cb_fireteam_1_respawn_zone.Content = "fireteam__1__respawn__zone";
				CheckBoxes.Add(cb_fireteam_1_respawn_zone);
				CheckBox cb_fireteam_2_respawn_zone = new CheckBox();
				cb_fireteam_2_respawn_zone.Content = "fireteam__2__respawn__zone";
				CheckBoxes.Add(cb_fireteam_2_respawn_zone);
				CheckBox cb_fireteam_3_respawn_zone = new CheckBox();
				cb_fireteam_3_respawn_zone.Content = "fireteam__3__respawn__zone";
				CheckBoxes.Add(cb_fireteam_3_respawn_zone);
				CheckBox cb_fireteam_4_respawn_zone = new CheckBox();
				cb_fireteam_4_respawn_zone.Content = "fireteam__4__respawn__zone";
				CheckBoxes.Add(cb_fireteam_4_respawn_zone);
				CheckBox cb_semi = new CheckBox();
				cb_semi.Content = "semi";
				CheckBoxes.Add(cb_semi);
				CheckBox cb_soccer_ball = new CheckBox();
				cb_soccer_ball.Content = "soccer__ball";
				CheckBoxes.Add(cb_soccer_ball);
				CheckBox cb_golf_ball = new CheckBox();
				cb_golf_ball.Content = "golf__ball";
				CheckBoxes.Add(cb_golf_ball);
				CheckBox cb_golf_ball_blue = new CheckBox();
				cb_golf_ball_blue.Content = "golf__ball__blue";
				CheckBoxes.Add(cb_golf_ball_blue);
				CheckBox cb_golf_ball_red = new CheckBox();
				cb_golf_ball_red.Content = "golf__ball__red";
				CheckBoxes.Add(cb_golf_ball_red);
				CheckBox cb_golf_club = new CheckBox();
				cb_golf_club.Content = "golf__club";
				CheckBoxes.Add(cb_golf_club);
				CheckBox cb_golf_cup = new CheckBox();
				cb_golf_cup.Content = "golf__cup";
				CheckBoxes.Add(cb_golf_cup);
				CheckBox cb_golf_tee = new CheckBox();
				cb_golf_tee.Content = "golf__tee";
				CheckBoxes.Add(cb_golf_tee);
				CheckBox cb_dice = new CheckBox();
				cb_dice.Content = "dice";
				CheckBoxes.Add(cb_dice);
				CheckBox cb_space_crate = new CheckBox();
				cb_space_crate.Content = "space__crate";
				CheckBoxes.Add(cb_space_crate);
				CheckBox cb_eradicator_loadout = new CheckBox();
				cb_eradicator_loadout.Content = "eradicator__loadout";
				CheckBoxes.Add(cb_eradicator_loadout);
				CheckBox cb_saboteur_loadout = new CheckBox();
				cb_saboteur_loadout.Content = "saboteur__loadout";
				CheckBoxes.Add(cb_saboteur_loadout);
				CheckBox cb_grenadier_loadout = new CheckBox();
				cb_grenadier_loadout.Content = "grenadier__loadout";
				CheckBoxes.Add(cb_grenadier_loadout);
				CheckBox cb_marksman_loadout = new CheckBox();
				cb_marksman_loadout.Content = "marksman__loadout";
				CheckBoxes.Add(cb_marksman_loadout);
				CheckBox cb_flare = new CheckBox();
				cb_flare.Content = "flare";
				CheckBoxes.Add(cb_flare);
				CheckBox cb_glow_stick = new CheckBox();
				cb_glow_stick.Content = "glow__stick";
				CheckBoxes.Add(cb_glow_stick);
				CheckBox cb_elite_shot = new CheckBox();
				cb_elite_shot.Content = "elite__shot";
				CheckBoxes.Add(cb_elite_shot);
				CheckBox cb_grenade_launcher = new CheckBox();
				cb_grenade_launcher.Content = "grenade__launcher";
				CheckBoxes.Add(cb_grenade_launcher);
				CheckBox cb_phantom_approach = new CheckBox();
				cb_phantom_approach.Content = "phantom__approach";
				CheckBoxes.Add(cb_phantom_approach);
				CheckBox cb_hologram_equipment = new CheckBox();
				cb_hologram_equipment.Content = "hologram__equipment";
				CheckBoxes.Add(cb_hologram_equipment);
				CheckBox cb_evade_equipment = new CheckBox();
				cb_evade_equipment.Content = "evade__equipment";
				CheckBoxes.Add(cb_evade_equipment);
				CheckBox cb_unsc_data_core = new CheckBox();
				cb_unsc_data_core.Content = "unsc__data__core";
				CheckBoxes.Add(cb_unsc_data_core);
				CheckBox cb_danger_zone = new CheckBox();
				cb_danger_zone.Content = "danger__zone";
				CheckBoxes.Add(cb_danger_zone);
				CheckBox cb_teleporter_sender = new CheckBox();
				cb_teleporter_sender.Content = "teleporter__sender";
				CheckBoxes.Add(cb_teleporter_sender);
				CheckBox cb_teleporter_reciever = new CheckBox();
				cb_teleporter_reciever.Content = "teleporter__reciever";
				CheckBoxes.Add(cb_teleporter_reciever);
				CheckBox cb_teleporter_2way = new CheckBox();
				cb_teleporter_2way.Content = "teleporter__2way";
				CheckBoxes.Add(cb_teleporter_2way);
				CheckBox cb_data_core_beam = new CheckBox();
				cb_data_core_beam.Content = "data__core__beam";
				CheckBoxes.Add(cb_data_core_beam);
				CheckBox cb_phantom_overwatch = new CheckBox();
				cb_phantom_overwatch.Content = "phantom__overwatch";
				CheckBoxes.Add(cb_phantom_overwatch);
				CheckBox cb_longsword = new CheckBox();
				cb_longsword.Content = "longsword";
				CheckBoxes.Add(cb_longsword);
				CheckBox cb_invisible_cube_of_derek = new CheckBox();
				cb_invisible_cube_of_derek.Content = "invisible__cube__of__derek";
				CheckBoxes.Add(cb_invisible_cube_of_derek);
				CheckBox cb_phantom_scenery = new CheckBox();
				cb_phantom_scenery.Content = "phantom__scenery";
				CheckBoxes.Add(cb_phantom_scenery);
				CheckBox cb_pelican_scenery = new CheckBox();
				cb_pelican_scenery.Content = "pelican__scenery";
				CheckBoxes.Add(cb_pelican_scenery);
				CheckBox cb_phantom = new CheckBox();
				cb_phantom.Content = "phantom";
				CheckBoxes.Add(cb_phantom);
				CheckBox cb_pelican = new CheckBox();
				cb_pelican.Content = "pelican";
				CheckBoxes.Add(cb_pelican);
				CheckBox cb_armory_shelf = new CheckBox();
				cb_armory_shelf.Content = "armory__shelf";
				CheckBoxes.Add(cb_armory_shelf);
				CheckBox cb_cov_resupply_capsule = new CheckBox();
				cb_cov_resupply_capsule.Content = "cov__resupply__capsule";
				CheckBoxes.Add(cb_cov_resupply_capsule);
				CheckBox cb_covy_drop_pod = new CheckBox();
				cb_covy_drop_pod.Content = "covy__drop__pod";
				CheckBoxes.Add(cb_covy_drop_pod);
				CheckBox cb_invisible_marker = new CheckBox();
				cb_invisible_marker.Content = "invisible__marker";
				CheckBoxes.Add(cb_invisible_marker);
				CheckBox cb_weak_respawn_zone = new CheckBox();
				cb_weak_respawn_zone.Content = "weak__respawn__zone";
				CheckBoxes.Add(cb_weak_respawn_zone);
				CheckBox cb_weak_anti_respawn_zone = new CheckBox();
				cb_weak_anti_respawn_zone.Content = "weak__anti__respawn__zone";
				CheckBoxes.Add(cb_weak_anti_respawn_zone);
				CheckBox cb_phantom_device = new CheckBox();
				cb_phantom_device.Content = "phantom__device";
				CheckBoxes.Add(cb_phantom_device);
				CheckBox cb_resupply_capsule = new CheckBox();
				cb_resupply_capsule.Content = "resupply__capsule";
				CheckBoxes.Add(cb_resupply_capsule);
				CheckBox cb_resupply_capsule_open = new CheckBox();
				cb_resupply_capsule_open.Content = "resupply__capsule__open";
				CheckBoxes.Add(cb_resupply_capsule_open);
				CheckBox cb_weapon_box = new CheckBox();
				cb_weapon_box.Content = "weapon__box";
				CheckBoxes.Add(cb_weapon_box);
				CheckBox cb_tech_console_stationary = new CheckBox();
				cb_tech_console_stationary.Content = "tech__console__stationary";
				CheckBoxes.Add(cb_tech_console_stationary);
				CheckBox cb_tech_console_wall = new CheckBox();
				cb_tech_console_wall.Content = "tech__console__wall";
				CheckBoxes.Add(cb_tech_console_wall);
				CheckBox cb_mp_cinematic_camera = new CheckBox();
				cb_mp_cinematic_camera.Content = "mp__cinematic__camera";
				CheckBoxes.Add(cb_mp_cinematic_camera);
				CheckBox cb_invis_cov_resupply_capsule = new CheckBox();
				cb_invis_cov_resupply_capsule.Content = "invis__cov__resupply__capsule";
				CheckBoxes.Add(cb_invis_cov_resupply_capsule);
				CheckBox cb_cov_power_module = new CheckBox();
				cb_cov_power_module.Content = "cov__power__module";
				CheckBoxes.Add(cb_cov_power_module);
				CheckBox cb_flak_cannon = new CheckBox();
				cb_flak_cannon.Content = "flak__cannon";
				CheckBoxes.Add(cb_flak_cannon);
				CheckBox cb_dropzone_boundary = new CheckBox();
				cb_dropzone_boundary.Content = "dropzone__boundary";
				CheckBoxes.Add(cb_dropzone_boundary);
				CheckBox cb_shield_door_small = new CheckBox();
				cb_shield_door_small.Content = "shield__door__small";
				CheckBoxes.Add(cb_shield_door_small);
				CheckBox cb_shield_door_medium = new CheckBox();
				cb_shield_door_medium.Content = "shield__door__medium";
				CheckBoxes.Add(cb_shield_door_medium);
				CheckBox cb_shield_door_large = new CheckBox();
				cb_shield_door_large.Content = "shield__door__large";
				CheckBoxes.Add(cb_shield_door_large);
				CheckBox cb_drop_shield_equipment = new CheckBox();
				cb_drop_shield_equipment.Content = "drop__shield__equipment";
				CheckBoxes.Add(cb_drop_shield_equipment);
				CheckBox cb_machinegun = new CheckBox();
				cb_machinegun.Content = "machinegun";
				CheckBoxes.Add(cb_machinegun);
				CheckBox cb_machinegun_turret = new CheckBox();
				cb_machinegun_turret.Content = "machinegun__turret";
				CheckBoxes.Add(cb_machinegun_turret);
				CheckBox cb_plasma_turret_weapon = new CheckBox();
				cb_plasma_turret_weapon.Content = "plasma__turret__weapon";
				CheckBoxes.Add(cb_plasma_turret_weapon);
				CheckBox cb_mounted_plasma_turret = new CheckBox();
				cb_mounted_plasma_turret.Content = "mounted__plasma__turret";
				CheckBoxes.Add(cb_mounted_plasma_turret);
				CheckBox cb_shade_turret = new CheckBox();
				cb_shade_turret.Content = "shade__turret";
				CheckBoxes.Add(cb_shade_turret);
				CheckBox cb_cargo_truck = new CheckBox();
				cb_cargo_truck.Content = "cargo__truck";
				CheckBoxes.Add(cb_cargo_truck);
				CheckBox cb_cart_electric = new CheckBox();
				cb_cart_electric.Content = "cart__electric";
				CheckBoxes.Add(cb_cart_electric);
				CheckBox cb_forklift = new CheckBox();
				cb_forklift.Content = "forklift";
				CheckBoxes.Add(cb_forklift);
				CheckBox cb_military_truck = new CheckBox();
				cb_military_truck.Content = "military__truck";
				CheckBoxes.Add(cb_military_truck);
				CheckBox cb_oni_van = new CheckBox();
				cb_oni_van.Content = "oni__van";
				CheckBoxes.Add(cb_oni_van);
				CheckBox cb_warthog_gunner = new CheckBox();
				cb_warthog_gunner.Content = "warthog__gunner";
				CheckBoxes.Add(cb_warthog_gunner);
				CheckBox cb_warthog_gauss_turret = new CheckBox();
				cb_warthog_gauss_turret.Content = "warthog__gauss__turret";
				CheckBoxes.Add(cb_warthog_gauss_turret);
				CheckBox cb_warthog_rocket_turret = new CheckBox();
				cb_warthog_rocket_turret.Content = "warthog__rocket__turret";
				CheckBoxes.Add(cb_warthog_rocket_turret);
				CheckBox cb_scorpion_infantry_gunner = new CheckBox();
				cb_scorpion_infantry_gunner.Content = "scorpion__infantry__gunner";
				CheckBoxes.Add(cb_scorpion_infantry_gunner);
				CheckBox cb_falcon_grenadier_left = new CheckBox();
				cb_falcon_grenadier_left.Content = "falcon__grenadier__left";
				CheckBoxes.Add(cb_falcon_grenadier_left);
				CheckBox cb_falcon_grenadier_right = new CheckBox();
				cb_falcon_grenadier_right.Content = "falcon__grenadier__right";
				CheckBoxes.Add(cb_falcon_grenadier_right);
				CheckBox cb_wraith_infantry_turret = new CheckBox();
				cb_wraith_infantry_turret.Content = "wraith__infantry__turret";
				CheckBoxes.Add(cb_wraith_infantry_turret);
				CheckBox cb_land_mine = new CheckBox();
				cb_land_mine.Content = "land__mine";
				CheckBoxes.Add(cb_land_mine);
				CheckBox cb_target_laser = new CheckBox();
				cb_target_laser.Content = "target__laser";
				CheckBoxes.Add(cb_target_laser);
				CheckBox cb_ff_kill_zone = new CheckBox();
				cb_ff_kill_zone.Content = "ff__kill__zone";
				CheckBoxes.Add(cb_ff_kill_zone);
				CheckBox cb_ff_plat_1x1_flat = new CheckBox();
				cb_ff_plat_1x1_flat.Content = "ff__plat__1x1__flat";
				CheckBoxes.Add(cb_ff_plat_1x1_flat);
				CheckBox cb_shade_anti_air = new CheckBox();
				cb_shade_anti_air.Content = "shade__anti__air";
				CheckBoxes.Add(cb_shade_anti_air);
				CheckBox cb_shade_flak = new CheckBox();
				cb_shade_flak.Content = "shade__flak";
				CheckBoxes.Add(cb_shade_flak);
				CheckBox cb_shade_plasma = new CheckBox();
				cb_shade_plasma.Content = "shade__plasma";
				CheckBoxes.Add(cb_shade_plasma);
				if (_mapInfo.MapInformation.Game != MapInfo.GameIdentifier.Halo4)
				{
					CheckBox cb_killball = new CheckBox();
					cb_killball.Content = "killball";
					CheckBoxes.Add(cb_killball);
					CheckBox cb_ff_light_red = new CheckBox();
					cb_ff_light_red.Content = "ff__light__red";
					CheckBoxes.Add(cb_ff_light_red);
					CheckBox cb_ff_light_blue = new CheckBox();
					cb_ff_light_blue.Content = "ff__light__blue";
					CheckBoxes.Add(cb_ff_light_blue);
					CheckBox cb_ff_light_green = new CheckBox();
					cb_ff_light_green.Content = "ff__light__green";
					CheckBoxes.Add(cb_ff_light_green);
					CheckBox cb_ff_light_orange = new CheckBox();
					cb_ff_light_orange.Content = "ff__light__orange";
					CheckBoxes.Add(cb_ff_light_orange);
					CheckBox cb_ff_light_purple = new CheckBox();
					cb_ff_light_purple.Content = "ff__light__purple";
					CheckBoxes.Add(cb_ff_light_purple);
					CheckBox cb_ff_light_yellow = new CheckBox();
					cb_ff_light_yellow.Content = "ff__light__yellow";
					CheckBoxes.Add(cb_ff_light_yellow);
					CheckBox cb_ff_light_white = new CheckBox();
					cb_ff_light_white.Content = "ff__light__white";
					CheckBoxes.Add(cb_ff_light_white);
					CheckBox cb_ff_light_flash_red = new CheckBox();
					cb_ff_light_flash_red.Content = "ff__light__flash__red";
					CheckBoxes.Add(cb_ff_light_flash_red);
					CheckBox cb_ff_light_flash_yellow = new CheckBox();
					cb_ff_light_flash_yellow.Content = "ff__light__flash__yellow";
					CheckBoxes.Add(cb_ff_light_flash_yellow);
					CheckBox cb_fx_colorblind = new CheckBox();
					cb_fx_colorblind.Content = "fx__colorblind";
					CheckBoxes.Add(cb_fx_colorblind);
					CheckBox cb_fx_gloomy = new CheckBox();
					cb_fx_gloomy.Content = "fx__gloomy";
					CheckBoxes.Add(cb_fx_gloomy);
					CheckBox cb_fx_juicy = new CheckBox();
					cb_fx_juicy.Content = "fx__juicy";
					CheckBoxes.Add(cb_fx_juicy);
					CheckBox cb_fx_nova = new CheckBox();
					cb_fx_nova.Content = "fx__nova";
					CheckBoxes.Add(cb_fx_nova);
					CheckBox cb_fx_olde_timey = new CheckBox();
					cb_fx_olde_timey.Content = "fx__olde__timey";
					CheckBoxes.Add(cb_fx_olde_timey);
					CheckBox cb_fx_pen_and_ink = new CheckBox();
					cb_fx_pen_and_ink.Content = "fx__pen__and__ink";
					CheckBoxes.Add(cb_fx_pen_and_ink);
					CheckBox cb_fx_dusk = new CheckBox();
					cb_fx_dusk.Content = "fx__dusk";
					CheckBoxes.Add(cb_fx_dusk);
					CheckBox cb_fx_golden_hour = new CheckBox();
					cb_fx_golden_hour.Content = "fx__golden__hour";
					CheckBoxes.Add(cb_fx_golden_hour);
					CheckBox cb_fx_eerie = new CheckBox();
					cb_fx_eerie.Content = "fx__eerie";
					CheckBoxes.Add(cb_fx_eerie);
					CheckBox cb_ff_grid = new CheckBox();
					cb_ff_grid.Content = "ff__grid";
					CheckBoxes.Add(cb_ff_grid);
					CheckBox cb_invisible_cube_of_alarming_1 = new CheckBox();
					cb_invisible_cube_of_alarming_1.Content = "invisible__cube__of__alarming__1";
					CheckBoxes.Add(cb_invisible_cube_of_alarming_1);
					CheckBox cb_invisible_cube_of_alarming_2 = new CheckBox();
					cb_invisible_cube_of_alarming_2.Content = "invisible__cube__of__alarming__2";
					CheckBoxes.Add(cb_invisible_cube_of_alarming_2);
					CheckBox cb_spawning_safe = new CheckBox();
					cb_spawning_safe.Content = "spawning__safe";
					CheckBoxes.Add(cb_spawning_safe);
					CheckBox cb_spawning_safe_soft = new CheckBox();
					cb_spawning_safe_soft.Content = "spawning__safe__soft";
					CheckBoxes.Add(cb_spawning_safe_soft);
					CheckBox cb_spawning_kill = new CheckBox();
					cb_spawning_kill.Content = "spawning__kill";
					CheckBoxes.Add(cb_spawning_kill);
					CheckBox cb_spawning_kill_soft = new CheckBox();
					cb_spawning_kill_soft.Content = "spawning__kill__soft";
					CheckBoxes.Add(cb_spawning_kill_soft);
					CheckBox cb_package_cabinet = new CheckBox();
					cb_package_cabinet.Content = "package__cabinet";
					CheckBoxes.Add(cb_package_cabinet);
					CheckBox cb_cov_powermodule_stand = new CheckBox();
					cb_cov_powermodule_stand.Content = "cov__powermodule__stand";
					CheckBoxes.Add(cb_cov_powermodule_stand);
				}
				if (_mapInfo.MapInformation.Game == MapInfo.GameIdentifier.Halo4)
				{
					CheckBox cb_stasis_field = new CheckBox();
					cb_stasis_field.Content = "stasis__field";
					CheckBoxes.Add(cb_stasis_field);
					CheckBox cb_forerunner_rifle = new CheckBox();
					cb_forerunner_rifle.Content = "forerunner__rifle";
					CheckBoxes.Add(cb_forerunner_rifle);
					CheckBox cb_vespid = new CheckBox();
					cb_vespid.Content = "vespid";
					CheckBoxes.Add(cb_vespid);
					CheckBox cb_spread_gun = new CheckBox();
					cb_spread_gun.Content = "spread__gun";
					CheckBoxes.Add(cb_spread_gun);
					CheckBox cb_attach_beam = new CheckBox();
					cb_attach_beam.Content = "attach__beam";
					CheckBoxes.Add(cb_attach_beam);
					CheckBox cb_blast_wave = new CheckBox();
					cb_blast_wave.Content = "blast__wave";
					CheckBoxes.Add(cb_blast_wave);
					CheckBox cb_reflective_shield = new CheckBox();
					cb_reflective_shield.Content = "reflective__shield";
					CheckBoxes.Add(cb_reflective_shield);
					CheckBox cb_teleporter = new CheckBox();
					cb_teleporter.Content = "teleporter";
					CheckBoxes.Add(cb_teleporter);
					CheckBox cb_burst_pistol = new CheckBox();
					cb_burst_pistol.Content = "burst__pistol";
					CheckBoxes.Add(cb_burst_pistol);
					CheckBox cb_rail_gun = new CheckBox();
					cb_rail_gun.Content = "rail__gun";
					CheckBoxes.Add(cb_rail_gun);
					CheckBox cb_stasis_rifle = new CheckBox();
					cb_stasis_rifle.Content = "stasis__rifle";
					CheckBoxes.Add(cb_stasis_rifle);
					CheckBox cb_assault_carbine = new CheckBox();
					cb_assault_carbine.Content = "assault__carbine";
					CheckBoxes.Add(cb_assault_carbine);
					CheckBox cb_auto_turret = new CheckBox();
					cb_auto_turret.Content = "auto__turret";
					CheckBoxes.Add(cb_auto_turret);
					CheckBox cb_surfboard = new CheckBox();
					cb_surfboard.Content = "surfboard";
					CheckBoxes.Add(cb_surfboard);
					CheckBox cb_juggernaut = new CheckBox();
					cb_juggernaut.Content = "juggernaut";
					CheckBoxes.Add(cb_juggernaut);
					CheckBox cb_xray = new CheckBox();
					cb_xray.Content = "xray";
					CheckBoxes.Add(cb_xray);
					CheckBox cb_needle_shotgun = new CheckBox();
					cb_needle_shotgun.Content = "needle__shotgun";
					CheckBoxes.Add(cb_needle_shotgun);
					CheckBox cb_grapple_harpoon = new CheckBox();
					cb_grapple_harpoon.Content = "grapple__harpoon";
					CheckBoxes.Add(cb_grapple_harpoon);
					CheckBox cb_scaleshot_rifle = new CheckBox();
					cb_scaleshot_rifle.Content = "scaleshot__rifle";
					CheckBoxes.Add(cb_scaleshot_rifle);
					CheckBox cb_covenant_carbine = new CheckBox();
					cb_covenant_carbine.Content = "covenant__carbine";
					CheckBoxes.Add(cb_covenant_carbine);
					CheckBox cb_beam_sniper_rifle = new CheckBox();
					cb_beam_sniper_rifle.Content = "beam__sniper__rifle";
					CheckBoxes.Add(cb_beam_sniper_rifle);
					CheckBox cb_sticky_grenade_launcher = new CheckBox();
					cb_sticky_grenade_launcher.Content = "sticky__grenade__launcher";
					CheckBoxes.Add(cb_sticky_grenade_launcher);
					CheckBox cb_thruster_pack_equipment = new CheckBox();
					cb_thruster_pack_equipment.Content = "thruster__pack__equipment";
					CheckBoxes.Add(cb_thruster_pack_equipment);
					CheckBox cb_lmg = new CheckBox();
					cb_lmg.Content = "lmg";
					CheckBoxes.Add(cb_lmg);
					CheckBox cb_mortar = new CheckBox();
					cb_mortar.Content = "mortar";
					CheckBoxes.Add(cb_mortar);
					CheckBox cb_stasis_pistol = new CheckBox();
					cb_stasis_pistol.Content = "stasis__pistol";
					CheckBoxes.Add(cb_stasis_pistol);
					CheckBox cb_disruption_grenade = new CheckBox();
					cb_disruption_grenade.Content = "disruption__grenade";
					CheckBoxes.Add(cb_disruption_grenade);
					CheckBox cb_active_hacker_equipment = new CheckBox();
					cb_active_hacker_equipment.Content = "active__hacker__equipment";
					CheckBoxes.Add(cb_active_hacker_equipment);
				}
				CheckBox cb_bishop_beam = new CheckBox();
				cb_bishop_beam.Content = "bishop__beam";
				CheckBoxes.Add(cb_bishop_beam);
				CheckBox cb_concussion_rifle = new CheckBox();
				cb_concussion_rifle.Content = "concussion__rifle";
				CheckBoxes.Add(cb_concussion_rifle);
				CheckBox cb_fuel_rod_cannon = new CheckBox();
				cb_fuel_rod_cannon.Content = "fuel__rod__cannon";
				CheckBoxes.Add(cb_fuel_rod_cannon);
				CheckBox cb_needle_grenade = new CheckBox();
				cb_needle_grenade.Content = "needle__grenade";
				CheckBoxes.Add(cb_needle_grenade);
				CheckBox cb_blam_beacon = new CheckBox();
				cb_blam_beacon.Content = "blam__beacon";
				CheckBoxes.Add(cb_blam_beacon);
				CheckBox cb_battle_rifle = new CheckBox();
				cb_battle_rifle.Content = "battle__rifle";
				CheckBoxes.Add(cb_battle_rifle);
				CheckBox cb_forerunner_smg = new CheckBox();
				cb_forerunner_smg.Content = "forerunner__smg";
				CheckBoxes.Add(cb_forerunner_smg);
				CheckBox cb_storm_magnum_ctf = new CheckBox();
				cb_storm_magnum_ctf.Content = "storm__magnum__ctf";
				CheckBoxes.Add(cb_storm_magnum_ctf);
				CheckBox cb_blam_effect = new CheckBox();
				cb_blam_effect.Content = "blam__effect";
				CheckBoxes.Add(cb_blam_effect);
				CheckBox cb_blam_device = new CheckBox();
				cb_blam_device.Content = "blam__device";
				CheckBoxes.Add(cb_blam_device);
				if (_mapInfo.MapInformation.Game == MapInfo.GameIdentifier.Halo4NetworkTest)
				{
					CheckBox cb_blam_crate_med = new CheckBox();
					cb_blam_crate_med.Content = "blam__crate__med";
					CheckBoxes.Add(cb_blam_crate_med);
					CheckBox cb_blam_teamemblem_fury = new CheckBox();
					cb_blam_teamemblem_fury.Content = "blam__teamemblem__fury";
					CheckBoxes.Add(cb_blam_teamemblem_fury);
					CheckBox cb_blam_teamemblem_demon = new CheckBox();
					cb_blam_teamemblem_demon.Content = "blam__teamemblem__demon";
					CheckBoxes.Add(cb_blam_teamemblem_demon);
					CheckBox cb_blam_teamemblem_generic = new CheckBox();
					cb_blam_teamemblem_generic.Content = "blam__teamemblem__generic";
					CheckBoxes.Add(cb_blam_teamemblem_generic);
				}
				CheckBox cb_regen_field_equipment = new CheckBox();
				cb_regen_field_equipment.Content = "regen__field__equipment";
				CheckBoxes.Add(cb_regen_field_equipment);
				CheckBox cb_dom_interact = new CheckBox();
				cb_dom_interact.Content = "dom__interact";
				CheckBoxes.Add(cb_dom_interact);
				CheckBox cb_extract_falloff_respawn = new CheckBox();
				cb_extract_falloff_respawn.Content = "extract__falloff__respawn";
				CheckBoxes.Add(cb_extract_falloff_respawn);
				CheckBox cb_koth_falloff_respawn = new CheckBox();
				cb_koth_falloff_respawn.Content = "koth__falloff__respawn";
				CheckBoxes.Add(cb_koth_falloff_respawn);
				CheckBox cb_flood_sword = new CheckBox();
				cb_flood_sword.Content = "flood__sword";
				CheckBoxes.Add(cb_flood_sword);
				CheckBox cb_aa_none = new CheckBox();
				cb_aa_none.Content = "aa__none";
				CheckBoxes.Add(cb_aa_none);
				CheckBox cb_blam_sound_object_01 = new CheckBox();
				cb_blam_sound_object_01.Content = "blam__sound__object__01";
				CheckBoxes.Add(cb_blam_sound_object_01);
				CheckBox cb_blam_sound_object_02 = new CheckBox();
				cb_blam_sound_object_02.Content = "blam__sound__object__02";
				CheckBoxes.Add(cb_blam_sound_object_02);
				CheckBox cb_blam_sound_object_03 = new CheckBox();
				cb_blam_sound_object_03.Content = "blam__sound__object__03";
				CheckBoxes.Add(cb_blam_sound_object_03);
				CheckBox cb_auto_turret_vehicle = new CheckBox();
				cb_auto_turret_vehicle.Content = "auto__turret__vehicle";
				CheckBoxes.Add(cb_auto_turret_vehicle);
				CheckBox cb_auto_turret_vehicle_pve = new CheckBox();
				cb_auto_turret_vehicle_pve.Content = "auto__turret__vehicle__pve";
				CheckBoxes.Add(cb_auto_turret_vehicle_pve);
				CheckBox cb_dom_base_switch = new CheckBox();
				cb_dom_base_switch.Content = "dom__base__switch";
				CheckBoxes.Add(cb_dom_base_switch);
				CheckBox cb_flood_thruster_pack_equipment = new CheckBox();
				cb_flood_thruster_pack_equipment.Content = "flood__thruster__pack__equipment";
				CheckBoxes.Add(cb_flood_thruster_pack_equipment);
				CheckBox cb_dom_turret = new CheckBox();
				cb_dom_turret.Content = "dom__turret";
				CheckBoxes.Add(cb_dom_turret);
				CheckBox cb_odd_falloff_respawn = new CheckBox();
				cb_odd_falloff_respawn.Content = "odd__falloff__respawn";
				CheckBoxes.Add(cb_odd_falloff_respawn);
				CheckBox cb_extract_anti_respawn = new CheckBox();
				cb_extract_anti_respawn.Content = "extract__anti__respawn";
				CheckBoxes.Add(cb_extract_anti_respawn);
				CheckBox cb_koth_anti_respawn = new CheckBox();
				cb_koth_anti_respawn.Content = "koth__anti__respawn";
				CheckBoxes.Add(cb_koth_anti_respawn);
				CheckBox cb_odd_anti_respawn = new CheckBox();
				cb_odd_anti_respawn.Content = "odd__anti__respawn";
				CheckBoxes.Add(cb_odd_anti_respawn);
				CheckBox cb_bomb_disarm = new CheckBox();
				cb_bomb_disarm.Content = "bomb__disarm";
				CheckBoxes.Add(cb_bomb_disarm);
				CheckBox cb_blam_emblem = new CheckBox();
				cb_blam_emblem.Content = "blam__emblem";
				CheckBoxes.Add(cb_blam_emblem);
				CheckBox cb_blam_switch = new CheckBox();
				cb_blam_switch.Content = "blam__switch";
				CheckBoxes.Add(cb_blam_switch);
				CheckBox cb_blam_sym_switch = new CheckBox();
				cb_blam_sym_switch.Content = "blam__sym__switch";
				CheckBoxes.Add(cb_blam_sym_switch);
				CheckBox cb_flashing_area = new CheckBox();
				cb_flashing_area.Content = "flashing__area";
				CheckBoxes.Add(cb_flashing_area);
				CheckBox cb_dominion_screen_flash = new CheckBox();
				cb_dominion_screen_flash.Content = "dominion__screen__flash";
				CheckBoxes.Add(cb_dominion_screen_flash);
				CheckBox cb_blam_incmoing = new CheckBox();
				cb_blam_incmoing.Content = "blam__incmoing";
				CheckBoxes.Add(cb_blam_incmoing);
				CheckBox cb_koth_incmoing = new CheckBox();
				cb_koth_incmoing.Content = "koth__incmoing";
				CheckBoxes.Add(cb_koth_incmoing);
				CheckBox cb_odd_confetti = new CheckBox();
				cb_odd_confetti.Content = "odd__confetti";
				CheckBoxes.Add(cb_odd_confetti);
				CheckBox cb_mantis = new CheckBox();
				cb_mantis.Content = "mantis";
				CheckBoxes.Add(cb_mantis);
				CheckBox cb_carry_bomb = new CheckBox();
				cb_carry_bomb.Content = "carry__bomb";
				CheckBoxes.Add(cb_carry_bomb);
				CheckBox cb_koth_explosion = new CheckBox();
				cb_koth_explosion.Content = "koth__explosion";
				CheckBoxes.Add(cb_koth_explosion);
				CheckBox cb_dmr_alt = new CheckBox();
				cb_dmr_alt.Content = "dmr__alt";
				CheckBoxes.Add(cb_dmr_alt);
				CheckBox cb_magnum_alt = new CheckBox();
				cb_magnum_alt.Content = "magnum__alt";
				CheckBoxes.Add(cb_magnum_alt);
				CheckBox cb_plasma_pistol_alt = new CheckBox();
				cb_plasma_pistol_alt.Content = "plasma__pistol__alt";
				CheckBoxes.Add(cb_plasma_pistol_alt);
				CheckBox cb_forerunner_rifle_alt = new CheckBox();
				cb_forerunner_rifle_alt.Content = "forerunner__rifle__alt";
				CheckBoxes.Add(cb_forerunner_rifle_alt);
				CheckBox cb_covenant_carbine_alt = new CheckBox();
				cb_covenant_carbine_alt.Content = "covenant__carbine__alt";
				CheckBoxes.Add(cb_covenant_carbine_alt);
				CheckBox cb_forerunner_smg_alt = new CheckBox();
				cb_forerunner_smg_alt.Content = "forerunner__smg__alt";
				CheckBoxes.Add(cb_forerunner_smg_alt);
				CheckBox cb_stasis_pistol_alt = new CheckBox();
				cb_stasis_pistol_alt.Content = "stasis__pistol__alt";
				CheckBoxes.Add(cb_stasis_pistol_alt);
				CheckBox cb_battle_rifle_alt = new CheckBox();
				cb_battle_rifle_alt.Content = "battle__rifle__alt";
				CheckBoxes.Add(cb_battle_rifle_alt);
				CheckBox cb_battle_rifle_alt2 = new CheckBox();
				cb_battle_rifle_alt2.Content = "battle__rifle__alt2";
				CheckBoxes.Add(cb_battle_rifle_alt2);
				CheckBox cb_assault_rifle_alt = new CheckBox();
				cb_assault_rifle_alt.Content = "assault__rifle__alt";
				CheckBoxes.Add(cb_assault_rifle_alt);
				CheckBox cb_assault_rifle_alt2 = new CheckBox();
				cb_assault_rifle_alt2.Content = "assault__rifle__alt2";
				CheckBoxes.Add(cb_assault_rifle_alt2);
				CheckBox cb_jet_pack_equipment_pve = new CheckBox();
				cb_jet_pack_equipment_pve.Content = "jet__pack__equipment__pve";
				CheckBoxes.Add(cb_jet_pack_equipment_pve);
				CheckBox cb_forerunner_vision_pve = new CheckBox();
				cb_forerunner_vision_pve.Content = "forerunner__vision__pve";
				CheckBoxes.Add(cb_forerunner_vision_pve);
				CheckBox cb_auto_turret_pve = new CheckBox();
				cb_auto_turret_pve.Content = "auto__turret__pve";
				CheckBoxes.Add(cb_auto_turret_pve);
				CheckBox cb_thruster_pack_pve = new CheckBox();
				cb_thruster_pack_pve.Content = "thruster__pack__pve";
				CheckBoxes.Add(cb_thruster_pack_pve);
				CheckBox cb_active_shield_pve = new CheckBox();
				cb_active_shield_pve.Content = "active__shield__pve";
				CheckBoxes.Add(cb_active_shield_pve);
				CheckBox cb_sticky_grenade_launcher_pve = new CheckBox();
				cb_sticky_grenade_launcher_pve.Content = "sticky__grenade__launcher__pve";
				CheckBoxes.Add(cb_sticky_grenade_launcher_pve);
				CheckBox cb_rail_gun_pve = new CheckBox();
				cb_rail_gun_pve.Content = "rail__gun__pve";
				CheckBoxes.Add(cb_rail_gun_pve);
				CheckBox cb_plasma_pistol_pve = new CheckBox();
				cb_plasma_pistol_pve.Content = "plasma__pistol__pve";
				CheckBoxes.Add(cb_plasma_pistol_pve);
				CheckBox cb_broadsword = new CheckBox();
				cb_broadsword.Content = "broadsword";
				CheckBoxes.Add(cb_broadsword);
				CheckBox cb_pelican_cannon = new CheckBox();
				cb_pelican_cannon.Content = "pelican__cannon";
				CheckBoxes.Add(cb_pelican_cannon);
				CheckBox cb_pelican_side_turret = new CheckBox();
				cb_pelican_side_turret.Content = "pelican__side__turret";
				CheckBoxes.Add(cb_pelican_side_turret);
				CheckBox cb_pelican_side_turret_mirror = new CheckBox();
				cb_pelican_side_turret_mirror.Content = "pelican__side__turret__mirror";
				CheckBoxes.Add(cb_pelican_side_turret_mirror);
				CheckBox cb_target_laser_m40 = new CheckBox();
				cb_target_laser_m40.Content = "target__laser__m40";
				CheckBoxes.Add(cb_target_laser_m40);
				CheckBox cb_campaign_mantis = new CheckBox();
				cb_campaign_mantis.Content = "campaign__mantis";
				CheckBoxes.Add(cb_campaign_mantis);
				CheckBox cb_active_camo_equipment_m20 = new CheckBox();
				cb_active_camo_equipment_m20.Content = "active__camo__equipment__m20";
				CheckBoxes.Add(cb_active_camo_equipment_m20);
				CheckBox cb_banshee_mp = new CheckBox();
				cb_banshee_mp.Content = "banshee__mp";
				CheckBoxes.Add(cb_banshee_mp);
				CheckBox cb_mammoth_turret = new CheckBox();
				cb_mammoth_turret.Content = "mammoth__turret";
				CheckBoxes.Add(cb_mammoth_turret);
				CheckBox cb_dom_turret_pad = new CheckBox();
				cb_dom_turret_pad.Content = "dom__turret__pad";
				CheckBoxes.Add(cb_dom_turret_pad);
				CheckBox cb_auto_turret_vehicle_knight = new CheckBox();
				cb_auto_turret_vehicle_knight.Content = "auto__turret__vehicle__knight";
				CheckBoxes.Add(cb_auto_turret_vehicle_knight);

				// This is necessary to make the code after this work without throwing an
				// an exception if there are bits enabled beyond the valid entries. 
				CheckBox cb_generic = new CheckBox();
				for (int i = 0; CheckBoxes.Count < 2048; i++)
					CheckBoxes.Add(cb_generic);

				// Check the checkbox if its corresponding bit is enabled
				for (int i = 0; i < 2048; i++)
				{
					if (_mapInfo.MapInformation.ObjectTable[i] == true)
						CheckBoxes[i].IsChecked = true;
					else
						CheckBoxes[i].IsChecked = false;
				}

				// Add the checkboxes to the UI
				if (_mapInfo.MapInformation.Game == MapInfo.GameIdentifier.HaloReachBetas)
					for (int i = 0; i < 161; i++)
						Bitfield_MPObjects.Items.Add(CheckBoxes[i]);
				if (_mapInfo.MapInformation.Game == MapInfo.GameIdentifier.HaloReach)
					for (int i = 0; i < 219; i++)
						Bitfield_MPObjects.Items.Add(CheckBoxes[i]);
				if (_mapInfo.MapInformation.Game == MapInfo.GameIdentifier.Halo4NetworkTest)
					for (int i = 0; i < 244; i++)
						Bitfield_MPObjects.Items.Add(CheckBoxes[i]);
				if (_mapInfo.MapInformation.Game == MapInfo.GameIdentifier.Halo4)
					for (int i = 0; i < 289; i++)
						Bitfield_MPObjects.Items.Add(CheckBoxes[i]);
			}
		}

		private void LoadInsertionPoints()
		{
			// Create blank insertion points
			for (int i = 0; i < 12; i++)
			{
				InsertionPointList.Add(new InsertionPoint());
				InsertionPointList[i].ZoneName = "";
				InsertionPointList[i].IPEnabled = false;
				InsertionPointList[i].IPVisible = false;
				IList<string> ipNames = new List<string>();
				IList<string> ipDescs = new List<string>();
				InsertionPointList[i].IPNames = ipNames;
				InsertionPointList[i].IPDescriptions = ipDescs;
				for (int ni = 0; ni < 17; ni++)
				{
					InsertionPointList[i].IPNames.Add("");
					InsertionPointList[i].IPDescriptions.Add("");
				}
			}

			switch (_mapInfo.MapInformation.Game)
			{
				case MapInfo.GameIdentifier.Halo3:
					insertionPointCount = 4;
					cbInsertUsed.IsEnabled = false;
					cbInsertUsed.Visibility = Visibility.Collapsed;
					txtZoneName.IsEnabled = false;
					txtZoneName.Visibility = Visibility.Collapsed;
					lblZoneName.Text = "Zone Index:";
					break;
				case MapInfo.GameIdentifier.Halo3ODST:
					insertionPointCount = 9;
					cbInsertUsed.IsEnabled = false;
					cbInsertUsed.Visibility = Visibility.Collapsed;
					txtZoneName.IsEnabled = false;
					txtZoneName.Visibility = Visibility.Collapsed;
					lblZoneName.Text = "Zone Index:";
					break;
				case MapInfo.GameIdentifier.HaloReachBetas:
				case MapInfo.GameIdentifier.HaloReach:
				case MapInfo.GameIdentifier.Halo4NetworkTest:
				case MapInfo.GameIdentifier.Halo4:
					insertionPointCount = 12;
					txtZoneIndex.IsEnabled = false;
					txtZoneIndex.Visibility = Visibility.Collapsed;
					break;
			}

			for (int i = 0; i < 12; i++)
			{
				ComboBoxItem cbi = (ComboBoxItem)cbInsertIndex.Items[i];
				if (i >= insertionPointCount)
				{
					cbi.IsEnabled = false;
					cbi.Visibility = System.Windows.Visibility.Collapsed;
				}
			}

			for (int i = 0; i < insertionPointCount; i++)
			{
				InsertionPointList[i].ZoneName = _mapInfo.MapInformation.MapCheckpoints[i].ZoneName;
				InsertionPointList[i].ZoneIndex = _mapInfo.MapInformation.MapCheckpoints[i].ZoneIndex;
				if (_mapInfo.MapInformation.MapCheckpoints[i].IsUsed == true)
					InsertionPointList[i].IPEnabled = true;
				if (_mapInfo.MapInformation.MapCheckpoints[i].IsUsed == false)
					InsertionPointList[i].IPEnabled = false;
				if (_mapInfo.MapInformation.MapCheckpoints[i].IsVisible == true)
					InsertionPointList[i].IPVisible = true;
				if (_mapInfo.MapInformation.MapCheckpoints[i].IsVisible == false)
					InsertionPointList[i].IPVisible = false;

				if (_mapInfo.MapInformation.Game != MapInfo.GameIdentifier.Halo4)
				{
					for (int n = 0; n < 12; n++)
					{
						InsertionPointList[i].IPNames[n] = _mapInfo.MapInformation.MapCheckpoints[i].CheckpointName[n];
						InsertionPointList[i].IPDescriptions[n] = _mapInfo.MapInformation.MapCheckpoints[i].CheckpointDescription[n];
					}
				}

				if (_mapInfo.MapInformation.Game == MapInfo.GameIdentifier.Halo4)
				{
					for (int n = 0; n < 17; n++)
					{
						InsertionPointList[i].IPNames[n] = _mapInfo.MapInformation.MapCheckpoints[i].CheckpointName[n];
						InsertionPointList[i].IPDescriptions[n] = _mapInfo.MapInformation.MapCheckpoints[i].CheckpointDescription[n];
					}
				}

				if (cbInsertIndex.SelectedIndex != -1)
				{
					txtZoneName.Text = InsertionPointList[cbInsertIndex.SelectedIndex].ZoneName;
					txtZoneIndex.Text = InsertionPointList[cbInsertIndex.SelectedIndex].ZoneIndex.ToString();
					if (_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].IsUsed == true)
						cbInsertUsed.IsChecked = true;
					if (_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].IsUsed == false)
						cbInsertUsed.IsChecked = false;
					if (_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].IsVisible == true)
						cbInsertVisible.IsChecked = true;
					if (_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].IsVisible == false)
						cbInsertVisible.IsChecked = false;
					txtInsertName.Text = InsertionPointList[cbInsertIndex.SelectedIndex].IPNames[cbInsertLanguages.SelectedIndex];
					txtInsertDesc.Text = InsertionPointList[cbInsertIndex.SelectedIndex].IPDescriptions[cbInsertLanguages.SelectedIndex];
				}
			}
		}

		private void UpdateMPObjects()
		{
			for (int i = 0; i < Bitfield_MPObjects.Items.Count; i++)
			{
				CheckBox cb = (CheckBox)Bitfield_MPObjects.Items[i];
				if (cb.IsChecked == true)
					_mapInfo.MapInformation.ObjectTable[i] = true;
				else
					_mapInfo.MapInformation.ObjectTable[i] = false;
			}
		}

		private void UpdateInsertionPoints()
		{
			// Save the current Index Values
			InsertionPointList[cbInsertIndex.SelectedIndex].ZoneName = txtZoneName.Text.Trim();
			if (!Equals(txtZoneIndex.BorderBrush, FindResource("ExtryzeAccentBrush")))
				InsertionPointList[cbInsertIndex.SelectedIndex].ZoneIndex = Byte.Parse(txtZoneIndex.Text);

			if (Equals(txtZoneIndex.BorderBrush, FindResource("ExtryzeAccentBrush")))
			{
				Close();
				MetroMessageBox.Show("MapInfo Not Saved", "The MapInfo could not be saved because the Zone Index is invalid.");
				return;
			}

			if (cbInsertVisible.IsChecked == true)
				InsertionPointList[cbInsertIndex.SelectedIndex].IPVisible = true;
			if (cbInsertVisible.IsChecked == false)
				InsertionPointList[cbInsertIndex.SelectedIndex].IPVisible = false;
			if (cbInsertUsed.IsChecked == true)
				InsertionPointList[cbInsertIndex.SelectedIndex].IPEnabled = true;
			if (cbInsertUsed.IsChecked == false)
				InsertionPointList[cbInsertIndex.SelectedIndex].IPEnabled = false;

			InsertionPointList[cbInsertIndex.SelectedIndex].IPNames[cbInsertLanguages.SelectedIndex] = txtInsertName.Text;
			InsertionPointList[cbInsertIndex.SelectedIndex].IPDescriptions[cbInsertLanguages.SelectedIndex] = txtInsertDesc.Text;

			// Update the Insertion Points
			for (int i = 0; i < insertionPointCount; i++)
			{
				_mapInfo.MapInformation.MapCheckpoints[i].IsVisible = InsertionPointList[i].IPVisible;
				_mapInfo.MapInformation.MapCheckpoints[i].IsUsed = InsertionPointList[i].IPEnabled;
				_mapInfo.MapInformation.MapCheckpoints[i].ZoneName = InsertionPointList[i].ZoneName;
				_mapInfo.MapInformation.MapCheckpoints[i].ZoneIndex = InsertionPointList[i].ZoneIndex;
				for (int n = 0; n < _languageset.Count; n++)
				{
					_mapInfo.MapInformation.MapCheckpoints[i].CheckpointName[n] = InsertionPointList[i].IPNames[n];
					_mapInfo.MapInformation.MapCheckpoints[i].CheckpointDescription[n] = InsertionPointList[i].IPDescriptions[n];
				}
			}
		}

		public class LanguageEntry
		{
			public string Language { get; set; }
			public int Index { get; set; }
			public string LanguageShort { get; set; }
		}

		private void cbInsertLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (insertionOldLanguage != -1)
			{
				// Save values to memory
				InsertionPointList[cbInsertIndex.SelectedIndex].IPNames[insertionOldLanguage] = txtInsertName.Text.Trim();
				InsertionPointList[cbInsertIndex.SelectedIndex].IPDescriptions[insertionOldLanguage] = txtInsertDesc.Text.Trim();

				// Make sure values aren't too long, kiddo
				if (InsertionPointList[cbInsertIndex.SelectedIndex].IPNames[insertionOldLanguage].Length > 32)
					InsertionPointList[cbInsertIndex.SelectedIndex].IPNames[insertionOldLanguage] = InsertionPointList[cbInsertIndex.SelectedIndex].IPNames[insertionOldLanguage].Remove(32);
				if (InsertionPointList[cbInsertIndex.SelectedIndex].IPDescriptions[insertionOldLanguage].Length > 128)
					InsertionPointList[cbInsertIndex.SelectedIndex].IPDescriptions[insertionOldLanguage] = InsertionPointList[cbInsertIndex.SelectedIndex].IPDescriptions[insertionOldLanguage].Remove(128);

			}
			// Update insertionOldLanguage int
			insertionOldLanguage = cbInsertLanguages.SelectedIndex;

			// Update UI
			txtInsertName.Text = InsertionPointList[cbInsertIndex.SelectedIndex].IPNames[insertionOldLanguage];
			txtInsertDesc.Text = InsertionPointList[cbInsertIndex.SelectedIndex].IPDescriptions[insertionOldLanguage];
		}

		private void cbInsertIndex_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (oldIndex == -1)
				cbInsertLanguages.SelectedIndex = 0;

			if (oldIndex != -1)
			{
				if (insertionOldLanguage != -1)
				{
					// Save values to memory
					InsertionPointList[oldIndex].IPNames[insertionOldLanguage] = txtInsertName.Text.Trim();
					InsertionPointList[oldIndex].IPDescriptions[insertionOldLanguage] = txtInsertDesc.Text.Trim();

					// Make sure values aren't too long, kiddo
					if (InsertionPointList[oldIndex].IPNames[insertionOldLanguage].Length > 32)
						InsertionPointList[oldIndex].IPNames[insertionOldLanguage] = InsertionPointList[cbInsertIndex.SelectedIndex].IPNames[insertionOldLanguage].Remove(32);
					if (InsertionPointList[oldIndex].IPDescriptions[insertionOldLanguage].Length > 128)
						InsertionPointList[oldIndex].IPDescriptions[insertionOldLanguage] = InsertionPointList[cbInsertIndex.SelectedIndex].IPDescriptions[insertionOldLanguage].Remove(128);
				}

				// Save values to memory
				InsertionPointList[oldIndex].ZoneName = txtZoneName.Text.Trim();
				if (!Equals(txtZoneIndex.BorderBrush, FindResource("ExtryzeAccentBrush")))
					InsertionPointList[oldIndex].ZoneIndex = Byte.Parse(txtZoneIndex.Text);

				// If the Zone Index is invalid, don't allow the index to change
				if (Equals(txtZoneIndex.BorderBrush, FindResource("ExtryzeAccentBrush")))
				{
					cbInsertIndex.SelectedIndex = oldIndex;
					return;
				}
				if (cbInsertVisible.IsChecked == true)
					InsertionPointList[oldIndex].IPVisible = true;
				if (cbInsertVisible.IsChecked == false)
					InsertionPointList[oldIndex].IPVisible = false;
				if (cbInsertUsed.IsChecked == true)
					InsertionPointList[oldIndex].IPEnabled = true;
				if (cbInsertUsed.IsChecked == false)
					InsertionPointList[oldIndex].IPEnabled = false;
			}

			// Update oldLanguage int
			oldIndex = cbInsertIndex.SelectedIndex;

			// Update UI
			txtZoneName.Text = InsertionPointList[oldIndex].ZoneName;
			txtZoneIndex.Text = InsertionPointList[oldIndex].ZoneIndex.ToString();
			if (InsertionPointList[oldIndex].IPVisible == true)
				cbInsertVisible.IsChecked = true;
			if (InsertionPointList[oldIndex].IPVisible == false)
				cbInsertVisible.IsChecked = false;
			if (InsertionPointList[oldIndex].IPEnabled == true)
				cbInsertUsed.IsChecked = true;
			if (InsertionPointList[oldIndex].IPEnabled == false)
				cbInsertUsed.IsChecked = false;
			if (insertionOldLanguage != -1)
			{
				txtInsertName.Text = InsertionPointList[cbInsertIndex.SelectedIndex].IPNames[insertionOldLanguage];
				txtInsertDesc.Text = InsertionPointList[cbInsertIndex.SelectedIndex].IPDescriptions[insertionOldLanguage];
			}
		}
	}
}