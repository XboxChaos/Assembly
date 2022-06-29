using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components;
using Assembly.Metro.Controls.PageTemplates.Games.Components.Editors;
using Assembly.Metro.Dialogs;
using Assembly.Windows;
using Xceed.Wpf.AvalonDock.Layout;
using Blamite.Blam;
using Blamite.Blam.Localization;
using Blamite.Blam.Resources;
using Blamite.Blam.Scripting;
using Blamite.Serialization;
using Blamite.Injection;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.RTE;
using Blamite.RTE.SecondGen;
using Blamite.Util;
using CloseableTabItemDemo;
using Microsoft.Win32;
using Newtonsoft.Json;
using XBDMCommunicator;
using Blamite.Blam.SecondGen;
using Blamite.Blam.ThirdGen;
using Blamite.RTE.ThirdGen;
using Blamite.Blam.Resources.Sounds;
using System.Reflection;
using Blamite.RTE.FirstGen;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
	internal class LanguageEntry
	{
		public LanguageEntry(string name, GameLanguage lang)
		{
			Name = name;
			Language = lang;
		}

		public string Name { get; private set; }
		public GameLanguage Language { get; private set; }
	}

	/// <summary>
	///     Interaction logic for Halo4Map.xaml
	/// </summary>
	public partial class HaloMap : INotifyPropertyChanged
	{
		private readonly string _cacheLocation;
		private readonly ObservableCollection<LanguageEntry> _languages = new ObservableCollection<LanguageEntry>();
		private readonly LayoutDocument _tab;

		private readonly Settings.TagSort _tagSorting;
		private TagHierarchy _allTags = new TagHierarchy();
		private EngineDescription _buildInfo;
		private ICacheFile _cacheFile;
		private Settings.MapInfoDockSide _dockSide;
		private ObservableCollection<HeaderValue> _headerDetails = new ObservableCollection<HeaderValue>();
		private IStreamManager _mapManager;
		private IRTEProvider _rteProvider;
		private Trie _stringIdTrie;
		private List<TagEntry> _tagEntries = new List<TagEntry>();
		private Settings.TagOpenMode _tagOpenMode;
		private TagHierarchy _visibleTags = new TagHierarchy();

		public static RoutedCommand DeleteBatchCommand = new RoutedCommand();

		/// <summary>
		///     New Instance of the Halo Map Location
		/// </summary>
		/// <param name="cacheLocation"></param>
		/// <param name="tab"></param>
		/// <param name="tagSorting"> </param>
		public HaloMap(string cacheLocation, LayoutDocument tab, Settings.TagSort tagSorting)
		{
			InitializeComponent();
			AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(CloseTab));

			// Setup Context Menus
			InitalizeContextMenus();

			_tab = tab;
			_tagSorting = tagSorting;
			_cacheLocation = cacheLocation;

			// Update dockpanel location
			UpdateDockPanelLocation();

			// Show UI Pending Stuff
			doingAction.Visibility = Visibility.Visible;

			tabScripts.Visibility = Visibility.Collapsed;

			// Read Settings
			cbShowEmptyTags.IsChecked = App.AssemblyStorage.AssemblySettings.HalomapShowEmptyClasses;
			cbShowBookmarkedTagsOnly.IsChecked = App.AssemblyStorage.AssemblySettings.HalomapOnlyShowBookmarkedTags;
			cbOpenDuplicate.IsChecked = App.AssemblyStorage.AssemblySettings.AutoOpenDuplicates;
			cbTabOpenMode.SelectedIndex = (int) App.AssemblyStorage.AssemblySettings.HalomapTagOpenMode;

			App.AssemblyStorage.AssemblySettings.PropertyChanged += SettingsChanged;

			var initalLoadBackgroundWorker = new BackgroundWorker();
			initalLoadBackgroundWorker.DoWork += initalLoadBackgroundWorker_DoWork;
			initalLoadBackgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

			initalLoadBackgroundWorker.RunWorkerAsync();
		}

		public ObservableCollection<HeaderValue> HeaderDetails
		{
			get { return _headerDetails; }
			set
			{
				_headerDetails = value;
				NotifyPropertyChanged("HeaderDetails");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public bool Close()
		{
			return true;
		}

		private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			doingAction.Visibility = Visibility.Hidden;

			if (e.Error == null) return;

			// Close Tab
			App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(_tab);
			MetroException.Show(e.Error);
		}

		private void initalLoadBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			InitalizeMap();
		}

		public void InitalizeMap()
		{
			using (FileStream fileStream = File.OpenRead(_cacheLocation))
			{
				var reader = new EndianReader(fileStream, Endian.BigEndian);

				var matches = CacheFileLoader.FindEngineDescriptions(reader, App.AssemblyStorage.AssemblySettings.DefaultDatabase);

				if (matches.Count > 1)
				{
					Dispatcher.Invoke(new Action(delegate
					{
						_buildInfo = MetroEnginePicker.Show(_cacheLocation, matches);
					}));

				}
				else if (matches.Count > 0)
				{
					_buildInfo = matches[0];
				}

#if DEBUG
				_cacheFile = CacheFileLoader.LoadCacheFileWithEngineDescription(reader, _cacheLocation, _buildInfo);
#else
				try
				{
					_cacheFile = CacheFileLoader.LoadCacheFileWithEngineDescription(reader, _cacheLocation, _buildInfo);
				}
				catch (Exception ex)
				{
					Dispatcher.Invoke(new Action(delegate
					{

						reader.SeekTo(0x00);
						if (reader.ReadUInt32() != 1836017764)//check for secret sauce
						{
							if (ex is NotSupportedException)
							{
								StatusUpdater.Update("Not a supported target engine");
								MetroMessageBox.Show("Unable to open cache file",
									ex.Message + ".\r\nMake sure your Assembly is up to date, otherwise try adding support in the 'Formats' folder.");
							}
							else
							{
								StatusUpdater.Update("An unknown error occured. Cache file may be corrupted.");
								throw ex;
							}
						}
						else
						{
							if (_0xabad1dea.IWff.Play())
								StatusUpdater.Update("Opening Module File...");
							else
							{
								StatusUpdater.Update("Not a supported target engine");
								MetroMessageBox.Show("Unable to open module file",
									"Module files are not supported.");
							}
						}

						App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(_tab);
					}));
					return;
				}
#endif

				_mapManager = new FileStreamManager(_cacheLocation, reader.Endianness);

				// Build SID trie
				_stringIdTrie = new Trie();
				if (_cacheFile.StringIDs != null)
					_stringIdTrie.AddRange(_cacheFile.StringIDs);

				Dispatcher.Invoke(new Action(delegate
				{
					if (App.AssemblyStorage.AssemblySettings.StartpageHideOnLaunch)
						App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(Home.TabGenre.StartPage);
				}));

				// Set up RTE
				switch (_buildInfo.PokingPlatform)
				{
					default:
					case RTEConnectionType.None:
					case RTEConnectionType.ConsoleXbox:
						break;
					case RTEConnectionType.ConsoleXbox360:
						_rteProvider = new XBDMRTEProvider(App.AssemblyStorage.AssemblySettings.Xbdm);
						break;
					case RTEConnectionType.LocalProcess32:
					case RTEConnectionType.LocalProcess64:
						{
							if (_cacheFile.Engine == EngineType.FirstGeneration)
							{
								if (!string.IsNullOrEmpty(_buildInfo.PokingModule)) // CEA MCC
									_rteProvider = new FirstGenMCCRTEProvider(_buildInfo);
								else // PC or Custom
									_rteProvider = new FirstGenRTEProvider(_buildInfo);
							}
							else if (_cacheFile.Engine == EngineType.SecondGeneration)
							{
								if (!string.IsNullOrEmpty(_buildInfo.PokingModule))
									_rteProvider = new SecondGenMCCRTEProvider(_buildInfo);
								else
									_rteProvider = new SecondGenRTEProvider(_buildInfo);
							}
							else if (_cacheFile.Engine == EngineType.ThirdGeneration)
							{
								if (!string.IsNullOrEmpty(_buildInfo.PokingModule))
									_rteProvider = new ThirdGenMCCRTEProvider(_buildInfo);
							}
							break;
						}
				}

				Dispatcher.Invoke(new Action(() => StatusUpdater.Update("Loaded Cache File")));

				// Add to Recents
				Dispatcher.Invoke(new Action(delegate
				{
					RecentFiles.AddNewEntry(Path.GetFileName(_cacheLocation), _cacheLocation,
						_buildInfo.Settings.GetSetting<string>("shortName"), Settings.RecentFileType.Cache);
					StatusUpdater.Update("Added To Recents");
				}));

				App.AssemblyStorage.AssemblyNetworkPoke.Maps.Add(new Tuple<ICacheFile, IRTEProvider>(_cacheFile, _rteProvider));

				/*ITag dice = _cacheFile.Tags[0x0102];
				IRenderModel diceModel = _cacheFile.ResourceMetaLoader.LoadRenderModelMeta(dice, reader);
				var resourceTable = _cacheFile.Resources.LoadResourceTable(reader);
				Resource diceResource = resourceTable.Resources[diceModel.ModelResourceIndex.Index];
				ICacheFile resourceFile = _cacheFile;
				Stream resourceStream = fileStream;
				if (diceResource.Location.PrimaryPage.FilePath != null)
				{
					resourceStream = File.OpenRead(Path.Combine(Path.GetDirectoryName(_cacheLocation), Path.GetFileName(diceResource.Location.PrimaryPage.FilePath)));
					resourceFile = new ThirdGenCacheFile(new EndianReader(resourceStream, Endian.BigEndian), _buildInfo, _cacheFile.BuildString);
				}
				ResourcePageExtractor extractor = new ResourcePageExtractor(resourceFile);
				string path = Path.GetTempFileName();
				FileStream pageStream = File.Open(path, FileMode.Create, FileAccess.ReadWrite);
				extractor.ExtractPage(diceResource.Location.PrimaryPage, resourceStream, pageStream);
				if (resourceStream != fileStream)
					resourceStream.Close();
				IReader pageReader = new EndianReader(pageStream, Endian.BigEndian);
				pageReader.SeekTo(diceResource.Location.PrimaryOffset);
				ObjExporter exporter = new ObjExporter("C:\\Users\\Aaron\\Desktop\\test.obj");
				System.Collections.BitArray sections = new System.Collections.BitArray(diceModel.Sections.Length, true);
				//sections[3] = true;
				//sections[1] = true;
				ModelReader.ReadModelData(pageReader, diceModel, sections, _buildInfo, exporter);
				exporter.Close();
				pageReader.Close();*/

				LoadHeader();
				LoadTags();
				LoadLocales();
				LoadScripts();
			}
		}

		private void LoadHeader()
		{
			Dispatcher.Invoke(new Action(delegate
			{
				var fi = new FileInfo(_cacheLocation);
				_tab.Title = fi.Name;
				App.AssemblyStorage.AssemblySettings.HomeWindow.UpdateTitleText(fi.Name.Replace(fi.Extension, ""));
				lblMapName.Text = _cacheFile.InternalName;

				lblMapHeader.Text = "Map Header;";
				lblDblClick.Visibility = Visibility.Visible;
				HeaderDetails.Clear();
				HeaderDetails.Add(new HeaderValue {Title = "Game:", Data = _buildInfo.Name});
				HeaderDetails.Add(new HeaderValue
				{
					Title = "Build:",
					Data = _cacheFile.BuildString.ToString(CultureInfo.InvariantCulture)
				});
				HeaderDetails.Add(new HeaderValue {Title = "Type:", Data = _cacheFile.Type.ToString()});
				HeaderDetails.Add(new HeaderValue {Title = "Internal Name:", Data = _cacheFile.InternalName});
				HeaderDetails.Add(new HeaderValue {Title = "Scenario Name:", Data = _cacheFile.ScenarioName});
				if (_cacheFile.MetaArea != null)
				{
					HeaderDetails.Add(new HeaderValue
					{
						Title = "Tag Data Base:",
						Data = "0x" + _cacheFile.MetaArea.BasePointer.ToString("X8")
					});
					HeaderDetails.Add(new HeaderValue {Title = "Tag Data Size:", Data = "0x" + _cacheFile.MetaArea.Size.ToString("X")});
					HeaderDetails.Add(new HeaderValue
					{
						Title = "Map Magic:",
						Data = "0x" + _cacheFile.MetaArea.OffsetToPointer(0).ToString("X8")
					});
					HeaderDetails.Add(new HeaderValue
					{
						Title = "Index Header Pointer:",
						Data = "0x" + _cacheFile.IndexHeaderLocation.AsPointer().ToString("X8")
					});
				}

				if (_cacheFile.XDKVersion > 0)
					HeaderDetails.Add(new HeaderValue
					{
						Title = "SDK Version:",
						Data = _cacheFile.XDKVersion.ToString(CultureInfo.InvariantCulture)
					});

				if (_cacheFile.RawTable != null)
				{
					HeaderDetails.Add(new HeaderValue
					{
						Title = "Raw Table Offset:",
						Data = "0x" + _cacheFile.RawTable.Offset.ToString("X8")
					});
					HeaderDetails.Add(new HeaderValue {Title = "Raw Table Size:", Data = "0x" + _cacheFile.RawTable.Size.ToString("X")});
				}

				if (_cacheFile.LocaleArea != null)
					HeaderDetails.Add(new HeaderValue
					{
						Title = "Index Offset Magic:",
						Data = "0x" + ((uint) -_cacheFile.LocaleArea.PointerMask).ToString("X8")
					});

				HeaderDetails.Add(new HeaderValue
				{
					Title = "Tags:",
					Data = _cacheFile.Tags.Count
				});

				if (_cacheFile.Partitions != null && _cacheFile.Partitions.Count() > 0)
				{
					for (int i = 0; i < _cacheFile.Partitions.Count(); i++)
					{
						long pointer = 0;
						if (_cacheFile.Partitions[i].Size > 0)
							pointer = _cacheFile.Partitions[i].BasePointer.AsPointer();

						HeaderDetails.Add(new HeaderValue
						{
							Title = "Partition " + i + ":",
							Data = "0x" + pointer.ToString("X8") + " 0x" + _cacheFile.Partitions[i].Size.ToString("X8")
						
						});
					}
				}
				Dispatcher.Invoke(new Action(() => panelHeaderItems.DataContext = HeaderDetails));

				StatusUpdater.Update("Loaded Header Info");
			}));
		}

		private void LoadTags()
		{
			if (_cacheFile.TagGroups.Count == 0)
			{
				// Cache file does not support tags
				Dispatcher.Invoke(new Action(() => tabTags.Visibility = Visibility.Collapsed));
				return;
			}

			// Only allow tag importing if resource data is available
			if (_cacheFile.Resources == null)
				Dispatcher.Invoke(new Action(() => btnImport.IsEnabled = false));

			// Hide import button if the cache file isn't thirdgen
			if (_cacheFile.Engine != EngineType.ThirdGeneration || (_cacheFile.Engine == EngineType.ThirdGeneration && _cacheFile.HeaderSize == 0x800))
				Dispatcher.Invoke(new Action(() => btnImport.Visibility = Visibility.Collapsed));

			// Hide save name button if the cache file isn't secondgen or thirdgen
			if (_cacheFile.Engine < EngineType.SecondGeneration)
				Dispatcher.Invoke(new Action(() => menuSaveTagNames.Visibility = Visibility.Collapsed));

			_tagEntries = _cacheFile.Tags.Select(WrapTag).ToList();
			_allTags = BuildTagHierarchy(
				c => c.Children.Count > 0,
				t => true);

			UpdateTagFilter();
		}

		private TagHierarchy BuildTagHierarchy(Func<TagGroup, bool> groupFilter, Func<TagEntry, bool> tagFilter)
		{
			// Build a dictionary of tag groups
			var groupWrappers = new Dictionary<ITagGroup, TagGroup>();
			foreach (ITagGroup tagGroup in _cacheFile.TagGroups)
			{
				string name = CharConstant.ToString(tagGroup.Magic);
				string description;
				if (tagGroup.Description.Value == 0)
				{
					if (_buildInfo.GroupNames != null)
						description = _buildInfo.GroupNames.RetrieveName(name);
					else
						description = "unknown";
				}
				else
					description = _cacheFile.StringIDs.GetString(tagGroup.Description) ?? "unknown";

				var wrapper = new TagGroup(tagGroup, name, description);
				groupWrappers[tagGroup] = wrapper;
			}

			// Now add tags which match the filter to their respective groups
			var result = new TagHierarchy
			{
				Entries = _tagEntries
			};

			foreach (TagEntry tag in _tagEntries.Where(t => t != null))
			{
				TagGroup parentGroup = groupWrappers[tag.RawTag.Group];
				if (tagFilter(tag))
					parentGroup.Children.Add(tag);
			}

			// Build a sorted list of groups, and then sort each tag in them
			List<TagGroup> groupList = groupWrappers.Values.Where(groupFilter).ToList();
			groupList.Sort((a, b) => String.Compare(a.TagGroupMagic, b.TagGroupMagic, StringComparison.OrdinalIgnoreCase));
			foreach (TagGroup tagGroup in groupList)
				tagGroup.Children.Sort((a, b) => String.Compare(a.TagFileName, b.TagFileName, StringComparison.OrdinalIgnoreCase));

			// Done!
			Dispatcher.Invoke(new Action(delegate
			{
				// Give the dispatcher ownership of the ObservableCollection
				result.Groups = new ObservableCollection<TagGroup>(groupList);
			}));

			return result;
		}

		private void LoadLocales()
		{
			//Dispatcher.Invoke(new Action(delegate { cbLocaleLanguages.Items.Clear(); }));
			/*int totalStrings = 0;
			foreach (ILanguage language in _cache.Languages)
				totalStrings += language.StringCount;
			Dispatcher.Invoke(new Action(delegate { lblLocaleTotalCount.Text = totalStrings.ToString(); }));*/

			if (!_cacheFile.Languages.AvailableLanguages.Any())
			{
				Dispatcher.Invoke(new Action(() => tabStrings.Visibility = Visibility.Collapsed));
				return;
			}

			// TODO: Define the language names in an XML file or something
			AddLanguage("English", GameLanguage.English);
			AddLanguage("Chinese (Traditional)", GameLanguage.ChineseTrad);
			AddLanguage("Chinese (Simplified)", GameLanguage.ChineseSimp);
			AddLanguage("Danish", GameLanguage.Danish);
			AddLanguage("Dutch", GameLanguage.Dutch);
			AddLanguage("Finnish", GameLanguage.Finnish);
			AddLanguage("French", GameLanguage.French);
			AddLanguage("German", GameLanguage.German);
			AddLanguage("Italian", GameLanguage.Italian);
			AddLanguage("Japanese", GameLanguage.Japanese);
			AddLanguage("Korean", GameLanguage.Korean);
			AddLanguage("Norwegian", GameLanguage.Norwegian);
			AddLanguage("Polish", GameLanguage.Polish);
			AddLanguage("Portuguese", GameLanguage.Portuguese);
			AddLanguage("Russian", GameLanguage.Russian);
			AddLanguage("Spanish", GameLanguage.Spanish);
			AddLanguage("Spanish (Latin American)", GameLanguage.LatinAmericanSpanish);

			Dispatcher.Invoke(new Action(delegate
			{
				lbLanguages.ItemsSource = _languages;
				StatusUpdater.Update("Initialized Languages");
			}));
		}

		private void LoadScripts()
		{
			if (_buildInfo.ScriptInfo == null || _cacheFile.ScriptFiles.Length == 0)
				return;

			Dispatcher.Invoke(new Action(delegate
			{
				tabScripts.Visibility = Visibility.Visible;
				lbScripts.ItemsSource = _cacheFile.ScriptFiles;
				StatusUpdater.Update("Initialized Scripts");
			}
				));
		}

        private void HeaderValueData_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
				Clipboard.SetText(((TextBlock) e.OriginalSource).Text);
		}

		private void AddLanguage(string name, GameLanguage lang)
		{
			if (!_cacheFile.Languages.AvailableLanguages.Contains(lang))
				return;

			_languages.Add(new LanguageEntry(name, lang));
		}

		private void SettingsChanged(object sender, EventArgs e)
		{
			if (App.AssemblyStorage.AssemblySettings.HalomapTagSort != _tagSorting)
			{
				// TODO: Update the tag sorting
			}

			if (App.AssemblyStorage.AssemblySettings.HalomapMapInfoDockSide != _dockSide)
				UpdateDockPanelLocation();

			if (App.AssemblyStorage.AssemblySettings.HalomapTagOpenMode != _tagOpenMode)
				UpdateTagOpenMode();
		}

		private void cbShowEmptyTags_Altered(object sender, RoutedEventArgs e)
		{
			UpdateEmptyTags(cbShowEmptyTags.IsChecked);
		}

		private void UpdateEmptyTags(bool shown)
		{
			// Update Settings
			App.AssemblyStorage.AssemblySettings.HalomapShowEmptyClasses = shown;

			// Update TreeView
			UpdateTagFilter();

			// Fuck bitches, get money
			// #xboxscenefame
		}

		private void cbOpenDuplicate_Altered(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.AutoOpenDuplicates = cbOpenDuplicate.IsChecked;
			
		}

		private void UpdateDockPanelLocation()
		{
			_dockSide = App.AssemblyStorage.AssemblySettings.HalomapMapInfoDockSide;

			if (_dockSide == Settings.MapInfoDockSide.Left)
			{
				colLeft.Width = new GridLength(400);
				colRight.Width = new GridLength(1, GridUnitType.Star);

				sideBar.SetValue(Grid.ColumnProperty, 0);
				mainContent.SetValue(Grid.ColumnProperty, 1);
			}
			else
			{
				colRight.Width = new GridLength(400);
				colLeft.Width = new GridLength(1, GridUnitType.Star);

				sideBar.SetValue(Grid.ColumnProperty, 1);
				mainContent.SetValue(Grid.ColumnProperty, 0);
			}
		}

		private void tvTagList_SelectedTagChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			// Check it's actually a tag, and not a group the user clicked
			var selectedItem = ((TreeView) sender).SelectedItem as TagEntry;
			if (selectedItem != null)
				CreateTag(selectedItem);
		}

		private void tvTagList_ItemDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = e.Source as TreeViewItem;
			if (item == null) return;
			var entry = item.DataContext as TagEntry;
			if (entry != null)
				CreateTag(entry);
		}

		private void LocaleButtonClick(object sender, RoutedEventArgs e)
		{
			var element = (FrameworkElement) sender;
			var language = (LanguageEntry) element.DataContext;
			string tabName = language.Name + " Strings";
			if (IsTagOpen(tabName))
			{
				SelectTabFromTitle(tabName);
			}
			else
			{
				var tab = new CloseableTabItem
				{
					Header = new ContentControl
					{
						Content = tabName,
						ContextMenu = BaseContextMenu
					},
					Content = new LocaleEditor(language.Language, _cacheFile, _mapManager, _stringIdTrie, _buildInfo.LocaleSymbols)
				};

				contentTabs.Items.Add(tab);
				contentTabs.SelectedItem = tab;
			}
		}

		private void ScriptButtonClick(object sender, RoutedEventArgs e)
		{
			var elem = e.Source as FrameworkElement;
			if (elem == null) return;
			var script = elem.DataContext as IScriptFile;
			if (script == null) return;

			string tabName = script.Name;
			if (IsTagOpen(tabName))
			{
				SelectTabFromTitle(tabName);
			}
			else
			{
                var tab = new CloseableTabItem
                {
                    Header = new ContentControl
                    {
                        Content = tabName,
                        ContextMenu = BaseContextMenu
                    },
                    Content = new ScriptEditor(_buildInfo, script, _mapManager, _cacheFile, _cacheFile.Endianness)
				};

				contentTabs.Items.Add(tab);
				contentTabs.SelectedItem = tab;
			}
		}

		private void DumpGroupTagList(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag group
			var item = e.Source as MenuItem;
			if (item == null)
				return;

			var tagGroup = item.DataContext as TagGroup;
			if (tagGroup == null)
				return;

			// Ask the user where to save the dump
			var sfd = new SaveFileDialog
			{
				Title = "Save Tag List",
				Filter = "Text Files|*.txt|Tag Lists|*.taglist|All Files|*.*"
			};
			bool? result = sfd.ShowDialog();
			if (!result.Value)
				return;

			// Dump all of the tags that belong to the group
			using (var writer = new StreamWriter(sfd.FileName))
			{
				foreach (ITag tag in _cacheFile.Tags)
				{
					if (tag == null || tag.Group != tagGroup.RawGroup) continue;

					string name = _cacheFile.FileNames.GetTagName(tag);
					if (name != null)
						writer.WriteLine("{0}={1}", tag.Index, name);
				}
			}

			MetroMessageBox.Show("Dump Successful", "Tag list dumped successfully.");
		}

		private void DumpFullTagList(object sender, RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog
			{
				Title = "Save Tag List",
				Filter = "Text Files|*.txt|Tag Lists|*.taglist|All Files|*.*"
			};
			bool? result = sfd.ShowDialog();
			if (!result.Value)
				return;

			using (var writer = new StreamWriter(sfd.FileName))
			{
				foreach (ITag tag in _cacheFile.Tags)
				{
					if (tag == null || tag.Group == null) continue;

					var groupArray = BitConverter.GetBytes(tag.Group.Magic);
					Array.Reverse(groupArray);
					var groupString = System.Text.Encoding.ASCII.GetString(groupArray);

					string name = _cacheFile.FileNames.GetTagName(tag);
					if (name != null)
						writer.WriteLine("{0},{1},{2}", groupString, tag.Index, name);
				}
			}

			MetroMessageBox.Show("Dump Successful", "Tag list dumped successfully.");
		}

		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		private void ExecutedJumpToCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var tag = e.Parameter as TagEntry;
			if (tag != null && !tag.IsNull)
				CreateTag(tag);
		}

		private void contextRename_Click(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag
			var item = e.Source as MenuItem;
			if (item == null)
				return;

			var tag = item.DataContext as TagEntry;
			if (tag == null)
				return;

			// Ask for the new name
			string newName = MetroInputBox.Show("Rename Tag",
				"Please enter a new name for the tag.\r\n\t\nThis will not update the cache file until you click the \"Save Tag Names\" button at the bottom.",
				tag.TagFileName, "Enter a tag name.");
			if (newName == null || newName == tag.TagFileName)
				return;

			// Set the name
			tag.TagFileName = newName;
			tag.NotifyTooltipUpdate();

			StatusUpdater.Update("Tag Renamed");
		}

		private void contextExtract_Click(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag
			var item = e.Source as MenuItem;
			if (item == null)
				return;
			var tag = item.DataContext as TagEntry;
			if (tag == null)
				return;
			
			extractTagToFile(tag);
		}

		private void extractTagToFile(TagEntry tag)
		{
			extractTagsToFile(new List<TagEntry>() { tag });
		}

		private void extractTagsToFile(List<TagEntry> tags)
		{
			// Ask where to save the extracted tag collection
			var sfd = new SaveFileDialog
			{
				Title = "Save Tag Set",
				Filter = "Tag Container Files|*.tagc"
			};
			bool? result = sfd.ShowDialog();
			if (!result.Value)
				return;

			var container = extractTags(tags, ExtractMode.Default, true, true);
			if (container == null)
				return;

			// Write it to a file
			using (var writer = new EndianWriter(File.Open(sfd.FileName, FileMode.Create, FileAccess.Write), _cacheFile.Endianness))
				TagContainerWriter.WriteTagContainer(container, writer);

			// YAY!
			MetroMessageBox.Show("Extraction Successful",
				"Extracted " +
				container.Tags.Count + " tag(s), " +
				container.DataBlocks.Count + " data block(s), " +
				container.ResourcePages.Count + " resource page pointer(s), " +
				container.ExtractedResourcePages.Count + " extracted resource page(s), and " +
				container.Resources.Count + " resource pointer(s).");
		}

		private TagContainer extractTags(List<TagEntry> tags, ExtractMode mode, bool withResources, bool withPredictions)
		{
			// Make a tag container
			var container = new TagContainer();

			// Recursively extract tags
			var tagsToProcess = new Queue<ITag>();
			var tagsProcessed = new HashSet<ITag>();
			var resourcesToProcess = new Queue<DatumIndex>();
			var resourcesProcessed = new HashSet<DatumIndex>();
			var resourcePagesProcessed = new HashSet<ResourcePage>();

			var soundCodecsToProcess = new Queue<int>();
			var soundPitchRangesToProcess = new Queue<int>();
			var soundLanguageDurationsToProcess = new Queue<int>();
			var soundPlaybacksToProcess = new Queue<int>();
			var soundScalesToProcess = new Queue<int>();
			var soundPromotionsToProcess = new Queue<int>();
			var soundCustomPlaybacksToProcess = new Queue<int>();
			var soundExtraInfoToProcess = new Queue<int>();

			var soundCodecsProcessed = new HashSet<int>();
			var soundPitchRangesProcessed = new HashSet<int>();
			var soundLanguageDurationsProcessed = new HashSet<int>();
			var soundPlaybacksProcessed = new HashSet<int>();
			var soundScalesProcessed = new HashSet<int>();
			var soundPromotionsProcessed = new HashSet<int>();
			var soundCustomPlaybacksProcessed = new HashSet<int>();
			var soundExtraInfoProcessed = new HashSet<int>();

			foreach (TagEntry t in tags)
				tagsToProcess.Enqueue(t.RawTag);

			ResourceTable resources = null;
			SoundResourceTable soundResources = null;

			using (var reader = _mapManager.OpenRead())
			{
				while (tagsToProcess.Count > 0)
				{
					var currentTag = tagsToProcess.Dequeue();
					if (tagsProcessed.Contains(currentTag))
						continue;

					// Get the plugin path
					var groupName = VariousFunctions.SterilizeTagGroupName(CharConstant.ToString(currentTag.Group.Magic)).Trim();
					var pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
						_buildInfo.Settings.GetSetting<string>("plugins"), groupName);

					if (!File.Exists(pluginPath) && _buildInfo.Settings.PathExists("fallbackPlugins"))
						pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
							_buildInfo.Settings.GetSetting<string>("fallbackPlugins"), groupName);

					if (pluginPath == null || !File.Exists(pluginPath))
					{
						StatusUpdater.Update("Plugin doesn't exist for an extracted tag. Cannot extract.");
						return null;
					}

					// Extract dem data blocks
					var blockBuilder = new DataBlockBuilder(reader, currentTag, _cacheFile, _buildInfo);
					using (var pluginReader = XmlReader.Create(pluginPath))
						AssemblyPluginLoader.LoadPlugin(pluginReader, blockBuilder);

					foreach (var block in blockBuilder.DataBlocks)
					{
						if (mode != ExtractMode.Default)
						{
							// Remove non-datablock fixups because those are still valid
							// TODO: A better approach might be to just make DataBlockBuilder ignore these in the first place
							block.StringIDFixups.Clear();
							block.ShaderFixups.Clear();
							if (!withResources)
								block.ResourceFixups.Clear();
							block.TagFixups.Clear();
						}
						container.AddDataBlock(block);
					}

					// Add data for the tag that was extracted
					var tagName = _cacheFile.FileNames.GetTagName(currentTag) ?? currentTag.Index.ToString();

					uint cont = _cacheFile.PointerExpander.Contract(currentTag.MetaLocation.AsPointer());

					var extractedTag = new ExtractedTag(currentTag.Index, cont, currentTag.Group.Magic,
						tagName);
					container.AddTag(extractedTag);

					// Mark the tag as processed and then enqueue all of its child tags and resources
					tagsProcessed.Add(currentTag);
					if (mode != ExtractMode.Duplicate)
						foreach (var tagRef in blockBuilder.ReferencedTags)
							tagsToProcess.Enqueue(_cacheFile.Tags[tagRef]);
					foreach (var resource in blockBuilder.ReferencedResources)
						resourcesToProcess.Enqueue(resource);

					//this needs to be loaded early because there are referenced tags that need to be queued
					if (blockBuilder.ContainsSoundReferences && soundResources == null && _cacheFile.SoundGestalt != null)
						soundResources = _cacheFile.SoundGestalt.LoadSoundResourceTable(reader);

					foreach (var codec in blockBuilder.ReferencedSoundCodecs)
						soundCodecsToProcess.Enqueue(codec);
					foreach (var pr in blockBuilder.ReferencedSoundPitchRanges)
						soundPitchRangesToProcess.Enqueue(pr);
					foreach (var lpr in blockBuilder.ReferencedSoundLanguagePitchRanges)
						soundLanguageDurationsToProcess.Enqueue(lpr);
					foreach (var playback in blockBuilder.ReferencedSoundPlaybacks)
						soundPlaybacksToProcess.Enqueue(playback);
					foreach (var scale in blockBuilder.ReferencedSoundScales)
						soundScalesToProcess.Enqueue(scale);
					foreach (var pro in blockBuilder.ReferencedSoundPromotions)
						soundPromotionsToProcess.Enqueue(pro);

					foreach (var cplayback in blockBuilder.ReferencedSoundCustomPlaybacks)
					{
						soundCustomPlaybacksToProcess.Enqueue(cplayback);
						if (soundResources != null)
						{
							var cpb = soundResources.CustomPlaybacks[cplayback];
							if (cpb.RadioEffect != null)
								tagsToProcess.Enqueue(_cacheFile.Tags[cpb.RadioEffect.Index]);

							if (cpb.Components != null)
								foreach (var comp in cpb.Components)
									if (comp.Sound != null)
										tagsToProcess.Enqueue(_cacheFile.Tags[comp.Sound.Index]);
						}
					}

					foreach (var extra in blockBuilder.ReferencedSoundExtraInfo)
						soundExtraInfoToProcess.Enqueue(extra);
				}

				// Load the resource table in if necessary
				if (resourcesToProcess.Count > 0 && _cacheFile.Resources != null)
					resources = _cacheFile.Resources.LoadResourceTable(reader);
			}

			// Extract sound info
			if (soundResources != null)
			{
				while (soundCodecsToProcess.Count > 0)
				{
					var index = soundCodecsToProcess.Dequeue();
					if (soundCodecsProcessed.Contains(index))
						continue;

					if (index >= soundResources.Codecs.Count)
						throw new IndexOutOfRangeException("Cannot extract sound codec index " + index + " because it is out of range.");

					container.AddSoundCodec(new ExtractedSoundCodec(index, soundResources.Codecs[index]));
				}

				while (soundPitchRangesToProcess.Count > 0)
				{
					var index = soundPitchRangesToProcess.Dequeue();
					if (soundPitchRangesProcessed.Contains(index))
						continue;

					if (index >= soundResources.PitchRanges.Count)
						throw new IndexOutOfRangeException("Cannot extract sound pitch range index " + index + " because it is out of range.");

					var pRange = soundResources.PitchRanges[index];

					string name = _cacheFile.StringIDs.GetString(pRange.Name);
					bool hasSection = pRange.HasEncodedData;

					List<ExtractedSoundPermutation> perms = new List<ExtractedSoundPermutation>();
					for (int i = 0; i < pRange.Permutations.Length; i++)
					{
						var p = pRange.Permutations[i];

						string pName = _cacheFile.StringIDs.GetString(p.Name);
						ExtractedSoundPermutation exP = new ExtractedSoundPermutation();
						exP.Name = pName;
						exP.EncodedSkipFraction = p.EncodedSkipFraction;
						exP.EncodedGain = p.EncodedGain;
						exP.EncodedPermutationInfoIndex = p.EncodedPermutationInfoIndex;

						exP.SampleSize = p.SampleSize;

						exP.FSBInfo = p.FSBInfo;

						exP.Chunks = new List<ExtractedSoundChunk>();

						foreach (var chunk in p.Chunks)
						{
							string bankSuffix = _cacheFile.StringIDs.GetString(chunk.FModBankSuffix);
							exP.Chunks.Add(new ExtractedSoundChunk(chunk, bankSuffix));
						}

						if (p.Languages != null)
						{
							exP.Languages = new List<ExtractedSoundLanguagePermutation>();

							foreach (var lang in p.Languages)
							{
								ExtractedSoundLanguagePermutation exLP = new ExtractedSoundLanguagePermutation();

								exLP.LanguageIndex = lang.LanguageIndex;
								exLP.SampleSize = lang.SampleSize;

								exLP.Chunks = new List<ExtractedSoundChunk>();

								foreach (var chunk in p.Chunks)
								{
									string bankSuffix = _cacheFile.StringIDs.GetString(chunk.FModBankSuffix);
									exLP.Chunks.Add(new ExtractedSoundChunk(chunk, bankSuffix));
								}

								exP.Languages.Add(exLP);
							}
						}

						if (p.LayerMarkers != null)
						{
							exP.LayerMarkers = new List<int>();
							exP.LayerMarkers.AddRange(p.LayerMarkers);
						}

						perms.Add(exP);
					}

					ExtractedSoundPitchRange exPRange = new ExtractedSoundPitchRange(index, name, pRange, perms);

					container.AddSoundPitchRange(exPRange);
				}

				while (soundLanguageDurationsToProcess.Count > 0)
				{
					var index = soundLanguageDurationsToProcess.Dequeue();
					if (soundLanguageDurationsProcessed.Contains(index))
						continue;

					ExtractedSoundLanguageDuration exLD = new ExtractedSoundLanguageDuration(index);

					foreach (var lang in soundResources.LanguageDurations)
					{
						ExtractedSoundLanguageDurationInfo exLI = new ExtractedSoundLanguageDurationInfo();
						exLI.Durations = new List<int>();

						exLI.LanguageIndex = lang.LanguageIndex;

						if (index >= soundResources.PitchRanges.Count)
							throw new IndexOutOfRangeException("Cannot extract sound pitch range index " + index + " because it is out of range.");

						var pRange = lang.PitchRanges[index];

						exLI.Durations = new List<int>();
						exLI.Durations.AddRange(pRange.Durations);

						exLD.Languages.Add(exLI);
					}

					container.AddSoundLanguageDuration(exLD);
				}

				while (soundPlaybacksToProcess.Count > 0)
				{
					var index = soundPlaybacksToProcess.Dequeue();
					if (soundPlaybacksProcessed.Contains(index))
						continue;

					if (index >= soundResources.Playbacks.Count)
						throw new IndexOutOfRangeException("Cannot extract sound playback index " + index + " because it is out of range.");

					container.AddSoundPlayback(new ExtractedSoundPlayback(index, soundResources.Playbacks[index]));
				}

				while (soundScalesToProcess.Count > 0)
				{
					var index = soundScalesToProcess.Dequeue();
					if (soundScalesProcessed.Contains(index))
						continue;

					if (index >= soundResources.Scales.Count)
						throw new IndexOutOfRangeException("Cannot extract sound scale index " + index + " because it is out of range.");

					container.AddSoundScale(new ExtractedSoundScale(index, soundResources.Scales[index]));
				}

				while (soundPromotionsToProcess.Count > 0)
				{
					var index = soundPromotionsToProcess.Dequeue();
					if (soundPromotionsProcessed.Contains(index))
						continue;

					if (index >= soundResources.Promotions.Count)
						throw new IndexOutOfRangeException("Cannot extract sound promotion index " + index + " because it is out of range.");

					container.AddSoundPromotion(new ExtractedSoundPromotion(index, soundResources.Promotions[index]));
				}

				while (soundCustomPlaybacksToProcess.Count > 0)
				{
					var index = soundCustomPlaybacksToProcess.Dequeue();
					if (soundCustomPlaybacksProcessed.Contains(index))
						continue;

					if (index >= soundResources.CustomPlaybacks.Count)
						throw new IndexOutOfRangeException("Cannot extract sound custom playback index " + index + " because it is out of range.");

					container.AddSoundCustomPlayback(new ExtractedSoundCustomPlayback(index, soundResources.CustomPlaybacks[index]));
				}

				while (soundExtraInfoToProcess.Count > 0)
				{
					var index = soundExtraInfoToProcess.Dequeue();
					if (soundExtraInfoProcessed.Contains(index))
						continue;

					if (index >= soundResources.ExtraInfos.Count)
						throw new IndexOutOfRangeException("Cannot extract sound extra info index " + index + " because it is out of range.");

					container.AddSoundExtraInfo(new ExtractedSoundExtraInfo(index, soundResources.ExtraInfos[index]));

					// thar (possibly) be resources
					if (soundResources.ExtraInfos[index].Datums != null)
						foreach (var datum in soundResources.ExtraInfos[index].Datums)
							if (datum != DatumIndex.Null)
								resourcesToProcess.Enqueue(datum);
				}
			}

			// Extract resource info
			if (resources != null)
			{
				if (withResources)
				{
					while (resourcesToProcess.Count > 0)
					{
						var index = resourcesToProcess.Dequeue();
						if (resourcesProcessed.Contains(index))
							continue;

						if (index.Index >= resources.Resources.Count)
							throw new IndexOutOfRangeException("Cannot extract resource index " + index + " because it is out of range.");

						// Add the resource
						var resource = resources.Resources[index.Index];
						container.AddResource(new ExtractedResourceInfo(index, resource));

						// Add data for its pages
						if (resource.Location == null) continue;

						foreach (ResourcePage page in resource.Location.PagesToArray())
						{
							// todo maybe: break up ResourcePointer and make resource.Location into a collection of em. Could also extend that to ExtractedResourceInfo to simplify things there
							if (page != null &&
							!resourcePagesProcessed.Contains(page))
							{
								container.AddResourcePage(page);
								resourcePagesProcessed.Add(page);

								if (mode != ExtractMode.Default)
									continue;

								using (var fileStream = File.OpenRead(_cacheLocation))
								{
									var resourceFile = _cacheFile;
									Stream resourceStream = fileStream;
									if (page.FilePath != null)
									{
										var resourceCacheInfo =
										App.AssemblyStorage.AssemblySettings.HalomapResourceCachePaths.FirstOrDefault(
											r => r.EngineName == _buildInfo.Name);

										var resourceCachePath = (resourceCacheInfo != null && resourceCacheInfo.ResourceCachePath != "")
											? resourceCacheInfo.ResourceCachePath : Path.GetDirectoryName(_cacheLocation);

										resourceCachePath = Path.Combine(resourceCachePath ?? "", Path.GetFileName(page.FilePath));

										if (!File.Exists(resourceCachePath))
										{
											MetroMessageBox.Show("Unable to extract tag",
												string.Format("Unable to extract tag, because a resource it relies on is stored in an external cache, \"{0}\" which could not be found.\r\nCheck Assembly's settings and set the file path to resource caches, or verify that the missing cache is in the same folder as the open cache file.",
												Path.GetFileName(resourceCachePath)));
											return null;
										}

										resourceStream = File.OpenRead(resourceCachePath);
										resourceFile = new ThirdGenCacheFile(new EndianReader(resourceStream, _cacheFile.Endianness), _buildInfo, _cacheLocation);
									}

									var extractor = new ResourcePageExtractor(resourceFile);
									byte[] pageData;
									using (var pageStream = new MemoryStream())
									{
										extractor.ExtractPage(page, resourceStream, pageStream);
										pageData = new byte[pageStream.Length];
										Buffer.BlockCopy(pageStream.GetBuffer(), 0, pageData, 0, (int)pageStream.Length);
									}
									container.AddExtractedResourcePage(new ExtractedPage(pageData, page.Index));
								}
							}
						}
					}
				}

				if (withPredictions)
				{
					foreach (ITag t in tagsProcessed)
					{
						foreach (ResourcePredictionD pred in resources.Predictions.Where(d => d.Tag.Index == t.Index))
						{
							ExtractedResourcePredictionD expred = new ExtractedResourcePredictionD(pred);

							foreach (ResourcePredictionC pc in pred.CEntries)
							{
								ExtractedResourcePredictionC expc = new ExtractedResourcePredictionC(pc);

								foreach (ResourcePredictionA pa in pc.BEntry.AEntries)
								{
									ExtractedResourcePredictionA expa = new ExtractedResourcePredictionA(pa);

									var res = resources.Resources[pa.Resource.Index];
									if (res.ParentTag != null)
									{
										expa.OriginalResourceName = _cacheFile.FileNames.GetTagName(res.ParentTag);
										expa.OriginalResourceGroup = res.ParentTag.Group.Magic;
									}
									else
									{
										expa.OriginalResourceName = "null";
										expa.OriginalResourceGroup = -1;
									}

									expc.BEntry.AEntries.Add(expa);

								}
								expred.CEntries.Add(expc);
							}

							foreach (ResourcePredictionA pa in pred.AEntries)
							{
								ExtractedResourcePredictionA expa = new ExtractedResourcePredictionA(pa);

								var res = resources.Resources[pa.Resource.Index];
								if (res.ParentTag != null)
								{
									expa.OriginalResourceName = _cacheFile.FileNames.GetTagName(res.ParentTag);
									expa.OriginalResourceGroup = res.ParentTag.Group.Magic;
								}
								else
								{
									expa.OriginalResourceName = "null";
									expa.OriginalResourceGroup = -1;
								}

								expred.AEntries.Add(expa);
							}
							container.AddPrediction(expred);
						}
					}
				}
			}
			return container;
		}

		private void btnImport_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Open Tag Container",
				Filter = "Tag Container Files|*.tagc"
			};
			bool? result = ofd.ShowDialog();
			if (!result.Value)
				return;
			
			TagContainer container;
			using (var reader = new EndianReader(File.OpenRead(ofd.FileName), _cacheFile.Endianness))
				container = TagContainerReader.ReadTagContainer(reader);

			Dialogs.ControlDialogs.InjectSettings injs = new Dialogs.ControlDialogs.InjectSettings(_allTags, container);

			// Handle defaults
			injs.UniqueShaders = _buildInfo.OptimizedShaders;

			// H3 MCC currently doesnt store a checksum for uncompressed resources, so this must be unticked
			injs.FindRaw = _buildInfo.UsesRawHashes;

			injs.ShowDialog();

			if (injs.DialogResult.HasValue && injs.DialogResult.Value)
			{
				var injector = new TagContainerInjector(_cacheFile, container, _buildInfo, (bool)injs.KeepSounds, (bool)injs.InjectRaw, (bool)injs.FindRaw, (bool)injs.UniqueShaders);
				using (IStream stream = _mapManager.OpenReadWrite())
				{
					foreach (ExtractedTag tag in container.Tags)
						injector.InjectTag(tag, stream);

					if ((bool)injs.AddPrediction)
						injector.InjectPredictions(container.Predictions, stream);

					injector.SaveChanges(stream);
				}

				// Fix the SID trie
				foreach (StringID sid in injector.InjectedStringIDs)
					_stringIdTrie.Add(_cacheFile.StringIDs.GetString(sid));

				LoadTags();
				MetroMessageBox.Show("Import Successful",
					"Imported " +
					injector.InjectedTags.Count + " tag(s), " +
					injector.InjectedBlocks.Count + " data block(s), " +
					injector.InjectedPages.Count + " resource page pointer(s), " +
					injector.InjectedExtractedResourcePages.Count + " raw page(s), " +
					injector.InjectedResources.Count + " resource pointer(s), and " +
					injector.InjectedStringIDs.Count + " stringID(s)." +
					"\r\n\r\nPlease remember that you cannot poke to injected or modified tags without causing problems. Load the modified map in the game first.\r\n\r\nAdditionally, if applicable, make sure that your game executable is patched so that any map header hash checks are bypassed. Using an executable which only has RSA checks patched out will refuse to load the map.");

			}
		}

		private void btnSaveNames_Click(object sender, RoutedEventArgs e)
		{
			// Store the names back to the cache file
			foreach (TagEntry tag in _allTags.Entries.Where(t => t != null))
			{
				if (tag.NameExists)
					_cacheFile.FileNames.SetTagName(tag.RawTag, tag.TagFileName);
				else
					_cacheFile.FileNames.SetTagName(tag.RawTag, "");
			}

			// Save it
			using (IStream stream = _mapManager.OpenReadWrite())
				_cacheFile.SaveChanges(stream);

			MetroMessageBox.Show("Success!", "Tag names saved successfully.");
		}

		private void contextDuplicate_Click(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag
			var item = e.Source as MenuItem;
			if (item == null)
				return;
			var tag = item.DataContext as TagEntry;
			if (tag == null)
				return;

			Dialogs.ControlDialogs.DupeSettings dupe = new Dialogs.ControlDialogs.DupeSettings(_cacheFile, tag.RawTag.Group, tag.TagFileName);

			dupe.ShowDialog();

			if (dupe.DialogResult.HasValue && dupe.DialogResult.Value)
			{
				var container = extractTags(new List<TagEntry>() { tag }, ExtractMode.Duplicate, (bool)dupe.DupeAsset, (bool)dupe.DupePred);

				if (container == null)
					return;

				container.Tags.First().Name = dupe.NewName;

				using (var stream = _mapManager.OpenReadWrite())
				{
					// Now inject the container
					var injector = new TagContainerInjector(_cacheFile, container, _buildInfo, (bool)dupe.DupeSoundGestalt);
					injector.InjectTag(container.Tags.First(), stream);
					injector.InjectPredictions(container.Predictions, stream);
					injector.SaveChanges(stream);
				}

				LoadTags();
				MetroMessageBox.Show("Duplicate Tag", "Tag duplicated successfully!");

				if (App.AssemblyStorage.AssemblySettings.AutoOpenDuplicates)
				{
					ITag result = _cacheFile.Tags.FindTagByName(dupe.NewName, tag.RawTag.Group, _cacheFile.FileNames);

					foreach (TagGroup c in tvTagList.Items)
					{
						if (c.RawGroup == result.Group)
						{
							foreach (TagEntry t in c.Children)
							{
								if (t.RawTag == result)
								{
									CreateTag(t);

									return;
								}
							}
						}
					}
				}
			}
		}

		private void contextForce_Click(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag
			var item = e.Source as MenuItem;
			if (item == null)
				return;
			var tag = item.DataContext as TagEntry;
			if (tag == null)
				return;

			var container = extractTags(new List<TagEntry>() { tag }, ExtractMode.Forceload, true, false);

			if (container == null)
				return;

			// Now take the info we just extracted and use it to forceload
			using (IStream stream = _mapManager.OpenReadWrite())
			{
				var _zonesets = _cacheFile.Resources.LoadZoneSets(stream);

				foreach (ExtractedTag et in container.Tags)
				{
					//add to global and remove from the rest
					_zonesets.GlobalZoneSet.ActivateTag(et.OriginalIndex, true);

					_zonesets.UnattachedZoneSet?.ActivateTag(et.OriginalIndex, false);
					_zonesets.DiscForbiddenZoneSet?.ActivateTag(et.OriginalIndex, false);
					_zonesets.DiscAlwaysStreamingZoneSet?.ActivateTag(et.OriginalIndex, false);
					_zonesets.RequiredMapVariantsZoneSet?.ActivateTag(et.OriginalIndex, false);
					_zonesets.SandboxMapVariantsZoneSet?.ActivateTag(et.OriginalIndex, false);

					if (_zonesets.GeneralZoneSets != null)
						foreach (var set in _zonesets.GeneralZoneSets)
							set?.ActivateTag(et.OriginalIndex, false);

					if (_zonesets.BSPZoneSets != null)
						foreach (var set in _zonesets.BSPZoneSets)
							set?.ActivateTag(et.OriginalIndex, false);

					if (_zonesets.BSPZoneSets2 != null)
						foreach (var set in _zonesets.BSPZoneSets2)
							set?.ActivateTag(et.OriginalIndex, false);

					if (_zonesets.BSPZoneSets3 != null)
						foreach (var set in _zonesets.BSPZoneSets3)
							set?.ActivateTag(et.OriginalIndex, false);

					if (_zonesets.CinematicZoneSets != null)
						foreach (var set in _zonesets.CinematicZoneSets)
							set?.ActivateTag(et.OriginalIndex, false);

					if (_zonesets.ScenarioZoneSets != null)
						foreach (var set in _zonesets.ScenarioZoneSets)
							set?.ActivateTag(et.OriginalIndex, false);
				}
					

				foreach (ExtractedResourceInfo eri in container.Resources)
				{
					//add to global and remove from the rest
					_zonesets.GlobalZoneSet.ActivateResource(eri.OriginalIndex, true);

					_zonesets.UnattachedZoneSet?.ActivateResource(eri.OriginalIndex, false);
					_zonesets.DiscForbiddenZoneSet?.ActivateResource(eri.OriginalIndex, false);
					_zonesets.DiscAlwaysStreamingZoneSet?.ActivateResource(eri.OriginalIndex, false);
					_zonesets.RequiredMapVariantsZoneSet?.ActivateResource(eri.OriginalIndex, false);
					_zonesets.SandboxMapVariantsZoneSet?.ActivateResource(eri.OriginalIndex, false);

					if (_zonesets.GeneralZoneSets != null)
						foreach (var set in _zonesets.GeneralZoneSets)
							set?.ActivateResource(eri.OriginalIndex, false);

					if (_zonesets.BSPZoneSets != null)
						foreach (var set in _zonesets.BSPZoneSets)
							set?.ActivateResource(eri.OriginalIndex, false);

					if (_zonesets.BSPZoneSets2 != null)
						foreach (var set in _zonesets.BSPZoneSets2)
							set?.ActivateResource(eri.OriginalIndex, false);

					if (_zonesets.BSPZoneSets3 != null)
						foreach (var set in _zonesets.BSPZoneSets3)
							set?.ActivateResource(eri.OriginalIndex, false);

					if (_zonesets.CinematicZoneSets != null)
						foreach (var set in _zonesets.CinematicZoneSets)
							set?.ActivateResource(eri.OriginalIndex, false);

					if (_zonesets.ScenarioZoneSets != null)
						foreach (var set in _zonesets.ScenarioZoneSets)
							set?.ActivateResource(eri.OriginalIndex, false);
				}

				_zonesets.SaveChanges(stream);

				_cacheFile.SaveChanges(stream);
			}

			LoadTags();
			MetroMessageBox.Show("Forceload Successful", "Done.");
		}

		private void TagContextMenu_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var tagContext = sender as ContextMenu;

			foreach (object tagItem in tagContext.Items)
			{
				// Check for particular names/objects to hide because datatemplate
				if (tagItem is MenuItem)
				{
					MenuItem tagMenuItem = tagItem as MenuItem;

					if ((tagMenuItem.Name == "itemRename" ||
						tagMenuItem.Name == "itemIsolate") &&
						_cacheFile.Engine < EngineType.SecondGeneration)
						tagMenuItem.Visibility = Visibility.Collapsed;
					else if ((tagMenuItem.Name == "itemDuplicate" ||
						tagMenuItem.Name == "itemExtract" ||
						tagMenuItem.Name == "itemForce" ||
						tagMenuItem.Name == "itemTagBatch")
						&& (_cacheFile.Engine != EngineType.ThirdGeneration || (_cacheFile.Engine == EngineType.ThirdGeneration && _cacheFile.HeaderSize == 0x800)))
						tagMenuItem.Visibility = Visibility.Collapsed;
				}
				if (tagItem is Separator)
				{
					Separator tagMenuItem = tagItem as Separator;
					if (tagMenuItem.Name == "sepTopBookmark")
						tagMenuItem.Visibility = Visibility.Collapsed;
				}
			}
		}

		private void GroupContextMenu_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var tagContext = sender as ContextMenu;
			var group = tagContext.DataContext as TagGroup;


			foreach (object tagItem in tagContext.Items)
			{
				// Check for particular names/objects to hide because datatemplate
				if (tagItem is MenuItem)
				{
					MenuItem tagMenuItem = tagItem as MenuItem;

					// Check if we need to hide stuff because the cache isn't thirdgen
					if (_cacheFile.Engine != EngineType.ThirdGeneration || (_cacheFile.Engine == EngineType.ThirdGeneration && _cacheFile.HeaderSize == 0x800))
					{
						
						if (tagMenuItem.Name == "itemGroupBatch")
							tagMenuItem.Visibility = Visibility.Collapsed;
					}

					if(group.TagGroupMagic != "hsc*" && tagMenuItem.Name == "hscItem")
					{
						tagMenuItem.Visibility = Visibility.Collapsed;
					}
				}
			}
		}

		private void SIDButton_Click(object sender, RoutedEventArgs e)
		{
			var elem = e.Source as FrameworkElement;
			if (elem == null) return;

			string tabName = "StringID Tools";
			if (IsTagOpen(tabName))
			{
				SelectTabFromTitle(tabName);
			}
			else
			{
				var tab = new CloseableTabItem
				{
					Header = new ContentControl
					{
						Content = tabName,
						ContextMenu = BaseContextMenu
					},
					Content = new SIDTools(_cacheFile)
				};

				contentTabs.Items.Add(tab);
				contentTabs.SelectedItem = tab;
			}
		}

		private void AddrButton_Click(object sender, RoutedEventArgs e)
		{
			var elem = e.Source as FrameworkElement;
			if (elem == null) return;

			string tabName = "Address Tools";
			if (IsTagOpen(tabName))
			{
				SelectTabFromTitle(tabName);
			}
			else
			{
				var tab = new CloseableTabItem
				{
					Header = new ContentControl
					{
						Content = tabName,
						ContextMenu = BaseContextMenu
					},
					Content = new AddressTools(_cacheFile, _buildInfo)
				};

				contentTabs.Items.Add(tab);
				contentTabs.SelectedItem = tab;
			}

		}

		private void HscItem_Click(object sender, RoutedEventArgs e)
		{
			DumpHscs();
		}

		private void DumpHscs()
		{
			var tags = _cacheFile.Tags.FindTagsByGroup("hsc*");

			if (_buildInfo.Layouts.HasLayout("hsc*") && tags != null)
			{
				string folder;

				using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
				{
					System.Windows.Forms.DialogResult result = dialog.ShowDialog();
					if (result == System.Windows.Forms.DialogResult.OK)
					{
						folder = dialog.SelectedPath;
					}
					else
					{
						return;
					}
				}

				foreach (var tag in tags)
				{
					string fileName;
					byte[] data;

					using (IReader reader = _mapManager.OpenRead())
					{
						reader.SeekTo(tag.MetaLocation.AsOffset());
						var values = StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("hsc*"));

						uint pointer = (uint)values.GetInteger("source pointer");
						var exp = _cacheFile.PointerExpander.Expand(pointer);
						var offset = _cacheFile.MetaArea.PointerToOffset(exp);
						reader.SeekTo(offset);
						data = reader.ReadBlock((int)values.GetInteger("source size"));
						fileName = values.GetString("file name") + ".hsc";
					}

					string path = Path.Combine(folder, fileName);

					File.WriteAllBytes(path, data);
				}

				MetroMessageBox.Show("All source files have been extracted.");
			}
			else
			{
				MetroMessageBox.Show("This map doesn't contain hsc files.");
			}
		}

		#region Tag List

		public static RoutedCommand ViewValueAsCommand = new RoutedCommand();
		public static RoutedCommand CommandTagBookmarking = new RoutedCommand();

		#endregion

		#region Tag Bookmarking

		private void contextBookmark_Click(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag group
			var item = e.Source as MenuItem;
			if (item == null)
				return;
			var tagEntry = item.DataContext as TagEntry;
			if (tagEntry == null)
				return;

			// Toggle its bookmarked status and update the tag tree if necessary
			tagEntry.IsBookmark = !tagEntry.IsBookmark;
			if (App.AssemblyStorage.AssemblySettings.HalomapOnlyShowBookmarkedTags)
				UpdateTagFilter();
		}

		private void cbShowBookmarkedTagsOnly_Altered(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.HalomapOnlyShowBookmarkedTags = cbShowBookmarkedTagsOnly.IsChecked;
			UpdateTagFilter();
		}

		private void contextSaveBookmarks_Click(object sender, RoutedEventArgs e)
		{
			List<TagEntry> bookmarkedTags = _allTags.Entries.Where(t => t != null && t.IsBookmark).ToList();
			if (bookmarkedTags.Count == 0)
			{
				MetroMessageBox.Show("No Bookmarked Tags!",
					"If you want to save the current bookmarks, it helps if you bookmark some tags first.");
				return;
			}

			// Save these bookmarks
			KeyValuePair<string, int> keypair = MetroTagBookmarkSaver.Show();

			if (keypair.Key == null || keypair.Value == -1)
				return;

			var bookmarkStorage = new BookmarkStorageFormat
			{
				StorageUsingTagNames = (keypair.Value == 1)
			};

			if (bookmarkStorage.StorageUsingTagNames)
			{
				bookmarkStorage.BookmarkedTagNames = bookmarkedTags.Select(t => new[]
				{
					CharConstant.ToString(t.RawTag.Group.Magic),
					t.TagFileName.ToLowerInvariant()
				}).ToList();
			}
			else
			{
				bookmarkStorage.BookmarkedDatumIndices = bookmarkedTags.Select(t => t.RawTag.Index.Value).ToList();
			}

			string jsonString = JsonConvert.SerializeObject(bookmarkStorage);

			if (File.Exists(keypair.Key))
				File.Delete(keypair.Key);

			File.WriteAllText(keypair.Key, jsonString);
		}

		private void contextLoadBookmarks_Click(object sender, RoutedEventArgs e)
		{
			if (_tagEntries.Any(t => t != null && t.IsBookmark))
			{
				if (
					MetroMessageBox.Show("You already have bookmarks!",
						"If you continue, your current bookmarks will be overwritten with the bookmarks you choose. \n\nAre you sure you wish to continue?",
						MetroMessageBox.MessageBoxButtons.YesNoCancel) != MetroMessageBox.MessageBoxResult.Yes)
					return;
			}

			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a Tag Bookmark File",
				Filter = "Assembly Tag Bookmark File (*.astb)|*.astb"
			};
			if (!(bool) ofd.ShowDialog())
				return;

			var bookmarkStorage = JsonConvert.DeserializeObject<BookmarkStorageFormat>(File.ReadAllText(ofd.FileName));
			foreach (TagEntry tag in _tagEntries.Where(t => t != null))
			{
				string groupName = CharConstant.ToString(tag.RawTag.Group.Magic);
				tag.IsBookmark = bookmarkStorage.StorageUsingTagNames
					? bookmarkStorage.BookmarkedTagNames.Any(pair => pair[0] == groupName && pair[1] == tag.TagFileName)
					: bookmarkStorage.BookmarkedDatumIndices.Contains(tag.RawTag.Index.Value);
			}

			if (App.AssemblyStorage.AssemblySettings.HalomapOnlyShowBookmarkedTags)
				UpdateTagFilter();
		}

		public class BookmarkStorageFormat
		{
			public bool StorageUsingTagNames { get; set; }

			public IList<string[]> BookmarkedTagNames { get; set; }
			public IList<uint> BookmarkedDatumIndices { get; set; }
		}

		#endregion

		#region ContextMenus

		public ContextMenu BaseContextMenu;
		public ContextMenu FilesystemContextMenu;

		/// <summary>
		///     Really hacky, but i didn't want to re-do the TabControl to make it DataBinded...
		/// </summary>
		private void InitalizeContextMenus()
		{
			// Create Lame Context Menu
			BaseContextMenu = new ContextMenu();
			BaseContextMenu.FontSize = 12;
			BaseContextMenu.Items.Add(new MenuItem {Header = "Close"});
			((MenuItem) BaseContextMenu.Items[0]).Click += contextMenuClose_Click;
			BaseContextMenu.Items.Add(new MenuItem {Header = "Close All"});
			((MenuItem) BaseContextMenu.Items[1]).Click += contextMenuCloseAll_Click;
			BaseContextMenu.Items.Add(new MenuItem {Header = "Close All But This"});
			((MenuItem) BaseContextMenu.Items[2]).Click += contextMenuCloseAllButThis_Click;
			BaseContextMenu.Items.Add(new MenuItem {Header = "Close Tabs To The Left"});
			((MenuItem) BaseContextMenu.Items[3]).Click += contextMenuCloseToLeft_Click;
			BaseContextMenu.Items.Add(new MenuItem {Header = "Close Tabs To The Right"});
			((MenuItem) BaseContextMenu.Items[4]).Click += contextMenuCloseToRight_Click;

			// Create Fun Context Menu
			FilesystemContextMenu = new ContextMenu();
			FilesystemContextMenu.Items.Add(new MenuItem {Header = "Close"});
			((MenuItem) FilesystemContextMenu.Items[0]).Click += contextMenuClose_Click;
			FilesystemContextMenu.Items.Add(new MenuItem {Header = "Close All"});
			((MenuItem) FilesystemContextMenu.Items[1]).Click += contextMenuCloseAll_Click;
			FilesystemContextMenu.Items.Add(new MenuItem {Header = "Close All But This"});
			((MenuItem) FilesystemContextMenu.Items[2]).Click += contextMenuCloseAllButThis_Click;
			FilesystemContextMenu.Items.Add(new MenuItem {Header = "Close Tabs To The Left"});
			((MenuItem) FilesystemContextMenu.Items[3]).Click += contextMenuCloseToLeft_Click;
			FilesystemContextMenu.Items.Add(new MenuItem {Header = "Close Tabs To The Right"});
			((MenuItem) FilesystemContextMenu.Items[4]).Click += contextMenuCloseToRight_Click;
			FilesystemContextMenu.Items.Add(new Separator());
			FilesystemContextMenu.Items.Add(new MenuItem {Header = "Copy File Path"});
			((MenuItem) FilesystemContextMenu.Items[6]).Click += contextMenuCopyFilePath_Click;
			FilesystemContextMenu.Items.Add(new MenuItem {Header = "Open Containing Folder"});
			((MenuItem) FilesystemContextMenu.Items[7]).Click += contextMenuOpenContainingFolder_Click;
		}

		private void contextMenuClose_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl) (target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			ExternalTabClose(tabitem);
		}

		private void contextMenuCloseAll_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			List<TabItem> toDelete = contentTabs.Items.OfType<CloseableTabItem>().Cast<TabItem>().ToList();

			ExternalTabsClose(toDelete);
		}

		private void contextMenuCloseAllButThis_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl) (target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;

			List<TabItem> toDelete =
				contentTabs.Items.OfType<CloseableTabItem>().Where(tab => !Equals(tab, tabitem)).Cast<TabItem>().ToList();

			ExternalTabsClose(toDelete, false);
		}

		private void contextMenuCloseToLeft_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl) (target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			int selectedIndexOfTab = GetSelectedIndex(tabitem);

			var toDelete = new List<TabItem>();
			for (int i = 0; i < selectedIndexOfTab; i++)
				toDelete.Add((TabItem) contentTabs.Items[i]);

			ExternalTabsClose(toDelete, false);
		}

		private void contextMenuCloseToRight_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl) (target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			int selectedIndexOfTab = GetSelectedIndex(tabitem);

			var toDelete = new List<TabItem>();
			for (int i = selectedIndexOfTab + 1; i < contentTabs.Items.Count; i++)
				toDelete.Add((TabItem) contentTabs.Items[i]);

			ExternalTabsClose(toDelete, false);
		}

		private static void contextMenuCopyFilePath_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl) (target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			if (tabitem != null) Clipboard.SetText(tabitem.Tag.ToString());
		}

		private static void contextMenuOpenContainingFolder_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl) (target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			if (tabitem == null) return;

			string filepathArgument = @"/select, " + tabitem.Tag;
			Process.Start("explorer.exe", filepathArgument);
		}

		#endregion

		#region Editors

		//private void btnEditorsString_Click(object sender, RoutedEventArgs e)
		//{
		//	if (IsTagOpen("StringID Viewer"))
		//		SelectTabFromTitle("StringID Viewer");
		//	else
		//	{
		//		var tab = new CloseableTabItem
		//					  {
		//						  Header = "StringID Viewer", 
		//						  Content = new Components.Editors.StringEditor(_cacheFile)
		//					  };

		//		contentTabs.Items.Add(tab);
		//		contentTabs.SelectedItem = tab;
		//	}
		//}

		#endregion

		#region Tab Management

		/// <summary>
		///     Check to see if a tag is already open in the Editor Pane
		/// </summary>
		/// <param name="tabTitle">The title of the tag to search for</param>
		/// <returns></returns>
		private bool IsTagOpen(string tabTitle)
		{
			return
				contentTabs.Items.Cast<CloseableTabItem>()
					.Any(tab => ((ContentControl) tab.Header).Content.ToString().ToLower() == tabTitle.ToLower());
		}

		/// <summary>
		///     Check to see if a tag is already open in the Editor Pane
		/// </summary>
		/// <param name="tag">The tag to search for</param>
		private bool IsTagOpen(TagEntry tag)
		{
			return
				contentTabs.Items.Cast<CloseableTabItem>().Any(tab => tab.Tag != null && ((TagEntry) tab.Tag).RawTag == tag.RawTag);
		}

		/// <summary>
		///     Select a tab based on a Tag Title
		/// </summary>
		/// <param name="tabTitle">The tag title to search for</param>
		private void SelectTabFromTitle(string tabTitle)
		{
			CloseableTabItem tab = null;

			foreach (
				CloseableTabItem tabb in
					contentTabs.Items.Cast<CloseableTabItem>()
						.Where(tabb => ((ContentControl) tabb.Header).Content.ToString().ToLower() == tabTitle.ToLower()))
				tab = tabb;

			if (tab != null)
				contentTabs.SelectedItem = tab;
		}

		/// <summary>
		///     Select a tab based on a TagEntry
		/// </summary>
		/// <param name="tag">The tag to search for</param>
		private void SelectTabFromTag(TagEntry tag)
		{
			CloseableTabItem tab = null;
			foreach (
				CloseableTabItem tabb in
					contentTabs.Items.Cast<CloseableTabItem>()
						.Where(tabb => tabb.Tag != null && ((TagEntry) tabb.Tag).RawTag == tag.RawTag))
				tab = tabb;

			if (tab != null)
				contentTabs.SelectedItem = tab;
		}

		/// <summary>
		///     Returns a tab header
		/// </summary>
		/// <param name="tag">The tag to use</param>
		private ContentControl TabHeaderFromTag(TagEntry tag)
		{
			return new ContentControl
			{
				Content =
							string.Format("{0}.{1}",
								tag.TagFileName.Substring(tag.TagFileName.LastIndexOf('\\') + 1),
								tag.GroupName),
				ContextMenu = BaseContextMenu,
				ToolTip =
							string.Format("{0}.{1}",
								tag.TagFileName,
								tag.GroupName),
			};
		}

		public void CreateTag(TagEntry tag)
		{
			TagEntry selectedTag = null;
			TabItem selectedTab = null;

			if (_tagOpenMode == Settings.TagOpenMode.ExistingTab)
			{
				// Get current tab, make sure it is a tag 
				var currentTab = contentTabs.SelectedItem as CloseableTabItem;

				if (currentTab != null &&
				    currentTab.Tag != null &&
				    currentTab.Content is MetaContainer &&
				    currentTab.Tag is TagEntry)
				{
					// Save this
					selectedTag = (TagEntry) currentTab.Tag;
					selectedTab = currentTab;
				}
				else
				{
					foreach (
						CloseableTabItem tab in
							contentTabs.Items.Cast<CloseableTabItem>().Where(tab => tab.Tag is TagEntry && tab.Content is MetaContainer))
					{
						selectedTag = (TagEntry) tab.Tag;
						selectedTab = tab;
					}
				}

				// ReSharper disable ConditionIsAlwaysTrueOrFalse
				if (selectedTag != null && selectedTab != null)
				{
					var metaContainer = (MetaContainer) selectedTab.Content;
					metaContainer.LoadNewTagEntry(tag);
					selectedTab.Header = TabHeaderFromTag(tag);
					selectedTab.Tag = tag;
					SelectTabFromTag(tag);

					return;
				}
				// ReSharper restore ConditionIsAlwaysTrueOrFalse
			}

			if (!IsTagOpen(tag))
			{
				contentTabs.Items.Add(new CloseableTabItem
				{
					Header = TabHeaderFromTag(tag),
					Tag = tag,
					Content =
						new MetaContainer(_buildInfo, _cacheLocation, tag, _allTags, _cacheFile, _mapManager, _rteProvider,
							_stringIdTrie)
				});
			}

			SelectTabFromTag(tag);
		}

		private void CloseTab(object source, RoutedEventArgs args)
		{
			var tabItem = args.OriginalSource as TabItem;
			if (tabItem == null) return;

			ExternalTabClose(tabItem);
		}

		public void ExternalTabClose(TabItem tab, bool updateFocus = true)
		{
			DisposeTab(tab);
			contentTabs.Items.Remove(tab);

			if (!updateFocus) return;

			if (contentTabs.Items.Count > 0)
				contentTabs.SelectedIndex = contentTabs.Items.Count - 1;
		}

		public void ExternalTabsClose(List<TabItem> tab, bool updateFocus = true)
		{
			foreach (TabItem tabItem in tab)
			{
				DisposeTab(tabItem);
				contentTabs.Items.Remove(tabItem);
			}
				

			if (!updateFocus) return;

			if (contentTabs.Items.Count > 0)
				contentTabs.SelectedIndex = contentTabs.Items.Count - 1;
		}

		public void DisposeTab(TabItem tab)
		{
			if (tab.Content.GetType() == typeof(MetaContainer))
			{
				MetaContainer mc = (MetaContainer)tab.Content;
				mc.Dispose();
			}
			else if (tab.Content.GetType() == typeof(ScriptEditor))
			{
				ScriptEditor se = (ScriptEditor)tab.Content;
				se.Dispose();
			}
		}

		public int GetSelectedIndex(TabItem selectedTab)
		{
			int index = 0;
			foreach (object tab in contentTabs.Items)
			{
				if (Equals(tab, selectedTab))
					return index;

				index++;
			}

			throw new Exception();
		}

        public void RefreshTags()
        {
            foreach (CloseableTabItem tab in contentTabs.Items)
            {
                var meta = tab.Content as MetaContainer;
                if(meta != null)
                {
                    meta.RefreshMetaEditor();
                }
            }
        }

		private void UpdateTagOpenMode()
		{
			_tagOpenMode = App.AssemblyStorage.AssemblySettings.HalomapTagOpenMode;

			cbTabOpenMode.SelectedIndex = (int) _tagOpenMode;
		}

		private void cbTabOpenMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cbTabOpenMode == null || cbTabOpenMode.SelectedIndex < 0) return;

			_tagOpenMode = (Settings.TagOpenMode) cbTabOpenMode.SelectedIndex;
			App.AssemblyStorage.AssemblySettings.HalomapTagOpenMode = _tagOpenMode;
		}

		#endregion

		#region Tag Searching

		private void UpdateTagFilter()
		{
			if (_cacheFile == null)
				return;

			string filter = "";
			Dispatcher.Invoke(new Action(delegate { filter = txtTagSearch.Text.ToLower(); }));

			_visibleTags = BuildTagHierarchy(
				c => FilterGroup(c, filter),
				t => FilterTag(t, filter));

			Dispatcher.Invoke(new Action(delegate { tvTagList.DataContext = _visibleTags.Groups; }));
		}

		private TagEntry WrapTag(ITag tag)
		{
			if (tag == null || tag.Group == null || (_cacheFile.Engine != EngineType.SecondGeneration && tag.MetaLocation == null))
				return null;

			string groupName = CharConstant.ToString(tag.Group.Magic);
			string name = _cacheFile.FileNames.GetTagName(tag);

			return new TagEntry(tag, groupName, name);
		}

		private void txtTagSearch_TextChanged(object sender = null, TextChangedEventArgs e = null)
		{
			// Clear button control
			if (txtTagSearch.Text.Length > 0)
				btnResetSearch.Visibility = Visibility.Visible;
			else
				btnResetSearch.Visibility = Visibility.Hidden;

			UpdateTagFilter();
		}

		private void btnResetSearch_Click(object sender, RoutedEventArgs e)
		{
			txtTagSearch.Text = "";
			txtTagSearch.Focus();
		}

		private bool FilterGroup(TagGroup tagGroup, string filter)
		{
			bool emptyFilter = string.IsNullOrWhiteSpace(filter);
			return tagGroup.Children.Count != 0 ||
			       (App.AssemblyStorage.AssemblySettings.HalomapShowEmptyClasses && emptyFilter &&
			        !App.AssemblyStorage.AssemblySettings.HalomapOnlyShowBookmarkedTags);
		}

		private bool FilterTag(TagEntry tag, string filter)
		{
			if (tag.IsNull)
				return false;
			if (App.AssemblyStorage.AssemblySettings.HalomapOnlyShowBookmarkedTags && !tag.IsBookmark)
				return false;
			if (string.IsNullOrWhiteSpace(filter))
				return true;

			if (filter.StartsWith("0x"))
			{
				// Datum search
				string searchHex = filter.Substring(2);
				if (tag.RawTag.Index.ToString().ToLower().Substring(2).Contains(searchHex))
					return true;
			}

			// Name search
			return tag.TagFileName.ToLower().Contains(filter) || tag.GroupName.ToLower().Contains(filter);
		}

		#endregion

		#region Batch
		private void itemTagBatch_Click(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag
			var item = e.Source as MenuItem;
			if (item == null)
				return;

			var tag = item.DataContext as TagEntry;
			if (tag == null)
				return;

			// Is it already in the list?
			if (batchTagList.Items.Contains(tag))
				return;

			// Add tag to batch listbox
			batchTagList.Items.Add(tag);

		}

		private void itemGroupBatch_Click(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag group
			var item = e.Source as MenuItem;
			if (item == null)
				return;

			var tagGroup = item.DataContext as TagGroup;
			if (tagGroup == null)
				return;

			// Add everything
			foreach (TagEntry entry in tagGroup.Children)
			{
				// Is it already in the list?
				if (batchTagList.Items.Contains(entry))
					continue;

				// Add tag to batch listbox
				batchTagList.Items.Add(entry);
			}

		}

		private void BatchExtract_Click(object sender, RoutedEventArgs e)
		{
			List<TagEntry> tags = new List<TagEntry>();
			tags.AddRange(batchTagList.Items.Cast<TagEntry>());

			extractTagsToFile(tags);
		}
		private void BatchClear_Click(object sender, RoutedEventArgs e)
		{
			batchTagList.Items.Clear();
		}

		private void BatchList_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete)
			{
				List<TagEntry> selected = batchTagList.SelectedItems.Cast<TagEntry>().ToList();

				foreach (TagEntry t in selected)
				{
					if (batchTagList.Items.Contains(t))
						batchTagList.Items.Remove(t);
				}

			}
		}
		#endregion

		private void itemIsolate_Click(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag
			var item = e.Source as MenuItem;
			if (item == null)
				return;

			var tag = item.DataContext as TagEntry;
			if (tag == null)
				return;

			if (tag.RawTag != null && tag.RawTag.MetaLocation == null)
				return;

			if (MetroMessageBox.Show("Tag Isolation - Assembly",
				"This will run the equivalent of Isolating a Tag Block or Data Reference, but across the tag's base data.\r\n" +
				"You should only use this in cases where multiple tags are pointing to the same data, and when you know what you are doing.\r\n" + 
				"This process will also close the tag if it is open, so save any changes to it before continuing.\r\n" + 
				"Don't forget to back up your map, too!",
				MetroMessageBox.MessageBoxButtons.OkCancel) != MetroMessageBox.MessageBoxResult.OK)
				return;

			//close the tag if its currently open in a tab
			TabItem tabb = contentTabs.Items.Cast<TabItem>()
				.FirstOrDefault(ct => ct.Tag != null && ((TagEntry)ct.Tag).RawTag == tag.RawTag);
			if (tabb != null)
				ExternalTabClose(tabb, false);

			//get the plugin to obtain the size of the base data
			var groupName = VariousFunctions.SterilizeTagGroupName(CharConstant.ToString(tag.RawTag.Group.Magic)).Trim();
			var pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
				_buildInfo.Settings.GetSetting<string>("plugins"), groupName);

			if (!File.Exists(pluginPath) && _buildInfo.Settings.PathExists("fallbackPlugins"))
				pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
					_buildInfo.Settings.GetSetting<string>("fallbackPlugins"), groupName);

			if (pluginPath == null || !File.Exists(pluginPath))
			{
				StatusUpdater.Update("Plugin doesn't exist for the selected tag. Cannot isolate.");
				return;
			}

			int baseSize = 0;
			using (var pluginReader = XmlReader.Create(pluginPath))
			{
				//redundant stuff from AssemblyPluginLoader so I don't have to create an instance
				if (!pluginReader.ReadToNextSibling("plugin"))
					throw new ArgumentException("The XML file is missing a <plugin> tag.");

				if (pluginReader.MoveToAttribute("baseSize"))
				{
					string str = pluginReader.Value;

					if (str.StartsWith("0x"))
						baseSize = int.Parse(str.Substring(2), NumberStyles.HexNumber);
					else
						baseSize = int.Parse(str);
				}
			}

			//allocate the new data and copy it over
			using (var stream = _mapManager.OpenReadWrite())
			{
				long newAddr = _cacheFile.Allocator.Allocate(baseSize, stream);

				//backup original locations
				long origAddress = tag.RawTag.MetaLocation.AsPointer();
				long origOffset = tag.RawTag.MetaLocation.AsOffset();
				long origAddressMax = origAddress + baseSize;
				uint contracted = _cacheFile.PointerExpander.Contract(origAddress);
				uint contractedMax = _cacheFile.PointerExpander.Contract(origAddressMax);

				//interops can exist within the base data, so any references should to be copied as well
				if (_cacheFile.TagInteropTable != null)
				{
					List<ITagInterop> newInterops = new List<ITagInterop>();

					List<ITagInterop> existingInterops = _cacheFile.TagInteropTable.Where(t => t.Pointer > contracted && t.Pointer < contractedMax).ToList();

					foreach (ITagInterop interop in existingInterops)
					{
						long interopAddr = _cacheFile.PointerExpander.Expand(interop.Pointer);
						long offsetIntoTag = interopAddr - origAddress;

						uint newInteropAddr = _cacheFile.PointerExpander.Contract(newAddr + offsetIntoTag);

						newInterops.Add(new Blamite.Blam.ThirdGen.Structures.ThirdGenTagInterop(newInteropAddr, interop.Type));
					}

					foreach (ITagInterop interop in newInterops)
						_cacheFile.TagInteropTable.Add(interop);
				}

				tag.RawTag.MetaLocation = SegmentPointer.FromPointer(newAddr, _cacheFile.MetaArea);

				_cacheFile.SaveChanges(stream);

				long newOffset = _cacheFile.MetaArea.PointerToOffset(newAddr);

				stream.SeekTo(origOffset);
				byte[] data = stream.ReadBlock(baseSize);
				stream.SeekTo(newOffset);
				stream.WriteBlock(data);

				tag.NotifyTooltipUpdate();

				MetroMessageBox.Show("Tag Isolation - Assembly",
					"The tag was isolated successfully.");
			}

		}

		public class HeaderValue
		{
			public string Title { get; set; }
			public object Data { get; set; }
		}

		public enum ExtractMode
		{
			Default = 0,
			Duplicate = 1,
			Forceload = 2,
		}

		public void Dispose()
		{
			App.AssemblyStorage.AssemblySettings.PropertyChanged -= SettingsChanged;

			List<TabItem> tabs = contentTabs.Items.OfType<TabItem>().ToList();

			App.AssemblyStorage.AssemblyNetworkPoke.Maps.Remove(new Tuple<ICacheFile, IRTEProvider>(_cacheFile, _rteProvider));

			ExternalTabsClose(tabs, false);

			_stringIdTrie = null;

			_tagEntries.Clear();
			_allTags.Entries.Clear();
			_visibleTags.Entries.Clear();

			//check for any viewvalueas dialogs that rely on this cache and close them
			foreach (Window w in Application.Current.Windows)
			{
				if (w.GetType() == typeof(Dialogs.ControlDialogs.ViewValueAs))
				{
					if (((Dialogs.ControlDialogs.ViewValueAs)w).ParentCache == _cacheFile)
						w.Close();
				}
			}
		}

		private void MenuSaveTagEditors_Click(object sender, RoutedEventArgs e)
		{
			List<TabItem> tabs = contentTabs.Items.OfType<TabItem>().ToList();

			foreach (TabItem tabItem in tabs)
			{
				if (tabItem.Content.GetType() == typeof(MetaContainer))
					((MetaContainer)tabItem.Content).ExternalSave();
			}

			MetroMessageBox.Show("Tags Saved", "The changes have been saved back to the original file.");
		}

		private void SIDFreeButton_Click(object sender, RoutedEventArgs e)
		{
			string bspLayoutName = "sbsp";
			string bspInstancedLayoutName = "sbsp instanced geometry";

			string bspLayoutBlockCountName = "number of instanced geometry";
			string bspLayoutBlockAddrName = "instanced geometry table address";

			string bspInstancedLayoutStringIDName = "name stringid";

			if (!_buildInfo.Layouts.HasLayout(bspLayoutName) || !_buildInfo.Layouts.HasLayout(bspInstancedLayoutName))
			{
				MetroMessageBox.Show("Free StringIDs", "This current engine does not contain the required \"" + bspLayoutName + "\" and \"" + bspInstancedLayoutName + "\" layouts in the Formats folder. Can not continue.");
				return;
			}

			StructureLayout sbspLayout = _buildInfo.Layouts.GetLayout(bspLayoutName);
			StructureLayout sbspInstancedLayout = _buildInfo.Layouts.GetLayout(bspInstancedLayoutName);

			if (!sbspLayout.HasField(bspLayoutBlockCountName) || !sbspLayout.HasField(bspLayoutBlockAddrName))
			{
				MetroMessageBox.Show("Free StringIDs", "This current engine does not contain the required \"" + bspLayoutBlockCountName + "\" and \"" + bspLayoutBlockAddrName + "\" fields for the \"" + bspLayoutName + "\" layout in the Formats folder. Can not continue.");
				return;
			}
			else if (!sbspLayout.HasField(bspLayoutBlockCountName))
			{
				MetroMessageBox.Show("Free StringIDs", "This current engine does not contain the required \"" + bspInstancedLayoutStringIDName + "\" field for the \"" + sbspInstancedLayout + "\" layout in the Formats folder. Can not continue.");
				return;
			}

			int origLength = _cacheFile.StringIDDataTable.Size;

			using (var stream = _mapManager.OpenReadWrite())
			{
				foreach (var tag in _cacheFile.Tags.FindTagsByGroup("sbsp"))
				{
					stream.SeekTo(tag.MetaLocation.AsOffset());

					StructureValueCollection values = StructureReader.ReadStructure(stream, sbspLayout);

					int count = (int)values.GetInteger(bspLayoutBlockCountName);
					uint contractedAddr = (uint)values.GetInteger(bspLayoutBlockAddrName);
					long expandedAddr = _cacheFile.PointerExpander.Expand(contractedAddr);

					StructureValueCollection[] instancedGeo = Blamite.Blam.Util.TagBlockReader.ReadTagBlock(stream, count, expandedAddr, sbspInstancedLayout, _cacheFile.MetaArea);

					foreach (var iGeo in instancedGeo)
					{
						uint rawVal = (uint)iGeo.GetInteger(bspInstancedLayoutStringIDName);
						StringID sid = new StringID(rawVal);

						string test = _cacheFile.StringIDs.GetString(sid);

						if (test.Length <= 10)
							continue;

						_cacheFile.StringIDs.SetString(sid, "ig" + rawVal.ToString("X"));
					}
				}

				_cacheFile.SaveChanges(stream);
			}

			int result = origLength - _cacheFile.StringIDDataTable.Size;

			MetroMessageBox.Show("Free StringIDs", "A total of " + result + " bytes were freed.");
				
		}
	}
}