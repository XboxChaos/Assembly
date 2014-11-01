using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
	///     Interaction logic for HaloCampaign.xaml
	/// </summary>
	public partial class HaloCampaign
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

		private List<TextBox> GetTextBoxes()
		{
			return new List<TextBox>
			{
				txtMap1,
				txtMap2,
				txtMap3,
				txtMap4,
				txtMap5,
				txtMap6,
				txtMap7,
				txtMap8,
				txtMap9,
				txtMap10,
				txtMap11,
				txtMap12,
				txtMap13,
				txtMap14,
				txtMap15,
				txtMap16,
				txtMap17,
				txtMap18,
				txtMap19,
				txtMap20,
				txtMap21,
				txtMap22,
				txtMap23,
				txtMap24,
				txtMap25,
				txtMap26,
				txtMap27,
				txtMap28,
				txtMap29,
				txtMap30,
				txtMap31,
				txtMap32,
				txtMap33,
				txtMap34,
				txtMap35,
				txtMap36,
				txtMap37,
				txtMap38,
				txtMap39,
				txtMap40,
				txtMap41,
				txtMap42,
				txtMap43,
				txtMap44,
				txtMap45,
				txtMap46,
				txtMap47,
				txtMap48,
				txtMap49,
				txtMap50,
				txtMap51,
				txtMap52,
				txtMap53,
				txtMap54,
				txtMap55,
				txtMap56,
				txtMap57,
				txtMap58,
				txtMap59,
				txtMap60,
				txtMap61,
				txtMap62,
				txtMap63,
				txtMap64
			};
		}

		private List<ToggleButton> GetToggleButtons()
		{
			return new List<ToggleButton>
			{
				cbMap1,
				cbMap2,
				cbMap3,
				cbMap4,
				cbMap5,
				cbMap6,
				cbMap7,
				cbMap8,
				cbMap9,
				cbMap10,
				cbMap11,
				cbMap12,
				cbMap13,
				cbMap14,
				cbMap15,
				cbMap16,
				cbMap17,
				cbMap18,
				cbMap19,
				cbMap20,
				cbMap21,
				cbMap22,
				cbMap23,
				cbMap24,
				cbMap25,
				cbMap26,
				cbMap27,
				cbMap28,
				cbMap29,
				cbMap30,
				cbMap31,
				cbMap32,
				cbMap33,
				cbMap34,
				cbMap35,
				cbMap36,
				cbMap37,
				cbMap38,
				cbMap39,
				cbMap40,
				cbMap41,
				cbMap42,
				cbMap43,
				cbMap44,
				cbMap45,
				cbMap46,
				cbMap47,
				cbMap48,
				cbMap49,
				cbMap50,
				cbMap51,
				cbMap52,
				cbMap53,
				cbMap54,
				cbMap55,
				cbMap56,
				cbMap57,
				cbMap58,
				cbMap59,
				cbMap60,
				cbMap61,
				cbMap62,
				cbMap63,
				cbMap64
			};
		}

		private PureBLF _blf;
		private Campaign _campaign;
		private bool _startEditing;
		private int oldLanguage = -1;

		public HaloCampaign(string infoLocation, LayoutDocument tab)
		{
			InitializeComponent();
			_blfLocation = infoLocation;

			var fi = new FileInfo(_blfLocation);
			_tab = tab;
			tab.Title = fi.Name;
			lblBLFname.Text = fi.Name;

			var thrd = new Thread(LoadCampaign);
			thrd.SetApartmentState(ApartmentState.STA);
			thrd.Start();
		}

		public void LoadCampaign()
		{
			try
			{
				// Just a lazy way to validate the BLF file
				_blf = new PureBLF(_blfLocation);
				if (_blf.BLFChunks[1].ChunkMagic != "cmpn")
					throw new Exception("The selected Campaign BLF is not a valid Campaign BLF file.");
				_blf.Close();

				_campaign = new Campaign(_blfLocation);

				Dispatcher.Invoke(new Action(delegate
				{
					// Add BLF Info
					paneBLFInfo.Children.Insert(0, new MapHeaderEntry("BLF Length:", "0x" + _campaign.Stream.Length.ToString("X")));
					paneBLFInfo.Children.Insert(1,
						new MapHeaderEntry("BLF Chunks:", _blf.BLFChunks.Count.ToString(CultureInfo.InvariantCulture)));

					// Load Languages
					LoadLanguages();

					// Load Map IDs
					LoadMapIDs();

					// Load Unlock Bytes
					LoadUnlockBytes();

					// Update UI
					_startEditing = true;
					cbLanguages.SelectedIndex = 0;

					if (App.AssemblyStorage.AssemblySettings.StartpageHideOnLaunch)
						App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(Home.TabGenre.StartPage);

					RecentFiles.AddNewEntry(new FileInfo(_blfLocation).Name, _blfLocation, "Campaign", Settings.RecentFileType.Campaign);
					Close();
				}));
			}
			catch (Exception ex)
			{
				Dispatcher.Invoke(new Action(delegate
				{
					MetroMessageBox.Show("Unable to open Campaign", ex.Message);
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
				_campaign.Close();
			}
			catch
			{
			}
			return true;
		}

		// Update Languages
		private void cbLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_campaign != null && _startEditing)
			{
				if (oldLanguage != -1)
				{
					// Save values to memory
					_campaign.HaloCampaign.MapNames[oldLanguage] = txtMapName.Text.Trim();
					_campaign.HaloCampaign.MapDescriptions[oldLanguage] = txtMapDesc.Text.Trim();

					// Make sure values arn't too long, kiddo
					if (_campaign.HaloCampaign.MapNames[oldLanguage].Length > 30)
						_campaign.HaloCampaign.MapNames[oldLanguage] = _campaign.HaloCampaign.MapNames[oldLanguage].Remove(30);
					if (_campaign.HaloCampaign.MapDescriptions[oldLanguage].Length > 126)
						_campaign.HaloCampaign.MapDescriptions[oldLanguage] =
							_campaign.HaloCampaign.MapDescriptions[oldLanguage].Remove(126);
				}

				// Update oldLanguage int
				oldLanguage = cbLanguages.SelectedIndex;

				// Update UI
				txtMapName.Text = _campaign.HaloCampaign.MapNames[oldLanguage];
				txtMapDesc.Text = _campaign.HaloCampaign.MapDescriptions[oldLanguage];
			}
		}

		// Update Campaign file
		private void btnUpdate_Click(object sender, RoutedEventArgs e)
		{
			_campaign = new Campaign(_blfLocation);
			
			// Update Current Map Name/Descrption Language Selection
			_campaign.HaloCampaign.MapNames[cbLanguages.SelectedIndex] = txtMapName.Text;
			_campaign.HaloCampaign.MapDescriptions[cbLanguages.SelectedIndex] = txtMapDesc.Text;

			if (MapIDsError() == true)
				return;

			// Update Map IDs
			UpdateMapIDs();

			// Update Unlock Bytes
			UpdateUnlockBytes();

			// Write all changes to file
			_campaign.UpdateCampaign();
			Close();
			MetroMessageBox.Show("Save Successful", "Your Campaign has been saved.");
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
			switch (_campaign.HaloCampaign.Game)
			{
				case Campaign.GameIdentifier.Halo4:
					_languageset = _halo4languages;
					break;
				default:
					_languageset = _languages;
					break;
			}
			cbLanguages.DataContext = _languageset;
		}

		private void LoadMapIDs()
		{
			foreach (var textbox in GetTextBoxes())
				textbox.Text = _campaign.HaloCampaign.MapIDs[int.Parse((string)textbox.Tag)].ToString(CultureInfo.InvariantCulture);
		}

		private void LoadUnlockBytes()
		{
			if (_campaign.HaloCampaign.Game == Campaign.GameIdentifier.Halo4)
			{
				lblToggleNote.Visibility = Visibility.Visible;
				foreach (var toggleButton in GetToggleButtons())
					toggleButton.IsChecked = Convert.ToBoolean(_campaign.HaloCampaign.UnlockBytes[int.Parse((string) toggleButton.Tag)]);
			}
			else
			{
				foreach (var toggleButton in GetToggleButtons())
					toggleButton.IsEnabled = false;
			}
		}

		// Check if any Map IDs are invalid
		private bool MapIDsError()
		{
			for (int i = 1; i <= 7; i += 2)
			{
				var sp = (StackPanel)gridMapIds.Children[i];
				if (sp.Children.Cast<TextBox>().Any(tb => Equals(tb.BorderBrush, FindResource("ExtryzeAccentBrush"))))
				{
					Close();
					MetroMessageBox.Show("Campaign Not Saved", "The Campaign could not be saved because one or more of the Map IDs is invalid.");
					return true;
				}
			}
			return false;
		}

		private void UpdateMapIDs()
		{
			foreach (var textbox in GetTextBoxes())
				_campaign.HaloCampaign.MapIDs[int.Parse((string) textbox.Tag)] = int.Parse(textbox.Text);
		}

		private void UpdateUnlockBytes()
		{
			if (_campaign.HaloCampaign.Game != Campaign.GameIdentifier.Halo4)
				return;

			foreach (var toggleButton in GetToggleButtons())
				_campaign.HaloCampaign.UnlockBytes[int.Parse((string)toggleButton.Tag)] = Convert.ToByte(toggleButton.IsChecked);
		}

		private void MapIDValidityCheck(object sender, TextChangedEventArgs e)
		{
			var textbox = sender as TextBox;

			if (textbox == null)
				return;

			Int32 tmp32;
			if (Int32.TryParse(textbox.Text, out tmp32))
				textbox.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF595959");
			else
				textbox.BorderBrush = (Brush)FindResource("ExtryzeAccentBrush");
		}

		public class LanguageEntry
		{
			public string Language { get; set; }
			public int Index { get; set; }
			public string LanguageShort { get; set; }
		}
	}
}