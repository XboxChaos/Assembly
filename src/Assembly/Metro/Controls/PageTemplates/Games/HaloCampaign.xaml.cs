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

					// Update UI
					_startEditing = true;
					cbLanguages.SelectedIndex = 0;
					//MessageBox.Show(_campaign.HaloCampaign.MapIDs[0].ToString());

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

		public class LanguageEntry
		{
			public string Language { get; set; }
			public int Index { get; set; }
			public string LanguageShort { get; set; }
		}
	}
}