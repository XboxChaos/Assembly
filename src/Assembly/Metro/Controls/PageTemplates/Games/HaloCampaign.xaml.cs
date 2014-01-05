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
					_campaign.HaloCampaign.MapNames[oldLanguage] = txtCampaignName.Text.Trim();
					_campaign.HaloCampaign.MapDescriptions[oldLanguage] = txtCampaignDesc.Text.Trim();

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
				txtCampaignName.Text = _campaign.HaloCampaign.MapNames[oldLanguage];
				txtCampaignDesc.Text = _campaign.HaloCampaign.MapDescriptions[oldLanguage];
			}
		}

		// Update Campaign file
		private void btnUpdate_Click(object sender, RoutedEventArgs e)
		{
			_campaign = new Campaign(_blfLocation);
			
			// Update Current Map Name/Descrption Language Selection
			_campaign.HaloCampaign.MapNames[cbLanguages.SelectedIndex] = txtCampaignName.Text;
			_campaign.HaloCampaign.MapDescriptions[cbLanguages.SelectedIndex] = txtCampaignDesc.Text;

			if (MapIDsError() == true)
				return;

			// Update Map IDs
			UpdateMapIDs();

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

		// Load Map IDs
		private void LoadMapIDs()
		{
			txtMap1.Text = _campaign.HaloCampaign.MapIDs[0].ToString();
			txtMap2.Text = _campaign.HaloCampaign.MapIDs[1].ToString();
			txtMap3.Text = _campaign.HaloCampaign.MapIDs[2].ToString();
			txtMap4.Text = _campaign.HaloCampaign.MapIDs[3].ToString();
			txtMap5.Text = _campaign.HaloCampaign.MapIDs[4].ToString();
			txtMap6.Text = _campaign.HaloCampaign.MapIDs[5].ToString();
			txtMap7.Text = _campaign.HaloCampaign.MapIDs[6].ToString();
			txtMap8.Text = _campaign.HaloCampaign.MapIDs[7].ToString();
			txtMap9.Text = _campaign.HaloCampaign.MapIDs[8].ToString();
			txtMap10.Text = _campaign.HaloCampaign.MapIDs[9].ToString();
			txtMap11.Text = _campaign.HaloCampaign.MapIDs[10].ToString();
			txtMap12.Text = _campaign.HaloCampaign.MapIDs[11].ToString();
			txtMap13.Text = _campaign.HaloCampaign.MapIDs[12].ToString();
			txtMap14.Text = _campaign.HaloCampaign.MapIDs[13].ToString();
			txtMap15.Text = _campaign.HaloCampaign.MapIDs[14].ToString();
			txtMap16.Text = _campaign.HaloCampaign.MapIDs[15].ToString();
			txtMap17.Text = _campaign.HaloCampaign.MapIDs[16].ToString();
			txtMap18.Text = _campaign.HaloCampaign.MapIDs[17].ToString();
			txtMap19.Text = _campaign.HaloCampaign.MapIDs[18].ToString();
			txtMap20.Text = _campaign.HaloCampaign.MapIDs[19].ToString();
			txtMap21.Text = _campaign.HaloCampaign.MapIDs[20].ToString();
			txtMap22.Text = _campaign.HaloCampaign.MapIDs[21].ToString();
			txtMap23.Text = _campaign.HaloCampaign.MapIDs[22].ToString();
			txtMap24.Text = _campaign.HaloCampaign.MapIDs[23].ToString();
			txtMap25.Text = _campaign.HaloCampaign.MapIDs[24].ToString();
			txtMap26.Text = _campaign.HaloCampaign.MapIDs[25].ToString();
			txtMap27.Text = _campaign.HaloCampaign.MapIDs[26].ToString();
			txtMap28.Text = _campaign.HaloCampaign.MapIDs[27].ToString();
			txtMap29.Text = _campaign.HaloCampaign.MapIDs[28].ToString();
			txtMap30.Text = _campaign.HaloCampaign.MapIDs[29].ToString();
			txtMap31.Text = _campaign.HaloCampaign.MapIDs[30].ToString();
			txtMap32.Text = _campaign.HaloCampaign.MapIDs[31].ToString();
			txtMap33.Text = _campaign.HaloCampaign.MapIDs[32].ToString();
			txtMap34.Text = _campaign.HaloCampaign.MapIDs[33].ToString();
			txtMap35.Text = _campaign.HaloCampaign.MapIDs[34].ToString();
			txtMap36.Text = _campaign.HaloCampaign.MapIDs[35].ToString();
			txtMap37.Text = _campaign.HaloCampaign.MapIDs[36].ToString();
			txtMap38.Text = _campaign.HaloCampaign.MapIDs[37].ToString();
			txtMap39.Text = _campaign.HaloCampaign.MapIDs[38].ToString();
			txtMap40.Text = _campaign.HaloCampaign.MapIDs[39].ToString();
			txtMap41.Text = _campaign.HaloCampaign.MapIDs[40].ToString();
			txtMap42.Text = _campaign.HaloCampaign.MapIDs[41].ToString();
			txtMap43.Text = _campaign.HaloCampaign.MapIDs[42].ToString();
			txtMap44.Text = _campaign.HaloCampaign.MapIDs[43].ToString();
			txtMap45.Text = _campaign.HaloCampaign.MapIDs[44].ToString();
			txtMap46.Text = _campaign.HaloCampaign.MapIDs[45].ToString();
			txtMap47.Text = _campaign.HaloCampaign.MapIDs[46].ToString();
			txtMap48.Text = _campaign.HaloCampaign.MapIDs[47].ToString();
			txtMap49.Text = _campaign.HaloCampaign.MapIDs[48].ToString();
			txtMap50.Text = _campaign.HaloCampaign.MapIDs[49].ToString();
			txtMap51.Text = _campaign.HaloCampaign.MapIDs[50].ToString();
			txtMap52.Text = _campaign.HaloCampaign.MapIDs[51].ToString();
			txtMap53.Text = _campaign.HaloCampaign.MapIDs[52].ToString();
			txtMap54.Text = _campaign.HaloCampaign.MapIDs[53].ToString();
			txtMap55.Text = _campaign.HaloCampaign.MapIDs[54].ToString();
			txtMap56.Text = _campaign.HaloCampaign.MapIDs[55].ToString();
			txtMap57.Text = _campaign.HaloCampaign.MapIDs[56].ToString();
			txtMap58.Text = _campaign.HaloCampaign.MapIDs[57].ToString();
			txtMap59.Text = _campaign.HaloCampaign.MapIDs[58].ToString();
			txtMap60.Text = _campaign.HaloCampaign.MapIDs[59].ToString();
			txtMap61.Text = _campaign.HaloCampaign.MapIDs[60].ToString();
			txtMap62.Text = _campaign.HaloCampaign.MapIDs[61].ToString();
			txtMap63.Text = _campaign.HaloCampaign.MapIDs[62].ToString();
			txtMap64.Text = _campaign.HaloCampaign.MapIDs[63].ToString();
		}

		// Check if any Map IDs are invalid
		private bool MapIDsError()
		{
			for (int i = 1; i <= 7; i += 2)
			{
				StackPanel sp = (StackPanel)gridMaxTeams.Children[i];
				foreach (TextBox tb in sp.Children)
				{
					if (Equals(tb.BorderBrush, FindResource("ExtryzeAccentBrush")))
					{
						Close();
						MetroMessageBox.Show("Campaign Not Saved", "The Campaign could not be saved because one or more of the Map IDs is invalid.");
						return true;
					}
				}
			}
			return false;
		}

		private void UpdateMapIDs()
		{
			_campaign.HaloCampaign.MapIDs[0] = Int32.Parse(txtMap1.Text);
			_campaign.HaloCampaign.MapIDs[1] = Int32.Parse(txtMap2.Text);
			_campaign.HaloCampaign.MapIDs[2] = Int32.Parse(txtMap3.Text);
			_campaign.HaloCampaign.MapIDs[3] = Int32.Parse(txtMap4.Text);
			_campaign.HaloCampaign.MapIDs[4] = Int32.Parse(txtMap5.Text);
			_campaign.HaloCampaign.MapIDs[5] = Int32.Parse(txtMap6.Text);
			_campaign.HaloCampaign.MapIDs[6] = Int32.Parse(txtMap7.Text);
			_campaign.HaloCampaign.MapIDs[7] = Int32.Parse(txtMap8.Text);
			_campaign.HaloCampaign.MapIDs[8] = Int32.Parse(txtMap9.Text);
			_campaign.HaloCampaign.MapIDs[9] = Int32.Parse(txtMap10.Text);
			_campaign.HaloCampaign.MapIDs[10] = Int32.Parse(txtMap11.Text);
			_campaign.HaloCampaign.MapIDs[11] = Int32.Parse(txtMap12.Text);
			_campaign.HaloCampaign.MapIDs[12] = Int32.Parse(txtMap13.Text);
			_campaign.HaloCampaign.MapIDs[13] = Int32.Parse(txtMap14.Text);
			_campaign.HaloCampaign.MapIDs[14] = Int32.Parse(txtMap15.Text);
			_campaign.HaloCampaign.MapIDs[15] = Int32.Parse(txtMap16.Text);
			_campaign.HaloCampaign.MapIDs[16] = Int32.Parse(txtMap17.Text);
			_campaign.HaloCampaign.MapIDs[17] = Int32.Parse(txtMap18.Text);
			_campaign.HaloCampaign.MapIDs[18] = Int32.Parse(txtMap19.Text);
			_campaign.HaloCampaign.MapIDs[19] = Int32.Parse(txtMap20.Text);
			_campaign.HaloCampaign.MapIDs[20] = Int32.Parse(txtMap21.Text);
			_campaign.HaloCampaign.MapIDs[21] = Int32.Parse(txtMap22.Text);
			_campaign.HaloCampaign.MapIDs[22] = Int32.Parse(txtMap23.Text);
			_campaign.HaloCampaign.MapIDs[23] = Int32.Parse(txtMap24.Text);
			_campaign.HaloCampaign.MapIDs[24] = Int32.Parse(txtMap25.Text);
			_campaign.HaloCampaign.MapIDs[25] = Int32.Parse(txtMap26.Text);
			_campaign.HaloCampaign.MapIDs[26] = Int32.Parse(txtMap27.Text);
			_campaign.HaloCampaign.MapIDs[27] = Int32.Parse(txtMap28.Text);
			_campaign.HaloCampaign.MapIDs[28] = Int32.Parse(txtMap29.Text);
			_campaign.HaloCampaign.MapIDs[29] = Int32.Parse(txtMap30.Text);
			_campaign.HaloCampaign.MapIDs[30] = Int32.Parse(txtMap31.Text);
			_campaign.HaloCampaign.MapIDs[31] = Int32.Parse(txtMap32.Text);
			_campaign.HaloCampaign.MapIDs[32] = Int32.Parse(txtMap33.Text);
			_campaign.HaloCampaign.MapIDs[33] = Int32.Parse(txtMap34.Text);
			_campaign.HaloCampaign.MapIDs[34] = Int32.Parse(txtMap35.Text);
			_campaign.HaloCampaign.MapIDs[35] = Int32.Parse(txtMap36.Text);
			_campaign.HaloCampaign.MapIDs[36] = Int32.Parse(txtMap37.Text);
			_campaign.HaloCampaign.MapIDs[37] = Int32.Parse(txtMap38.Text);
			_campaign.HaloCampaign.MapIDs[38] = Int32.Parse(txtMap39.Text);
			_campaign.HaloCampaign.MapIDs[39] = Int32.Parse(txtMap40.Text);
			_campaign.HaloCampaign.MapIDs[40] = Int32.Parse(txtMap41.Text);
			_campaign.HaloCampaign.MapIDs[41] = Int32.Parse(txtMap42.Text);
			_campaign.HaloCampaign.MapIDs[42] = Int32.Parse(txtMap43.Text);
			_campaign.HaloCampaign.MapIDs[43] = Int32.Parse(txtMap44.Text);
			_campaign.HaloCampaign.MapIDs[44] = Int32.Parse(txtMap45.Text);
			_campaign.HaloCampaign.MapIDs[45] = Int32.Parse(txtMap46.Text);
			_campaign.HaloCampaign.MapIDs[46] = Int32.Parse(txtMap47.Text);
			_campaign.HaloCampaign.MapIDs[47] = Int32.Parse(txtMap48.Text);
			_campaign.HaloCampaign.MapIDs[48] = Int32.Parse(txtMap49.Text);
			_campaign.HaloCampaign.MapIDs[49] = Int32.Parse(txtMap50.Text);
			_campaign.HaloCampaign.MapIDs[50] = Int32.Parse(txtMap51.Text);
			_campaign.HaloCampaign.MapIDs[51] = Int32.Parse(txtMap52.Text);
			_campaign.HaloCampaign.MapIDs[52] = Int32.Parse(txtMap53.Text);
			_campaign.HaloCampaign.MapIDs[53] = Int32.Parse(txtMap54.Text);
			_campaign.HaloCampaign.MapIDs[54] = Int32.Parse(txtMap55.Text);
			_campaign.HaloCampaign.MapIDs[55] = Int32.Parse(txtMap56.Text);
			_campaign.HaloCampaign.MapIDs[56] = Int32.Parse(txtMap57.Text);
			_campaign.HaloCampaign.MapIDs[57] = Int32.Parse(txtMap58.Text);
			_campaign.HaloCampaign.MapIDs[58] = Int32.Parse(txtMap59.Text);
			_campaign.HaloCampaign.MapIDs[59] = Int32.Parse(txtMap60.Text);
			_campaign.HaloCampaign.MapIDs[60] = Int32.Parse(txtMap61.Text);
			_campaign.HaloCampaign.MapIDs[61] = Int32.Parse(txtMap62.Text);
			_campaign.HaloCampaign.MapIDs[62] = Int32.Parse(txtMap63.Text);
			_campaign.HaloCampaign.MapIDs[63] = Int32.Parse(txtMap64.Text);
		}

		public class LanguageEntry
		{
			public string Language { get; set; }
			public int Index { get; set; }
			public string LanguageShort { get; set; }
		}

		// Highlight the textbox if the Map ID is invalid
		private void MapIDValidityCheck(TextBox textbox)
		{
			Int32 tmp32;
			if (Int32.TryParse(textbox.Text, out tmp32))
				textbox.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF595959");
			else
				textbox.BorderBrush = (Brush)FindResource("ExtryzeAccentBrush");
		}

		private void txtMap1_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap1);
		}

		private void txtMap2_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap2);
		}

		private void txtMap3_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap3);
		}

		private void txtMap4_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap4);
		}

		private void txtMap5_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap5);
		}

		private void txtMap6_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap6);
		}

		private void txtMap7_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap7);
		}

		private void txtMap8_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap8);
		}

		private void txtMap9_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap9);
		}

		private void txtMap10_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap10);
		}

		private void txtMap11_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap11);
		}

		private void txtMap12_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap12);
		}

		private void txtMap13_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap13);
		}

		private void txtMap14_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap14);
		}

		private void txtMap15_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap15);
		}

		private void txtMap16_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap16);
		}

		private void txtMap17_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap17);
		}

		private void txtMap18_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap18);
		}

		private void txtMap19_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap19);
		}

		private void txtMap20_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap20);
		}

		private void txtMap21_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap21);
		}

		private void txtMap22_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap22);
		}

		private void txtMap23_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap23);
		}

		private void txtMap24_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap24);
		}

		private void txtMap25_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap25);
		}

		private void txtMap26_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap26);
		}

		private void txtMap27_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap27);
		}

		private void txtMap28_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap28);
		}

		private void txtMap29_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap29);
		}

		private void txtMap30_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap30);
		}

		private void txtMap31_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap31);
		}

		private void txtMap32_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap32);
		}

		private void txtMap33_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap33);
		}

		private void txtMap34_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap34);
		}

		private void txtMap35_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap35);
		}

		private void txtMap36_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap36);
		}

		private void txtMap37_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap37);
		}

		private void txtMap38_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap38);
		}

		private void txtMap39_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap39);
		}

		private void txtMap40_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap40);
		}

		private void txtMap41_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap41);
		}

		private void txtMap42_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap42);
		}

		private void txtMap43_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap43);
		}

		private void txtMap44_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap44);
		}

		private void txtMap45_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap45);
		}

		private void txtMap46_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap46);
		}

		private void txtMap47_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap47);
		}

		private void txtMap48_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap48);
		}

		private void txtMap49_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap49);
		}

		private void txtMap50_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap50);
		}

		private void txtMap51_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap51);
		}

		private void txtMap52_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap52);
		}

		private void txtMap53_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap53);
		}

		private void txtMap54_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap54);
		}

		private void txtMap55_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap55);
		}

		private void txtMap56_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap56);
		}

		private void txtMap57_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap57);
		}

		private void txtMap58_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap58);
		}

		private void txtMap59_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap59);
		}

		private void txtMap60_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap60);
		}

		private void txtMap61_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap61);
		}

		private void txtMap62_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap62);
		}

		private void txtMap63_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap63);
		}

		private void txtMap64_TextChanged(object sender, TextChangedEventArgs e)
		{
			MapIDValidityCheck(txtMap64);
		}
	}
}