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
using Blamite.RTE.H2Vista;
using Blamite.Util;
using CloseableTabItemDemo;
using Microsoft.Win32;
using Newtonsoft.Json;
//using XBDMCommunicator;
using Blamite.Blam.ThirdGen;
using Blamite.Blam.FourthGen;
using Blamite.RTE.Eldorado;

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
        private readonly string _cacheLocation, _tagslocation, _stringslocation, _filesLocation;
        private string _tagnamesLocation;
		private readonly ObservableCollection<LanguageEntry> _languages = new ObservableCollection<LanguageEntry>();
		private readonly LayoutDocument _tab;

		private readonly Settings.TagSort _tagSorting;
		private TagHierarchy _allTags = new TagHierarchy();
		private EngineDescription _buildInfo;
		private ICacheFile _cacheFile;
		private Settings.MapInfoDockSide _dockSide;
		private ObservableCollection<HeaderValue> _headerDetails = new ObservableCollection<HeaderValue>();
		private IStreamManager _mapManager, _stringidsManager, _tagnamesManager;
		private IRTEProvider _rteProvider;
		private Trie _stringIdTrie;
		private List<TagEntry> _tagEntries = new List<TagEntry>();
		private Settings.TagOpenMode _tagOpenMode;
		private TagHierarchy _visibleTags = new TagHierarchy();

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
            _filesLocation = new FileInfo(cacheLocation).Directory.ToString() + "\\";
            _tagslocation = _filesLocation + "tags.dat";
            _stringslocation = _filesLocation + "string_ids.dat";

			// Update dockpanel location
			UpdateDockPanelLocation();

			// Show UI Pending Stuff
			doingAction.Visibility = Visibility.Visible;

			tabScripts.Visibility = Visibility.Collapsed;

			// Read Settings
			cbShowEmptyTags.IsChecked = App.AssemblyStorage.AssemblySettings.HalomapShowEmptyClasses;
			cbShowBookmarkedTagsOnly.IsChecked = App.AssemblyStorage.AssemblySettings.HalomapOnlyShowBookmarkedTags;
			cbTabOpenMode.SelectedIndex = (int) App.AssemblyStorage.AssemblySettings.HalomapTagOpenMode;
			App.AssemblyStorage.AssemblySettings.PropertyChanged += SettingsChanged;

			var initalLoadBackgroundWorker = new BackgroundWorker();
			initalLoadBackgroundWorker.DoWork += initalLoadBackgroundWorker_DoWork;
			initalLoadBackgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

            initalLoadBackgroundWorker.RunWorkerAsync();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			this.Focus();
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
            InitalizeMap(_tagslocation, _stringslocation, _filesLocation);
		}

        private static FileStream TryInitFilestream(string filepath)
        {
            try
            {
                FileStream fs = File.OpenRead(filepath);
                return fs;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public void InitalizeMap(string tagsLocation, string stringsLocation, string filesLocation)
		{
			using (FileStream mapFileStream = File.OpenRead(_cacheLocation))
            using (FileStream tagsFileStream = tagsLocation != null ? TryInitFilestream(tagsLocation) : null)
            using (FileStream stringsFileStream = stringsLocation != null ? TryInitFilestream(stringsLocation) : null)
			{
                var map_reader = new EndianReader(mapFileStream, Endian.BigEndian);
                var tags_reader = new EndianReader(tagsFileStream, Endian.BigEndian);
                var strings_reader = new EndianReader(stringsFileStream, Endian.BigEndian);

				try
				{
                    _cacheFile = CacheFileLoader.LoadCacheFile(map_reader, tags_reader, strings_reader, out _tagnamesLocation, filesLocation, App.AssemblyStorage.AssemblySettings.DefaultDatabase, out _buildInfo, tagsLocation, stringsLocation);
                    
#if DEBUG
					Dispatcher.Invoke(new Action(() => contentTabs.Items.Add(new CloseableTabItem
					{
						Header = new ContentControl
						{
							Content = "Debug Tools",
							ContextMenu = BaseContextMenu
						},
						Content = new DebugTools(_cacheFile)
					})));
#endif
				}
				catch (Exception ex)
				{
					Dispatcher.Invoke(new Action(delegate
					{
                        if (!_0xabad1dea.IWff.Heman(map_reader))
						{
							if (ex is NotSupportedException)
							{
								StatusUpdater.Update("Not a supported target engine");
								MetroMessageBox.Show("Unable to open cache file",
									ex.Message + ".\r\nWhy not add support in the 'Formats' folder?");
							}
							else
							{
								StatusUpdater.Update("An unknown error occured. Cache file may be corrupted.");
								throw ex;
							}
						}
						else
						{
							StatusUpdater.Update("HEYYEYAAEYAAAEYAEYAA");
						}

						App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(_tab);
					}));
					return;
				}

				// Build SID trie
				_stringIdTrie = new Trie();
				if (_cacheFile.StringIDs != null)
					_stringIdTrie.AddRange(_cacheFile.StringIDs);

				Dispatcher.Invoke(new Action(delegate
				{
					if (App.AssemblyStorage.AssemblySettings.StartpageHideOnLaunch)
						App.AssemblyStorage.AssemblySettings.HomeWindow.ExternalTabClose(Home.TabGenre.StartPage);
				}));

				// Set up RTE & Map Manager
				switch (_cacheFile.Engine)
				{
					case EngineType.SecondGeneration:
						_rteProvider = new H2VistaRTEProvider("halo2.exe");
                        _mapManager = new FileStreamManager(_cacheLocation, map_reader.Endianness);
                        _stringidsManager= null;
                        _tagnamesManager = null;
						break;

					case EngineType.ThirdGeneration:
						//_rteProvider = new XBDMRTEProvider(App.AssemblyStorage.AssemblySettings.Xbdm);
                        _mapManager = new FileStreamManager(_cacheLocation, map_reader.Endianness);
                        _stringidsManager= null;
                        _tagnamesManager = null;
						break;

                    case EngineType.FourthGeneration:
						switch (_cacheFile.BuildString)
						{
							default:
							case "1.106708 cert_ms23":
								_rteProvider = new EldoradoRTEProvider("eldorado.exe");
								break;
							case "11.1.498295 Live":
								_rteProvider = new ZBTRTEProvider("darkloaded.exe");
								break;
							case "12.1.700123 cert_ms30_oct19":
								_rteProvider = new ZBT70RTEProvider("darkloaded.exe");
								break;
						}
						//_rteProvider = new EldoradoRTEProvider("eldorado.exe");
                        _mapManager = new FileStreamManager(_tagslocation, map_reader.Endianness);
                        _stringidsManager = new FileStreamManager(_stringslocation, map_reader.Endianness);
                        _tagnamesManager = new FileStreamManager(_tagnamesLocation, map_reader.Endianness);
                        break;
				}

				Dispatcher.Invoke(new Action(() => StatusUpdater.Update("Loaded Cache File")));

				// Add to Recents
				Dispatcher.Invoke(new Action(delegate
				{
					RecentFiles.AddNewEntry(Path.GetFileName(_cacheLocation), _cacheLocation,
						_buildInfo.Settings.GetSetting<string>("shortName"), Settings.RecentFileType.Cache);
					StatusUpdater.Update("Added To Recents");
				}));

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
						Title = "Meta Base:",
						Data = "0x" + _cacheFile.MetaArea.BasePointer.ToString("X8")
					});
					HeaderDetails.Add(new HeaderValue {Title = "Meta Size:", Data = "0x" + _cacheFile.MetaArea.Size.ToString("X")});
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

				Dispatcher.Invoke(new Action(() => panelHeaderItems.DataContext = HeaderDetails));

				StatusUpdater.Update("Loaded Header Info");
			}));
		}

		private void LoadTags()
		{
			if (_cacheFile.TagClasses.Count == 0)
			{
				// Cache file does not support tags
				Dispatcher.Invoke(new Action(() => tabTags.Visibility = Visibility.Collapsed));
				return;
			}

			// Only allow tag importing if resource data is available
			if (_cacheFile.Resources == null)
				Dispatcher.Invoke(new Action(() => btnImport.IsEnabled = false));

			// Hide import/save name buttons if the cache file isn't thirdgen
			if (!(_cacheFile.Engine == EngineType.ThirdGeneration || _cacheFile.Engine == EngineType.FourthGeneration))
				Dispatcher.Invoke(new Action(() => panelTagButtons.Visibility = Visibility.Collapsed));

			_tagEntries = _cacheFile.Tags.Select(WrapTag).ToList();
			_allTags = BuildTagHierarchy(
				c => c.Children.Count > 0,
				t => true);

			UpdateTagFilter();
		}

		private TagHierarchy BuildTagHierarchy(Func<TagClass, bool> classFilter, Func<TagEntry, bool> tagFilter)
		{
			// Build a dictionary of tag classes
			var classWrappers = new Dictionary<ITagClass, TagClass>();
			foreach (ITagClass tagClass in _cacheFile.TagClasses)
			{
				string name = CharConstant.ToString(tagClass.Magic);

                string description = tagClass.Description.Value.ToString();
                switch(_cacheFile.Engine)
                {
                    case EngineType.FourthGeneration:
                        FourthGenCacheFile cache_file = (FourthGenCacheFile)_cacheFile;
                        description = cache_file.StringIDs.GetString(tagClass.Description);
                        break;
                    default:
                        description = _cacheFile.StringIDs.GetString(tagClass.Description) ?? "unknown";
                        break;
                }
				var wrapper = new TagClass(tagClass, name, description);
				classWrappers[tagClass] = wrapper;
			}

			// Now add tags which match the filter to their respective classes
			var result = new TagHierarchy
			{
				Entries = _tagEntries
			};

			foreach (TagEntry tag in _tagEntries.Where(t => t != null))
			{
				TagClass parentClass = classWrappers[tag.RawTag.Class];
				if (tagFilter(tag))
					parentClass.Children.Add(tag);
			}

			// Build a sorted list of classes, and then sort each tag in them
			List<TagClass> classList = classWrappers.Values.Where(classFilter).ToList();
			classList.Sort((a, b) => String.Compare(a.TagClassMagic, b.TagClassMagic, StringComparison.OrdinalIgnoreCase));
			foreach (TagClass tagClass in classList)
				tagClass.Children.Sort((a, b) => String.Compare(a.TagFileName, b.TagFileName, StringComparison.OrdinalIgnoreCase));

			// Done!
			Dispatcher.Invoke(new Action(delegate
			{
				// Give the dispatcher ownership of the ObservableCollection
				result.Classes = new ObservableCollection<TagClass>(classList);
			}));

			return result;
		}

		private void LoadLocales()
		{
            switch(_cacheFile.Engine)
            {
                case EngineType.FourthGeneration:

                    if (_cacheFile.Languages == null || !_cacheFile.Languages.AvailableLanguages.Any())
                    {
                        Dispatcher.Invoke(new Action(() => tabStrings.Visibility = Visibility.Hidden));
                        return;
                    }

                    Dispatcher.Invoke(new Action(delegate
                    {
                        lbLanguages.ItemsSource = _languages;
                        StatusUpdater.Update("Initialized Languages");
                    }));

                    break;
                default:

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

                    break;
            }

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

		private static void CloseTab(object source, RoutedEventArgs args)
		{
			var tabItem = args.OriginalSource as TabItem;
			if (tabItem == null) return;

			var tabControl = tabItem.Parent as TabControl;
			if (tabControl != null)
				tabControl.Items.Remove(tabItem);
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
			UpdateEmptyTags(cbShowEmptyTags.IsChecked ?? false);
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
			// Check it's actually a tag, and not a class the user clicked
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
					Content = new ScriptEditor(_buildInfo, script, _mapManager)
				};

				contentTabs.Items.Add(tab);
				contentTabs.SelectedItem = tab;
			}
		}

		private void DumpClassTagList(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag class
			var item = e.Source as MenuItem;
			if (item == null)
				return;

			var tagClass = item.DataContext as TagClass;
			if (tagClass == null)
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

			// Dump all of the tags that belong to the class
			using (var writer = new StreamWriter(sfd.FileName))
			{
				foreach (ITag tag in _cacheFile.Tags)
				{
					if (tag == null || tag.Class != tagClass.RawClass) continue;

                    string name;
                    switch (_cacheFile.Engine)
                    {
                        case EngineType.FourthGeneration:
                            FourthGenCacheFile cache_file = (FourthGenCacheFile)_cacheFile;
                            //name = cache_file.Test((uint)tag.Class.Magic);
                            name = "";
                            break;
                        default:
                            name = _cacheFile.FileNames.GetTagName(tag);
                            break;
                    }
					
					if (name != null)
						writer.WriteLine("{0}={1}", tag.Index, name);
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
			if (tag != null)
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

			// Passing true so raw is extracted as well
			extractTags(true, tag);
		}

		private void contextExtractClassic_Click(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag
			var item = e.Source as MenuItem;
			if (item == null)
				return;
			var tag = item.DataContext as TagEntry;
			if (tag == null)
				return;

			// Passing false so raw is not extracted
			extractTags(false, tag);
		}

		private void extractTags(bool withRaw, TagEntry tag)
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

			// Make a tag container
			var container = new TagContainer();

			// Recursively extract tags
			var tagsToProcess = new Queue<ITag>();
			var tagsProcessed = new HashSet<ITag>();
			var resourcesToProcess = new Queue<DatumIndex>();
			var resourcesProcessed = new HashSet<DatumIndex>();
			var resourcePagesProcessed = new HashSet<ResourcePage>();
			tagsToProcess.Enqueue(tag.RawTag);

			ResourceTable resources = null;
            using (var reader = _mapManager.OpenRead())
			{
				while (tagsToProcess.Count > 0)
				{
					var currentTag = tagsToProcess.Dequeue();
					if (tagsProcessed.Contains(currentTag))
						continue;

					// Get the plugin path
					var className = VariousFunctions.SterilizeTagClassName(CharConstant.ToString(currentTag.Class.Magic)).Trim();
					var pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
						_buildInfo.Settings.GetSetting<string>("plugins"), className);

					// Extract dem data blocks
					var blockBuilder = new DataBlockBuilder(reader, currentTag, _cacheFile, _buildInfo);
					using (var pluginReader = XmlReader.Create(pluginPath))
						AssemblyPluginLoader.LoadPlugin(pluginReader, blockBuilder);

					foreach (var block in blockBuilder.DataBlocks)
						container.AddDataBlock(block);

					// Add data for the tag that was extracted
					var tagName = _cacheFile.FileNames.GetTagName(currentTag) ?? currentTag.Index.ToString();
					var extractedTag = new ExtractedTag(currentTag.Index, currentTag.MetaLocation.AsPointer(), currentTag.Class.Magic,
						tagName);
					container.AddTag(extractedTag);

					// Mark the tag as processed and then enqueue all of its child tags and resources
					tagsProcessed.Add(currentTag);
					foreach (var tagRef in blockBuilder.ReferencedTags)
						tagsToProcess.Enqueue(_cacheFile.Tags[tagRef]);
					foreach (var resource in blockBuilder.ReferencedResources)
						resourcesToProcess.Enqueue(resource);
				}

				// Load the resource table in if necessary
				if (resourcesToProcess.Count > 0)
					resources = _cacheFile.Resources.LoadResourceTable(reader);
			}

			// Extract resource info
			if (resources != null)
			{
				while (resourcesToProcess.Count > 0)
				{
					var index = resourcesToProcess.Dequeue();
					if (resourcesProcessed.Contains(index))
						continue;

					// Add the resource
					var resource = resources.Resources[index.Index];
					container.AddResource(new ExtractedResourceInfo(index, resource));

					// Add data for its pages
					if (resource.Location == null) continue;

					if (resource.Location.PrimaryPage != null &&
						!resourcePagesProcessed.Contains(resource.Location.PrimaryPage))
					{
						container.AddResourcePage(resource.Location.PrimaryPage);
						resourcePagesProcessed.Add(resource.Location.PrimaryPage);

						if (withRaw)
						{
							using (var fileStream = File.OpenRead(_cacheLocation))
							{
								var resourceFile = _cacheFile;
								Stream resourceStream = fileStream;
								if (resource.Location.PrimaryPage.FilePath != null)
								{
									var resourceCacheInfo =
									App.AssemblyStorage.AssemblySettings.HalomapResourceCachePaths.FirstOrDefault(
										r => r.EngineName == _buildInfo.Name);

									var resourceCachePath = (resourceCacheInfo != null)
										? resourceCacheInfo.ResourceCachePath
										: Path.GetDirectoryName(_cacheLocation);

									resourceCachePath = Path.Combine(resourceCachePath ?? "", Path.GetFileName(resource.Location.PrimaryPage.FilePath));

									if (!File.Exists(resourceCachePath))
									{
										MetroMessageBox.Show("Unable to extract tag",
											"Unable to extract tag, because a resource it relies on is in a external cache '{0}' that could not be found. Check Assembly's settings and set the file path to resource caches.");
										return;
									}

									resourceStream =
										File.OpenRead(resourceCachePath);
									resourceFile = new ThirdGenCacheFile(new EndianReader(resourceStream, Endian.BigEndian), _buildInfo,
										_cacheFile.BuildString);
								}

								var extractor = new ResourcePageExtractor(resourceFile);
								byte[] pageData;
								using (var pageStream = new MemoryStream())
								{
									extractor.ExtractPage(resource.Location.PrimaryPage, resourceStream, pageStream);
									pageData = new byte[pageStream.Length];
									Buffer.BlockCopy(pageStream.GetBuffer(), 0, pageData, 0, (int) pageStream.Length);
								}
								container.AddExtractedResourcePage(new ExtractedPage(pageData, resource.Location.PrimaryPage.Index));
							}
						}
					}
					if (resource.Location.SecondaryPage == null || resourcePagesProcessed.Contains(resource.Location.SecondaryPage))
						continue;

					container.AddResourcePage(resource.Location.SecondaryPage);
					resourcePagesProcessed.Add(resource.Location.SecondaryPage);

					if (withRaw)
					{
						using (var fileStream = File.OpenRead(_cacheLocation))
						{
							var resourceFile = _cacheFile;
							Stream resourceStream = fileStream;
							if (resource.Location.SecondaryPage.FilePath != null)
							{
								var resourceCacheInfo =
									App.AssemblyStorage.AssemblySettings.HalomapResourceCachePaths.FirstOrDefault(
										r => r.EngineName == _buildInfo.Name);

								var resourceCachePath = (resourceCacheInfo != null)
									? resourceCacheInfo.ResourceCachePath
									: Path.GetDirectoryName(_cacheLocation);

								resourceCachePath = Path.Combine(resourceCachePath ?? "", Path.GetFileName(resource.Location.SecondaryPage.FilePath));

								if (!File.Exists(resourceCachePath))
								{
									MetroMessageBox.Show("Unable to extract tag",
										"Unable to extract tag, because a resource it relies on is in a external cache '{0}' that could not be found. Check Assembly's settings and set the file path to resource caches.");
									return;
								}

								resourceStream =
									File.OpenRead(resourceCachePath);
								resourceFile = new ThirdGenCacheFile(new EndianReader(resourceStream, Endian.BigEndian), _buildInfo,
									_cacheFile.BuildString);
							}

							var extractor = new ResourcePageExtractor(resourceFile);
							byte[] pageData;
							using (var pageStream = new MemoryStream())
							{
								extractor.ExtractPage(resource.Location.SecondaryPage, resourceStream, pageStream);
								pageData = new byte[pageStream.Length];
								Buffer.BlockCopy(pageStream.GetBuffer(), 0, pageData, 0, (int)pageStream.Length);
							}
							container.AddExtractedResourcePage(new ExtractedPage(pageData, resource.Location.SecondaryPage.Index));
						}
					}
				}
			}

			// Write it to a file
			using (var writer = new EndianWriter(File.Open(sfd.FileName, FileMode.Create, FileAccess.Write), Endian.BigEndian))
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
			using (var reader = new EndianReader(File.OpenRead(ofd.FileName), Endian.BigEndian))
				container = TagContainerReader.ReadTagContainer(reader);

			var injector = new TagContainerInjector(_cacheFile, container);
            using (IStream stream = _mapManager.OpenReadWrite())
			{
				foreach (ExtractedTag tag in container.Tags)
					injector.InjectTag(tag, stream);

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

		private void btnSaveNames_Click(object sender, RoutedEventArgs e)
		{
			// Store the names back to the cache file
			foreach (TagEntry tag in _allTags.Entries.Where(t => t != null))
				_cacheFile.FileNames.SetTagName(tag.RawTag, tag.TagFileName);

            switch(_cacheFile.Engine)
            {
                case EngineType.FourthGeneration:
                    // Save it
                    using (IStream stream = _tagnamesManager.OpenReadWrite())
                    {
                        FourthGenCacheFile cache = (FourthGenCacheFile)_cacheFile;
                        cache.SaveFileNames(stream);
                    }
                    break;
                default:
                    // Save it
                    using (IStream stream = _mapManager.OpenReadWrite())
                        _cacheFile.SaveChanges(stream);
                    break;
            }

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

			// TODO: Make this into a dialog with more options
			string newName;
			while (true)
			{
				newName = MetroInputBox.Show("Duplicate Tag", "Please enter a name for the new tag.", tag.TagFileName, "Enter a name.");
				if (newName == null)
					return;
				if (newName != tag.TagFileName && _cacheFile.Tags.FindTagByName(newName, tag.RawTag.Class, _cacheFile.FileNames) == null)
					break;
				MetroMessageBox.Show("Duplicate Tag", "Please enter a name that is different from the original and that is not in use.");
			}

			// Make a tag container for the tag and then inject it
			// TODO: A lot of this was copied and pasted from the tag extraction code...need to clean things up
			var container = new TagContainer();
            using (var stream = _mapManager.OpenReadWrite())
			{
				// Get the plugin path
				string className = VariousFunctions.SterilizeTagClassName(CharConstant.ToString(tag.RawTag.Class.Magic)).Trim();
				string pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
					_buildInfo.Settings.GetSetting<string>("plugins"), className);

				// Extract data blocks
				var builder = new DataBlockBuilder(stream, tag.RawTag, _cacheFile, _buildInfo);
				using (XmlReader pluginReader = XmlReader.Create(pluginPath))
					AssemblyPluginLoader.LoadPlugin(pluginReader, builder);
				foreach (var block in builder.DataBlocks)
				{
					// Remove non-datablock fixups because those are still valid
					// TODO: A better approach might be to just make DataBlockBuilder ignore these in the first place
					block.StringIDFixups.Clear();
					block.ShaderFixups.Clear();
					block.ResourceFixups.Clear();
					block.TagFixups.Clear();
					container.AddDataBlock(block);
				}
				var extracted = new ExtractedTag(tag.RawTag.Index, tag.RawTag.MetaLocation.AsPointer(), tag.RawTag.Class.Magic, newName);
				container.AddTag(extracted);

				// Now inject the container
				var injector = new TagContainerInjector(_cacheFile, container);
				injector.InjectTag(extracted, stream);
				injector.SaveChanges(stream);
			}

			LoadTags();
			MetroMessageBox.Show("Duplicate Tag", "Tag duplicated successfully!");
		}

		private void TagContextMenu_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var tagContext = sender as ContextMenu;

			// Check if we need to hide stuff because the cache isn't thirdgen
            if (!(_cacheFile.Engine == EngineType.ThirdGeneration || _cacheFile.Engine == EngineType.FourthGeneration))
				foreach (object tagItem in tagContext.Items)
				{
					// Check for particular names/objects to hide because datatemplate
					if (tagItem is MenuItem)
					{
						MenuItem tagMenuItem = tagItem as MenuItem;
						if (tagMenuItem.Name == "itemRename" ||
							tagMenuItem.Name == "itemDuplicate" ||
							tagMenuItem.Name == "itemExtract" ||
							tagMenuItem.Name == "itemExtractNoRaw")
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

		#region Tag List

		public static RoutedCommand ViewValueAsCommand = new RoutedCommand();
		public static RoutedCommand CommandTagBookmarking = new RoutedCommand();

		#endregion

		#region Tag Bookmarking

		private void contextBookmark_Click(object sender, RoutedEventArgs e)
		{
			// Get the menu item and the tag class
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
			App.AssemblyStorage.AssemblySettings.HalomapOnlyShowBookmarkedTags = cbShowBookmarkedTagsOnly.IsChecked ?? false;
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
					CharConstant.ToString(t.RawTag.Class.Magic),
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
				string className = CharConstant.ToString(tag.RawTag.Class.Magic);
				tag.IsBookmark = bookmarkStorage.StorageUsingTagNames
					? bookmarkStorage.BookmarkedTagNames.Any(pair => pair[0] == className && pair[1] == tag.TagFileName)
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
		/// <param name="tabTitle">THe title of the tag to search for</param>
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
					selectedTab.Header = new ContentControl
					{
						Content =
							string.Format("{0}.{1}",
								tag.TagFileName.Substring(tag.TagFileName.LastIndexOf('\\') + 1),
								CharConstant.ToString(tag.RawTag.Class.Magic)),
						ContextMenu = BaseContextMenu
					};
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
					Header = new ContentControl
					{
						Content =
							string.Format("{0}.{1}",
								tag.TagFileName.Substring(tag.TagFileName.LastIndexOf('\\') + 1),
								CharConstant.ToString(tag.RawTag.Class.Magic)),
						ContextMenu = BaseContextMenu
					},
					Tag = tag,
					Content =
						new MetaContainer(_buildInfo, _cacheLocation, tag, _allTags, _cacheFile, _mapManager, _rteProvider,
							_stringIdTrie)
				});
			}

			SelectTabFromTag(tag);
		}

		public void ExternalTabClose(TabItem tab, bool updateFocus = true)
		{
			contentTabs.Items.Remove(tab);

			if (!updateFocus) return;

			foreach (
				TabItem datTab in
					contentTabs.Items.Cast<TabItem>()
						.Where(datTab => ((ContentControl) datTab.Header).Content.ToString() == "Start Page"))
			{
				contentTabs.SelectedItem = datTab;
				return;
			}

			if (contentTabs.Items.Count > 0)
				contentTabs.SelectedIndex = contentTabs.Items.Count - 1;
		}

		public void ExternalTabsClose(List<TabItem> tab, bool updateFocus = true)
		{
			foreach (TabItem tabItem in tab)
				contentTabs.Items.Remove(tabItem);

			if (!updateFocus) return;

			foreach (
				TabItem datTab in
					contentTabs.Items.Cast<TabItem>()
						.Where(datTab => ((ContentControl) datTab.Header).Content.ToString() == "Start Page"))
			{
				contentTabs.SelectedItem = datTab;
				return;
			}

			if (contentTabs.Items.Count > 0)
				contentTabs.SelectedIndex = contentTabs.Items.Count - 1;
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
				c => FilterClass(c, filter),
				t => FilterTag(t, filter));

			Dispatcher.Invoke(new Action(delegate { tvTagList.DataContext = _visibleTags.Classes; }));
		}

		private TagEntry WrapTag(ITag tag)
		{
			if (tag == null || tag.Class == null)
				return null;

            

			string className = CharConstant.ToString(tag.Class.Magic);
            //string name = tag.Index.ToString();
            
            string name = "";

            if (_cacheFile.FileNames != null)
                name = _cacheFile.FileNames.GetTagName(tag);
            //else
                //name = _cacheFile.StringIDs.GetString((int)tag.Index.Value);

			if (string.IsNullOrWhiteSpace(name))
				name = tag.Index.ToString();

			return new TagEntry(tag, className, name);
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

		private bool FilterClass(TagClass tagClass, string filter)
		{
			bool emptyFilter = string.IsNullOrWhiteSpace(filter);
			return tagClass.Children.Count != 0 ||
			       (App.AssemblyStorage.AssemblySettings.HalomapShowEmptyClasses && emptyFilter &&
			        !App.AssemblyStorage.AssemblySettings.HalomapOnlyShowBookmarkedTags);
		}

		private bool FilterTag(TagEntry tag, string filter)
		{
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
			return tag.TagFileName.ToLower().Contains(filter) || tag.ClassName.ToLower().Contains(filter);
		}

		#endregion

		public class HeaderValue
		{
			public string Title { get; set; }
			public object Data { get; set; }
		}

	}
}