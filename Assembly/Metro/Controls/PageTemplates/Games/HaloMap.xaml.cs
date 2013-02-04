using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.IO;
using System.Xml.Linq;
using System.Collections.ObjectModel;

using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.IO;
using Assembly.Metro.Dialogs;
using ExtryzeDLL.Util;
using CloseableTabItemDemo;
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components;
using Assembly.Windows;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Flexibility;
using Microsoft.Win32;
using ExtryzeDLL.Blam.SecondGen;
using ExtryzeDLL.Blam.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
    class LanguageEntry
    {
        public LanguageEntry(string name, int index, ILanguage baseEntry)
        {
            Name = name;
            Base = baseEntry;
            Index = index;
        }

        public string Name { get; private set; }
        public int Index { get; private set; }
        public ILanguage Base { get; private set; }
    }

    /// <summary>
    /// Interaction logic for Halo4Map.xaml
    /// </summary>
    public partial class HaloMap : UserControl
    {
        private IStreamManager _mapManager;
        private CacheFileVersionInfo _version;
        private XDocument _supportedBuilds;
        private BuildInfoLoader _layoutLoader;
        private BuildInformation _buildInfo;
        private ICacheFile _cacheFile;
        private string _cacheLocation;
        private TabItem _tab;
        private List<EmptyClassesObject> _emptyClasses = new List<EmptyClassesObject>();
        private class EmptyClassesObject
        {
            public int Index { get; set; }
            public string ClassName { get; set; }
            public TagClass TagClass { get; set; }
        }

        private Settings.TagSort _tagSorting;
        private Settings.MapInfoDockSide _dockSide;
        private TagHierarchy _hierarchy = new TagHierarchy();
        private ObservableCollection<TagClass> _tagsComplete;
        private ObservableCollection<TagClass> _tagsPopulated = new ObservableCollection<TagClass>();
        private ObservableCollection<LanguageEntry> _languages = new ObservableCollection<LanguageEntry>();

        #region Public Access
        public TagHierarchy TagHierarchy { get { return _hierarchy; } set { _hierarchy = value; } }
        public ICacheFile CacheFile { get { return _cacheFile; } set { _cacheFile = value; } }
        #endregion

        /// <summary>
        /// New Instance of the Halo Map Location
        /// </summary>
        /// <param name="cacheLocation"></param>
        public HaloMap(string cacheLocation, TabItem tab)
        {
            InitializeComponent();
            AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(CloseTab));

			// Setup Context Menus
			InitalizeContextMenus();

            _tab = tab;
            _cacheLocation = cacheLocation;

            // Update dockpanel location
            UpdateDockPanelLocation();

            // Show UI Pending Stuff
            doingAction.Visibility = Visibility.Visible;

            tabScripts.Visibility = Visibility.Collapsed;

            // Read Settings
            cbShowEmptyTags.IsChecked = Settings.halomapShowEmptyClasses;
            Settings.SettingsChanged += SettingsChanged;

            var initalLoadBackgroundWorker = new BackgroundWorker();
            initalLoadBackgroundWorker.DoWork += initalLoadBackgroundWorker_DoWork;
            initalLoadBackgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

            initalLoadBackgroundWorker.RunWorkerAsync();
        }

        public bool Close()
        {
            return true;
        }

        void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            doingAction.Visibility = Visibility.Hidden;
            if (e.Error == null) return;

            // Close Tab
            Settings.homeWindow.ExternalTabClose(_tab);
            MetroException.Show(e.Error);
        }

        void initalLoadBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            InitalizeMap();
        }

        private static Endian GetEndianness(Stream stream)
        {
            // Read the magic value
            // 'head' = big-endian
            // 'daeh' = little-endian
            var buffer = new byte[4];
            stream.Read(buffer, 0, 4);

            if (buffer[0] == 'h' && buffer[1] == 'e' && buffer[2] == 'a' && buffer[3] == 'd')
                return Endian.BigEndian;
            return Endian.LittleEndian;
        }
        
        public void InitalizeMap()
        {
            using (var fileStream = File.OpenRead(_cacheLocation))
            {
	            var endianness = GetEndianness(fileStream);
				_mapManager = new FileStreamManager(_cacheLocation, endianness);
                var reader = new EndianReader(fileStream, endianness);
                Dispatcher.Invoke(new Action(() => StatusUpdater.Update("Opened File")));

                _version = new CacheFileVersionInfo(reader);
                _supportedBuilds = XDocument.Load(VariousFunctions.GetApplicationLocation() + @"Formats\SupportedBuilds.xml");
                _layoutLoader = new BuildInfoLoader(_supportedBuilds, VariousFunctions.GetApplicationLocation() + @"Formats\");
                _buildInfo = _layoutLoader.LoadBuild(_version.BuildString);

                Dispatcher.Invoke(new Action(() => StatusUpdater.Update("Loaded Build Definitions")));

                if (_buildInfo == null)
                {
                    Dispatcher.Invoke(new Action(delegate
                        {
                            if (!_0xabad1dea.IWff.Heman(reader))
                            {
                                StatusUpdater.Update("Not a supported cache build");
                                MetroMessageBox.Show("Unable to open cache file", "Unsupported blam engine build \"" + _version.BuildString + "\". Why not add support in the 'Formats' folder?");
                            }
                            else
                            {
                                StatusUpdater.Update("HEYYEYAAEYAAAEYAEYAA");
                            }

                            Settings.homeWindow.ExternalTabClose((TabItem)Parent);
                        }));
                    return;
                }
                Dispatcher.Invoke(new Action(delegate
                {
                    if (Settings.startpageHideOnLaunch)
                        Settings.homeWindow.ExternalTabClose(Home.TabGenre.StartPage);
                }));

                // Load the cache file
                switch (_version.Engine)
                {
                    case EngineType.SecondGeneration:
                        _cacheFile = new SecondGenCacheFile(reader, _buildInfo, _version.BuildString);
                        break;

                    case EngineType.ThirdGeneration:
                        _cacheFile = new ThirdGenCacheFile(reader, _buildInfo, _version.BuildString);
                        break;
                }
                Dispatcher.Invoke(new Action(() => StatusUpdater.Update("Loaded Cache File")));

                // Add to Recents
                Dispatcher.Invoke(new Action(delegate
                {
                    RecentFiles.AddNewEntry(Path.GetFileName(_cacheLocation), _cacheLocation, _buildInfo.ShortName, Settings.RecentFileType.Cache);
                    StatusUpdater.Update("Added To Recents");
                }));

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
                _tab.Header = new ContentControl
                                  {
                                      Content = fi.Name,
                                      ContextMenu = Settings.homeWindow.FilesystemContextMenu
                                  };
                Settings.homeWindow.UpdateTitleText(fi.Name.Replace(fi.Extension, ""));
                lblMapName.Text = _cacheFile.Info.InternalName;

                lblMapHeader.Text = "Map Header;";
                listMapHeader.Children.Clear();
                listMapHeader.Children.Add(new MapHeaderEntry("Game:", _buildInfo.GameName));
                listMapHeader.Children.Add(new MapHeaderEntry("Build:", _cacheFile.Info.BuildString.ToString(CultureInfo.InvariantCulture)));
                listMapHeader.Children.Add(new MapHeaderEntry("Type:", _cacheFile.Info.Type.ToString()));
                listMapHeader.Children.Add(new MapHeaderEntry("Internal Name:", _cacheFile.Info.InternalName));
                listMapHeader.Children.Add(new MapHeaderEntry("Scenario Name:", _cacheFile.Info.ScenarioName));
                listMapHeader.Children.Add(new MapHeaderEntry("Virtual Base:", "0x" + _cacheFile.Info.VirtualBaseAddress.ToString("X8")));
                listMapHeader.Children.Add(new MapHeaderEntry("Virtual Size:", "0x" + _cacheFile.Info.MetaSize.ToString("X")));
                listMapHeader.Children.Add(new MapHeaderEntry("SDK Version:", _cacheFile.Info.XDKVersion.ToString(CultureInfo.InvariantCulture)));
                listMapHeader.Children.Add(new MapHeaderEntry("Raw Table Offset:", "0x" + _cacheFile.Info.RawTableOffset.ToString("X8")));
                listMapHeader.Children.Add(new MapHeaderEntry("Raw Table Size:", "0x" + _cacheFile.Info.RawTableSize.ToString("X")));
                listMapHeader.Children.Add(new MapHeaderEntry("Index Header Address:", PointerAddressString(_cacheFile.Info.IndexHeaderLocation)));
                listMapHeader.Children.Add(new MapHeaderEntry("Index Offset Magic:", "0x" + _cacheFile.Info.LocaleOffsetMask.ToString("X")));
                listMapHeader.Children.Add(new MapHeaderEntry("Map Magic:", "0x" + _cacheFile.Info.AddressMask.ToString("X8")));

                StatusUpdater.Update("Loaded Header Info");
            }));
        }

        private static string PointerAddressString(Pointer pointer)
        {
            if (!pointer.HasAddress)
                return "0x00000000";
            return "0x" + pointer.AsAddress().ToString("X8");
        }

        private void LoadTags()
        {            
            // Load all the tag classes into data
            var classes = new List<TagClass>();
            var classWrappers = new Dictionary<ITagClass, TagClass>();
            Dispatcher.Invoke(new Action(() =>
                {
                    foreach (var tagClass in _cacheFile.TagClasses)
                    {
                        var wrapper = new TagClass(tagClass, CharConstant.ToString(tagClass.Magic), _cacheFile.StringIDs.GetString(tagClass.Description));
                        classes.Add(wrapper);
                        classWrappers[tagClass] = wrapper;
                    }
                }));

            Dispatcher.Invoke(new Action(() => StatusUpdater.Update("Loaded Tag Classes")));

            // Load all the tags into the treeview (into their class categoies)
            _hierarchy.Entries = new List<TagEntry>();
            foreach (var tag in _cacheFile.Tags)
            {
                if (!tag.MetaLocation.IsNull)
                {
                    var fileName = _cacheFile.FileNames.FindTagName(tag);
                    if (fileName == null || fileName.Trim() == "")
                        fileName = tag.Index.ToString();

                    var parentClass = classWrappers[tag.Class];
                    var entry = new TagEntry(tag, parentClass, fileName);
                    parentClass.Children.Add(entry);
                    _hierarchy.Entries.Add(entry);
                }
                else
                    _hierarchy.Entries.Add(null);
            }
            
            foreach (TagClass tagClass in classes)
                tagClass.Children.Sort((x, y) => String.Compare(x.TagFileName, y.TagFileName, StringComparison.OrdinalIgnoreCase));


            //// Taglist Generation
            /*string taglistPath = @"C:\" + _cacheFile.Info.InternalName.ToLower() + ".taglist";
            List<string> taglist = new List<string>();
            taglist.Add("<scenario=\"" + _cacheFile.Info.ScenarioName + "\">");
            for (int i = 0; i < _cacheFile.Tags.Count; i++)
            {
                ITag tag = _cacheFile.Tags[i];
                if (tag.Index.IsValid)
                    taglist.Add(string.Format("\t<tag id=\"{0}\" class=\"{1}\">{2}</tag>", tag.Index.ToString(), ExtryzeDLL.Util.CharConstant.ToString(tag.Class.Magic) ,_cacheFile.FileNames.FindTagName(tag.Index)));
            }
            taglist.Add("</scenario>");
            File.WriteAllLines(taglistPath, taglist.ToArray<string>());*/


            Dispatcher.Invoke(new Action(() => StatusUpdater.Update("Loaded Tags")));

            classes.Sort((x, y) => String.Compare(x.TagClassMagic, y.TagClassMagic, StringComparison.OrdinalIgnoreCase));
            Dispatcher.Invoke(new Action(delegate
                                             {
                                                 _tagsComplete = new ObservableCollection<TagClass>(classes);

                                                 // Load un-populated tags
                                                 foreach (var tagClass in _tagsComplete.Where(tagClass => tagClass.Children.Count > 0))
                                                     _tagsPopulated.Add(tagClass);
                                                 _hierarchy.Classes = _tagsPopulated;
                                             }));

            // Add to the treeview
            Dispatcher.Invoke(new Action(() => UpdateEmptyTags(cbShowEmptyTags.IsChecked != null && (bool) cbShowEmptyTags.IsChecked)));
        }
        private void LoadLocales()
        {
            //Dispatcher.Invoke(new Action(delegate { cbLocaleLanguages.Items.Clear(); }));
            /*int totalStrings = 0;
            foreach (ILanguage language in _cache.Languages)
                totalStrings += language.StringCount;
            Dispatcher.Invoke(new Action(delegate { lblLocaleTotalCount.Text = totalStrings.ToString(); }));*/

            // TODO: Define the language names in an XML file or something
            AddLanguage("English", LocaleLanguage.English);
            AddLanguage("Chinese", LocaleLanguage.Chinese);
            AddLanguage("Danish", LocaleLanguage.Danish);
            AddLanguage("Dutch", LocaleLanguage.Dutch);
            AddLanguage("Finnish", LocaleLanguage.Finnish);
            AddLanguage("French", LocaleLanguage.French);
            AddLanguage("German", LocaleLanguage.German);
            AddLanguage("Italian", LocaleLanguage.Italian);
            AddLanguage("Japanese", LocaleLanguage.Japanese);
            AddLanguage("Korean", LocaleLanguage.Korean);
            AddLanguage("Norwegian", LocaleLanguage.Norwegian);
            AddLanguage("Polish", LocaleLanguage.Polish);
            AddLanguage("Portuguese", LocaleLanguage.Portuguese);
            AddLanguage("Russian", LocaleLanguage.Russian);
            AddLanguage("Spanish", LocaleLanguage.Spanish);
            AddLanguage("Spanish (Latin American)", LocaleLanguage.LatinAmericanSpanish);
            AddLanguage("Extra", LocaleLanguage.Unknown);

            Dispatcher.Invoke(new Action(delegate
                {
                    lbLanguages.ItemsSource = _languages;
                    StatusUpdater.Update("Initialized Languages");
                }
            ));
        }
        private void LoadScripts()
        {
            if (_buildInfo.ScriptDefinitionsFilename == null) return;

            // TODO: Actually handle this properly for H4
            var scripts = new List<string>
                              {
                                  _cacheFile.Info.InternalName + ".hsc"
                              };

            Dispatcher.Invoke(new Action(delegate
                                             {
                                                 tabScripts.Visibility = Visibility.Visible;
                                                 lbScripts.ItemsSource = scripts;
                                                 StatusUpdater.Update("Initialized Scripts");
                                             }
                                  ));
        }

        private void AddLanguage(string name, int index)
        {
            if (index < 0 || index >= _cacheFile.Languages.Count) return;

            var baseLang = _cacheFile.Languages[index];
            if (baseLang.StringCount > 0)
                _languages.Add(new LanguageEntry(name, index, baseLang));
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
            if (Settings.halomapTagSort != _tagSorting)
            {
                // TODO: Update the tag sorting
            }

            if (Settings.halomapMapInfoDockSide != _dockSide)
                UpdateDockPanelLocation();
        }

        private void cbShowEmptyTags_Altered(object sender, RoutedEventArgs e) { UpdateEmptyTags(cbShowEmptyTags.IsChecked != null && (bool)cbShowEmptyTags.IsChecked); }
        private void UpdateEmptyTags(bool shown)
        {
            // Update Settings
            Settings.halomapShowEmptyClasses = shown;

            // Update TreeView
            tvTagList.DataContext = shown ? _tagsComplete : _tagsPopulated;

            // Fuck bitches, get money
            // #xboxscenefame
        }

        private void UpdateDockPanelLocation()
        {
            _dockSide = Settings.halomapMapInfoDockSide;

            if (_dockSide == Settings.MapInfoDockSide.Left)
            {
                colLeft.Width = new GridLength(300, GridUnitType.Star);
                colRight.Width = new GridLength(700, GridUnitType.Star);

                sideBar.SetValue(Grid.ColumnProperty, 0);
                mainContent.SetValue(Grid.ColumnProperty, 1);
            }
            else
            {
                colRight.Width = new GridLength(300, GridUnitType.Star);
                colLeft.Width = new GridLength(700, GridUnitType.Star);

                sideBar.SetValue(Grid.ColumnProperty, 1);
                mainContent.SetValue(Grid.ColumnProperty, 0);
            }
        }

        #region Content Menu Options
        void openTag_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        void openTagNewTab_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        void renameTaglistTag_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion

		#region ContextMenus

		public ContextMenu BaseContextMenu;
		public ContextMenu FilesystemContextMenu;

		/// <summary>
		/// Really hacky, but i didn't want to re-do the TabControl to make it DataBinded...
		/// </summary>
		private void InitalizeContextMenus()
		{
			// Create Lame Context Menu
			BaseContextMenu = new ContextMenu();
			BaseContextMenu.Items.Add(new MenuItem { Header = "Close" }); ((MenuItem)BaseContextMenu.Items[0]).Click += contextMenuClose_Click;
			BaseContextMenu.Items.Add(new MenuItem { Header = "Close All" }); ((MenuItem)BaseContextMenu.Items[1]).Click += contextMenuCloseAll_Click;
			BaseContextMenu.Items.Add(new MenuItem { Header = "Close All But This" }); ((MenuItem)BaseContextMenu.Items[2]).Click += contextMenuCloseAllButThis_Click;
			BaseContextMenu.Items.Add(new MenuItem { Header = "Close Tabs To The Left" }); ((MenuItem)BaseContextMenu.Items[3]).Click += contextMenuCloseToLeft_Click;
			BaseContextMenu.Items.Add(new MenuItem { Header = "Close Tabs To The Right" }); ((MenuItem)BaseContextMenu.Items[4]).Click += contextMenuCloseToRight_Click;

			// Create Fun Context Menu
			FilesystemContextMenu = new ContextMenu();
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Close" }); ((MenuItem)FilesystemContextMenu.Items[0]).Click += contextMenuClose_Click;
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Close All" }); ((MenuItem)FilesystemContextMenu.Items[1]).Click += contextMenuCloseAll_Click;
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Close All But This" }); ((MenuItem)FilesystemContextMenu.Items[2]).Click += contextMenuCloseAllButThis_Click;
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Close Tabs To The Left" }); ((MenuItem)FilesystemContextMenu.Items[3]).Click += contextMenuCloseToLeft_Click;
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Close Tabs To The Right" }); ((MenuItem)FilesystemContextMenu.Items[4]).Click += contextMenuCloseToRight_Click;
			FilesystemContextMenu.Items.Add(new Separator());
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Copy File Path" }); ((MenuItem)FilesystemContextMenu.Items[6]).Click += contextMenuCopyFilePath_Click;
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Open Containing Folder" }); ((MenuItem)FilesystemContextMenu.Items[7]).Click += contextMenuOpenContainingFolder_Click;
		}

		private void contextMenuClose_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			ExternalTabClose(tabitem);
		}
		private void contextMenuCloseAll_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var toDelete = contentTabs.Items.OfType<CloseableTabItem>().Cast<TabItem>().ToList();

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
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;

			var toDelete = contentTabs.Items.OfType<CloseableTabItem>().Where(tab => !Equals(tab, tabitem)).Cast<TabItem>().ToList();

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
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			var selectedIndexOfTab = GetSelectedIndex(tabitem);

			var toDelete = new List<TabItem>();
			for (var i = 0; i < selectedIndexOfTab; i++)
				toDelete.Add((TabItem)contentTabs.Items[i]);

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
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			var selectedIndexOfTab = GetSelectedIndex(tabitem);

			var toDelete = new List<TabItem>();
			for (var i = selectedIndexOfTab + 1; i < contentTabs.Items.Count; i++)
				toDelete.Add((TabItem)contentTabs.Items[i]);

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
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
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
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			if (tabitem == null) return;

			var filepathArgument = @"/select, " + tabitem.Tag;
			Process.Start("explorer.exe", filepathArgument);
		}
		#endregion

        #region Editors
        private void btnEditorsString_Click(object sender, RoutedEventArgs e)
        {
            if (IsTagOpen("StringID Viewer"))
                SelectTabFromTitle("StringID Viewer");
            else
            {
                var tab = new CloseableTabItem
                              {
                                  Header = "StringID Viewer", 
                                  Content = new Components.Editors.StringEditor(_cacheFile)
                              };

                contentTabs.Items.Add(tab);
                contentTabs.SelectedItem = tab;
            }
        }
        #endregion

        #region Tab Management
        /// <summary>
        /// Check to see if a tag is already open in the Editor Pane
        /// </summary>
        /// <param name="tabTitle">THe title of the tag to search for</param>
        /// <returns></returns>
        private bool IsTagOpen(string tabTitle)
        {
            return contentTabs.Items.Cast<CloseableTabItem>().Any(tab => ((ContentControl)tab.Header).Content.ToString().ToLower() == tabTitle.ToLower());
        }

        /// <summary>
        /// Check to see if a tag is already open in the Editor Pane
        /// </summary>
        /// <param name="tag">The tag to search for</param>
        private bool IsTagOpen(TagEntry tag)
        {
            return contentTabs.Items.Cast<CloseableTabItem>().Any(tab => tab.Tag == tag);
        }

        /// <summary>
        /// Select a tab based on a Tag Title
        /// </summary>
        /// <param name="tabTitle">The tag title to search for</param>
        private void SelectTabFromTitle(string tabTitle)
        {
            CloseableTabItem tab = null;

            foreach (var tabb in contentTabs.Items.Cast<CloseableTabItem>().Where(tabb => ((ContentControl)tabb.Header).Content.ToString().ToLower() == tabTitle.ToLower()))
                tab = tabb;

            if (tab != null)
                contentTabs.SelectedItem = tab;
        }
        /// <summary>
        /// Select a tab based on a TagEntry
        /// </summary>
        /// <param name="tag">The tag to search for</param>
        private void SelectTabFromTag(TagEntry tag)
        {
            CloseableTabItem tab = null;

            foreach (var tabb in contentTabs.Items.Cast<CloseableTabItem>().Where(tabb => tabb.Tag == tag))
                tab = tabb;

            if (tab != null)
                contentTabs.SelectedItem = tab;
        }

        public void OpenTag(TagEntry tag)
        {
            if (!IsTagOpen(tag))
                {
                    contentTabs.Items.Add(new CloseableTabItem
                    {
                        Header = new ContentControl
						{ 
							Content = string.Format("{0}.{1}", tag.TagFileName.Substring(tag.TagFileName.LastIndexOf('\\') + 1), CharConstant.ToString(tag.RawTag.Class.Magic)),
							ContextMenu = BaseContextMenu
						},
                        Tag = tag,
                        Content = new MetaContainer(_buildInfo, tag, _hierarchy, _cacheFile, _mapManager)
                    });
                }

                SelectTabFromTag(tag);
        }

		public void ExternalTabClose(TabItem tab, bool updateFocus = true)
		{
			contentTabs.Items.Remove(tab);

			if (!updateFocus) return;

			foreach (var datTab in contentTabs.Items.Cast<TabItem>().Where(datTab => ((ContentControl)datTab.Header).Content.ToString() == "Start Page"))
			{
				contentTabs.SelectedItem = datTab;
				return;
			}

			if (contentTabs.Items.Count > 0)
				contentTabs.SelectedIndex = contentTabs.Items.Count - 1;
		}
		public void ExternalTabsClose(List<TabItem> tab, bool updateFocus = true)
		{
			foreach (var tabItem in tab)
				contentTabs.Items.Remove(tabItem);

			if (!updateFocus) return;

			foreach (var datTab in contentTabs.Items.Cast<TabItem>().Where(datTab => ((ContentControl)datTab.Header).Content.ToString() == "Start Page"))
			{
				contentTabs.SelectedItem = datTab;
				return;
			}

			if (contentTabs.Items.Count > 0)
				contentTabs.SelectedIndex = contentTabs.Items.Count - 1;
		}

		public int GetSelectedIndex(TabItem selectedTab)
		{
			var index = 0;
			foreach (var tab in contentTabs.Items)
			{
				if (Equals(tab, selectedTab))
					return index;

				index++;
			}

			throw new Exception();
		}
        #endregion

        private void tvTagList_SelectedTagChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Check it's actually a tag, and not a class the user clicked
            var selectedItem = ((TreeView)sender).SelectedItem as TagEntry;
            if (selectedItem != null)
                OpenTag(selectedItem);
        }

        private void tvTagList_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = e.Source as TreeViewItem;
            if (item == null) return;
            var entry = item.DataContext as TagEntry;
            if (entry != null)
                OpenTag(entry);
        }

        private void LocaleButtonClick(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            var language = (LanguageEntry)element.DataContext;
            var tabName = language.Name + " Locales";
            if (IsTagOpen(tabName))
            {
                SelectTabFromTitle(tabName);
            }
            else
            {
                var tab = new CloseableTabItem
                              {
                                  Header = new ContentControl()
	                                           {
												   Content = tabName,
												   ContextMenu = BaseContextMenu
	                                           },
                                  Content = new Components.Editors.LocaleEditor(_cacheFile, _mapManager, language.Index)
                              };

                contentTabs.Items.Add(tab);
                contentTabs.SelectedItem = tab;
            }
        }

        private void ScriptButtonClick(object sender, RoutedEventArgs e)
        {
            var tabName = _cacheFile.Info.InternalName.Replace("_", "__") + ".hsc";
            if (IsTagOpen(tabName))
            {
                SelectTabFromTitle(tabName);
            }
            else
            {
                var tab = new CloseableTabItem
                              {
								  Header = new ContentControl()
								  {
									  Content = tabName,
									  ContextMenu = BaseContextMenu
								  },
                                  Content = new Components.Editors.ScriptEditor(_cacheFile, _buildInfo.ScriptDefinitionsFilename)
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
            var result = sfd.ShowDialog();
            if (!result.Value)
                return;

            // Dump all of the tags that belong to the class
            using (var writer = new StreamWriter(sfd.FileName))
            {
                foreach (var tag in _cacheFile.Tags)
                {
                    if (tag == null || tag.Class != tagClass.RawClass) continue;

                    var name = _cacheFile.FileNames.FindTagName(tag);
                    if (name != null)
                        writer.WriteLine("{0}={1}", tag.Index, name);
                }
            }

            MetroMessageBox.Show("Dump Successful", "Tag list dumped successfully.");
        }
    }
}