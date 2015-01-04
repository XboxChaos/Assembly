using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components;
using Assembly.Metro.Dialogs;
using Assembly.Windows;
using Xceed.Wpf.AvalonDock.Layout;
using Blamite.Blam.ThirdGen;
using Blamite.Blam.ThirdGen.BLF;
using Blamite.Serialization.MapInfo;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
	/// <summary>
	///     Interaction logic for HaloInfo.xaml
	/// </summary>
	public partial class HaloInfo
	{
		private readonly LayoutDocument _tab;
		private readonly string _blfLocation;

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

		private readonly ObservableCollection<LanguageEntry> _halo4Languages = new ObservableCollection<LanguageEntry>
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

		private readonly List<TextBox> _maxTeamTextBoxes = new List<TextBox>();

		private PureBLF _blf;
		private MapInfo _mapInfo;
		private bool _startEditing;
		private int _oldLanguage = -1;
		private int _oldInsertionLanguage = -1;
		private int _oldInsertionIndex = -1;

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

		private bool IsTextBoxValid(TextBox textbox)
		{
			return (Equals(textbox.BorderBrush, FindResource("ExtryzeAccentBrush")));
		}

		private void NumericalTextBoxOnTextChanged(object sender, TextChangedEventArgs textChangedEventArgs)
		{
			var textbox = sender as TextBox;
			if (textbox == null)
				return;

			Byte tmp8;
			if (Byte.TryParse(textbox.Text, out tmp8))
				textbox.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF595959");
			else
				textbox.BorderBrush = (Brush)FindResource("ExtryzeAccentBrush");
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

		#region Loading Code

		public void LoadMapInfo()
		{
			try
			{
				// Just a lazy way to validate the BLF file
				_blf = new PureBLF(_blfLocation);
				if (_blf.BLFChunks[1].ChunkMagic != "levl")
					throw new Exception("The selected Map Info BLF is not a valid Map Info BLF file.");
				_blf.Close();

				_mapInfo = new MapInfo(_blfLocation, App.AssemblyStorage.AssemblySettings.DefaultMapInfoDatabase);

				Dispatcher.Invoke(new Action(delegate
				{

					// Add BLF Info
					paneBLFInfo.Children.Insert(0, new MapHeaderEntry("MapInfo Version:", _mapInfo.Engine.Version.ToString(CultureInfo.InvariantCulture)));
					paneBLFInfo.Children.Insert(1, new MapHeaderEntry("BLF Length:", "0x" + _mapInfo.Stream.Length.ToString("X")));
					paneBLFInfo.Children.Insert(2, new MapHeaderEntry("BLF Chunks:", _blf.BLFChunks.Count.ToString(CultureInfo.InvariantCulture)));
					
					// Hide unused elements
					if (_mapInfo.Engine.MaxTeamCollection == null)
						tiMaxTeams.Visibility = Visibility.Collapsed;
					if (_mapInfo.Engine.MultiplayerObjectCollection == null)
						tiMPObjects.Visibility = Visibility.Collapsed;
					if (!_mapInfo.Engine.UsesDefaultAuthor)
					{
						lblDefaultAuthor.Visibility = Visibility.Collapsed;
						txtDefaultAuthor.Visibility = Visibility.Collapsed;
					}

					// Load Languages
					LoadLanguages();

					// Load Map Info
					txtGameName.Text = _mapInfo.Engine.Name;
					txtMapID.Text = _mapInfo.MapInformation.MapID.ToString(CultureInfo.InvariantCulture);
					txtMapInternalName.Text = _mapInfo.MapInformation.InternalName;
					txtMapPhysicalName.Text = _mapInfo.MapInformation.PhysicalName;

					// Load Default Author & change margin if necessary
					txtDefaultAuthor.Text = _mapInfo.MapInformation.DefaultAuthor;
					if (_mapInfo.Engine.UsesDefaultAuthor && _mapInfo.Engine.Version <= 8)
							lblDefaultAuthor.Margin = new Thickness(0, 37, 0, 3);

					// Set up the Type combo box
					// TODO: Add flags to formats?
					cbType_Cine.Visibility = cbType_FF.Visibility = _mapInfo.Engine.Version < 5 ? Visibility.Collapsed : Visibility.Visible;
					cbType_Cine.IsEnabled = cbType_FF.IsEnabled = _mapInfo.Engine.Version >= 5;
					cbType_FF.Content = _mapInfo.Engine.Version < 8 ? "Firefight" : "Spartan Ops";
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
					cbForgeOnly.Visibility  = _mapInfo.Engine.Version < 9 ? Visibility.Collapsed : Visibility.Visible;
					cbVisible.IsChecked = _mapInfo.MapInformation.Flags.HasFlag(LevelFlags.Visible);
					cbGeneratesFilm.IsChecked = _mapInfo.MapInformation.Flags.HasFlag(LevelFlags.GeneratesFilm);
					cbDLC.IsChecked = _mapInfo.MapInformation.Flags.HasFlag(LevelFlags.IsDLC);
					cbForgeOnly.IsChecked = _mapInfo.MapInformation.Flags.HasFlag(LevelFlags.IsForgeOnly);

					// Load Max Teams
					if (_mapInfo.Engine.MaxTeamCollection != null)
						LoadMaxTeams();

					// Load MP Objects
					if (_mapInfo.Engine.MultiplayerObjectCollection != null)
						LoadMultiplayerObjects();

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
					MetroMessageBox.Show("Unable to open MapInfo", ex.Message);
					App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(_tab);
					Close();
				}));
			}
		}

		private void LoadLanguages()
		{
			// If the game is Halo 4, use that set of languages, if not, use the default set.
			cbLanguages.DataContext = cbInsertLanguages.DataContext = _mapInfo.Engine.LanguageCount > 12 ? _halo4Languages : _languages;
		}

		private void LoadMaxTeams()
		{
			foreach (var maxTeam in _mapInfo.Engine.MaxTeamCollection)
			{
				// Create a TextBlock to show the object's friendly name
				var textBlock = new TextBlock
				{
					Text = maxTeam.Name + ":",
					Margin = maxTeam.Index < 3 ? new Thickness(0, 10, 0, 3) : new Thickness(0, 7, 0, 3),
					VerticalAlignment = VerticalAlignment.Center,
					Tag = maxTeam.Index
				};
				textBlock.SetResourceReference(StyleProperty, "GenericTextblock");

				// Create the TextBox to hold the data
				var textBox = new TextBox
				{
					Text = _mapInfo.MapInformation.MaxTeamCounts[maxTeam.Index].ToString(CultureInfo.InvariantCulture),
					Margin = maxTeam.Index < 3 ? new Thickness(10, 7, 0, 0) : new Thickness(10, 4, 0, 0),
					MaxLength = 3,
					Tag = maxTeam.Index
				};
				textBox.TextChanged += NumericalTextBoxOnTextChanged;
				_maxTeamTextBoxes.Add(textBox);

				// Put the TextBlock and TextBox in the correct column
				switch (maxTeam.Index % 3)
				{
					case 0:
						stkRow1Names.Children.Add(textBlock);
						stkRow1TextBoxes.Children.Add(textBox);
						break;
					case 1:
						stkRow2Names.Children.Add(textBlock);
						stkRow2TextBoxes.Children.Add(textBox);
						break;
					case 2:
						stkRow3Names.Children.Add(textBlock);
						stkRow3TextBoxes.Children.Add(textBox);
						break;
				}
			}
		}

		private void LoadMultiplayerObjects()
		{
			foreach (var mpObject in _mapInfo.Engine.MultiplayerObjectCollection)
			{
				// Create a CheckBox to show the object's friendly name
				var checkBox = new CheckBox
				{
					Content = mpObject.Name,
					Tag = mpObject.Index,
					IsChecked = _mapInfo.MapInformation.ObjectTable[mpObject.Index]
				};

				// Add the checkBox to the ListBox
				bfMPObjects.Items.Add(checkBox);
			}
		}

		private void LoadInsertionPoints()
		{
			for (int i = 0; i < _mapInfo.Engine.InsertionCount; i++)
				cbInsertIndex.Items.Add(new ComboBoxItem { Content = i });

			if (!_mapInfo.Engine.InsertionUsesVisibility)
				cbInsertVisible.Visibility = Visibility.Collapsed;
			if (!_mapInfo.Engine.InsertionUsesUsage)
				cbInsertUsed.Visibility = Visibility.Collapsed;
			if (_mapInfo.Engine.InsertionZoneType != ZoneType.Index)
				txtZoneIndex.Visibility = Visibility.Collapsed;
			if (_mapInfo.Engine.InsertionZoneType != ZoneType.Name)
				txtZoneName.Visibility = Visibility.Collapsed;
		}

		#endregion

		#region Update Code

		private void UpdateMapInfo()
		{
			var errorStrings = new List<string>();

			_mapInfo = new MapInfo(_blfLocation, App.AssemblyStorage.AssemblySettings.DefaultMapInfoDatabase);
			if (IsTextBoxValid(txtMapID))
				errorStrings.Add("Map ID");
			if (IsTextBoxValid(txtZoneIndex))
				errorStrings.Add("Insertion Point " + cbInsertIndex.SelectedIndex + " Zone Index");
			errorStrings.AddRange(from textBox in _maxTeamTextBoxes where IsTextBoxValid(textBox) select _mapInfo.Engine.MaxTeamCollection.GetLayout((int)textBox.Tag).Name + " Max Teams");

			if (errorStrings.Count != 0)
			{
				Close();
				MetroMessageBox.Show("MapInfo Not Saved", errorStrings.Aggregate("The data given for the following is either not a valid number or too large.\n", (current, error) => current + ("\n" + error)));
				return;
			}

			// Update MapID
			if (!Equals(txtMapID.BorderBrush, FindResource("ExtryzeAccentBrush")))
				_mapInfo.MapInformation.MapID = Int32.Parse(txtMapID.Text);

			// Check if MapID was invalid, if so tell user.
			if (Equals(txtMapID.BorderBrush, FindResource("ExtryzeAccentBrush")))
			{
				Close();
				MetroMessageBox.Show("MapID Not Saved", "The MapID was not saved into the MapInfo. Change the MapID to a valid number, then save again.");
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

			// Update Max Teams
			if (_mapInfo.Engine.MaxTeamCollection != null)
				UpdateMaxTeams();

			// Update MP Objects
			if (_mapInfo.Engine.MultiplayerObjectCollection != null)
				UpdateMultiplayerObjects();

			// Update Default Author
			if (_mapInfo.Engine.UsesDefaultAuthor)
				_mapInfo.MapInformation.DefaultAuthor = txtDefaultAuthor.Text;

			// Update Insertion Points
			UpdateInsertionPoints();

			// Write all changes to file
			_mapInfo.UpdateMapInfo();
			Close();
			MetroMessageBox.Show("Save Successful", "Your MapInfo has been saved.");
			App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(_tab);
		}

		private void UpdateMaxTeams()
		{
			foreach (var textBox in _maxTeamTextBoxes)
				_mapInfo.MapInformation.MaxTeamCounts[(int)textBox.Tag] = Byte.Parse(textBox.Text);
		}

		private void UpdateMultiplayerObjects()
		{
			foreach (CheckBox checkBox in bfMPObjects.Items)
				if (checkBox.IsChecked != null)
					_mapInfo.MapInformation.ObjectTable[(int)checkBox.Tag] = (bool)checkBox.IsChecked;
		}

		private void UpdateInsertionPoints()
		{
			// Save the current Index Values
			_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].ZoneName = txtZoneName.Text.Trim();
			if (Equals(txtZoneIndex.BorderBrush, FindResource("ExtryzeAccentBrush")))
			{
				Close();
				MetroMessageBox.Show("MapInfo Not Saved", "The MapInfo could not be saved because the Insertion Point Zone Index is invalid.");
				return;
			}
			_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].ZoneIndex = Byte.Parse(txtZoneIndex.Text);
			if (cbInsertVisible.IsChecked != null)
				_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].IsVisible = (bool)(cbInsertVisible.IsChecked);
			if (cbInsertUsed.IsChecked != null)
				_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].IsUsed = (cbInsertUsed.IsChecked == false);
			_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointNames[cbInsertLanguages.SelectedIndex] = txtInsertName.Text;
			_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointDescriptions[cbInsertLanguages.SelectedIndex] = txtInsertDesc.Text;

		}

		#endregion

		#region Event Handlers

		private void txtMapID_TextChanged(object sender, TextChangedEventArgs e)
		{
			Int32 tmp32;
			if (Int32.TryParse(txtMapID.Text, out tmp32))
				txtMapID.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF595959");
			else
				txtMapID.BorderBrush = (Brush)FindResource("ExtryzeAccentBrush");
		}

		private void btnUpdate_Click(object sender, RoutedEventArgs e)
		{
			UpdateMapInfo();
		}

		private void cbLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_mapInfo != null && _startEditing)
			{
				if (_oldLanguage != -1)
				{
					// Save values to memory
					_mapInfo.MapInformation.MapNames[_oldLanguage] = txtMapName.Text.Trim();
					_mapInfo.MapInformation.MapDescriptions[_oldLanguage] = txtMapDesc.Text.Trim();

					// Make sure values arn't too long, kiddo
					if (_mapInfo.MapInformation.MapNames[_oldLanguage].Length > 31)
						_mapInfo.MapInformation.MapNames[_oldLanguage] = _mapInfo.MapInformation.MapNames[_oldLanguage].Remove(31);
					if (_mapInfo.MapInformation.MapDescriptions[_oldLanguage].Length > 127)
						_mapInfo.MapInformation.MapDescriptions[_oldLanguage] = _mapInfo.MapInformation.MapDescriptions[_oldLanguage].Remove(127);
				}

				// Update oldLanguage int
				_oldLanguage = cbLanguages.SelectedIndex;

				// Update UI
				txtMapName.Text = _mapInfo.MapInformation.MapNames[_oldLanguage];
				txtMapDesc.Text = _mapInfo.MapInformation.MapDescriptions[_oldLanguage];
			}
		}

		private void cbInsertIndex_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_oldInsertionIndex == -1)
				cbInsertLanguages.SelectedIndex = 0;

			// Save values to memory
			if (_oldInsertionIndex != -1)
			{
				if (_oldInsertionLanguage != -1)
				{
					_mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].CheckpointNames[_oldInsertionLanguage] = txtInsertName.Text.Trim();
					_mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].CheckpointDescriptions[_oldInsertionLanguage] = txtInsertDesc.Text.Trim();

					// Make sure values aren't too long, kiddo
					if (_mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].CheckpointNames[_oldInsertionLanguage].Length > 31)
						_mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].CheckpointNames[_oldInsertionLanguage]
							= _mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointNames[_oldInsertionLanguage].Remove(31);
					if (_mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].CheckpointDescriptions[_oldInsertionLanguage].Length > 127)
						_mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].CheckpointDescriptions[_oldInsertionLanguage]
							= _mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointDescriptions[_oldInsertionLanguage].Remove(127);
				}

				_mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].ZoneName = txtZoneName.Text.Trim();
				if (!Equals(txtZoneIndex.BorderBrush, FindResource("ExtryzeAccentBrush")))
					_mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].ZoneIndex = Byte.Parse(txtZoneIndex.Text);
				if (cbInsertVisible.IsChecked != null)
					_mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].IsVisible = ((bool)cbInsertVisible.IsChecked);
				if (cbInsertUsed.IsChecked != null)
					_mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].IsUsed = ((bool)cbInsertUsed.IsChecked);

				// If the Zone Index is an invalid number or too large, prevent the user from changing the index
				if (Equals(txtZoneIndex.BorderBrush, FindResource("ExtryzeAccentBrush")))
				{
					cbInsertIndex.SelectedIndex = _oldInsertionIndex;
					return;
				}
			}

			// Update oldLanguage int
			_oldInsertionIndex = cbInsertIndex.SelectedIndex;

			// Update UI
			txtZoneName.Text = _mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].ZoneName;
			txtZoneIndex.Text = _mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].ZoneIndex.ToString();
			cbInsertVisible.IsChecked = _mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].IsVisible;
			cbInsertUsed.IsChecked = _mapInfo.MapInformation.MapCheckpoints[_oldInsertionIndex].IsUsed;
			if (_oldInsertionLanguage != -1)
			{
				txtInsertName.Text = _mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointNames[_oldInsertionLanguage];
				txtInsertDesc.Text = _mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointDescriptions[_oldInsertionLanguage];
			}
		}

		private void cbInsertLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_oldInsertionLanguage != -1)
			{
				// Save values to memory
				_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointNames[_oldInsertionLanguage] = txtInsertName.Text.Trim();
				_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointDescriptions[_oldInsertionLanguage] = txtInsertDesc.Text.Trim();

				// Make sure values aren't too long, kiddo
				if (_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointNames[_oldInsertionLanguage].Length > 31)
					_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointNames[_oldInsertionLanguage] =
						_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointNames[_oldInsertionLanguage].Remove(31);
				if (_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointDescriptions[_oldInsertionLanguage].Length > 127)
					_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointDescriptions[_oldInsertionLanguage] =
						_mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointDescriptions[_oldInsertionLanguage].Remove(127);

			}
			// Update _oldInsertionLanguage int
			_oldInsertionLanguage = cbInsertLanguages.SelectedIndex;

			// Update UI
			txtInsertName.Text = _mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointNames[_oldInsertionLanguage];
			txtInsertDesc.Text = _mapInfo.MapInformation.MapCheckpoints[cbInsertIndex.SelectedIndex].CheckpointDescriptions[_oldInsertionLanguage];
		}

		#endregion

		public class LanguageEntry
		{
			public string Language { get; set; }
			public int Index { get; set; }
			public string LanguageShort { get; set; }
		}
	}
}