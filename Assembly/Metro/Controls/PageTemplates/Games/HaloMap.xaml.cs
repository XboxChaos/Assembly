using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.ObjectModel;

using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.IO;
using Assembly.Metro.Dialogs;
using ExtryzeDLL.Util;
using CloseableTabItemDemo;
using Assembly.Backend;
using Assembly.Metro.Controls.PageTemplates.Games.Components;
using System.Collections;
using Assembly.Windows;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Flexibility;

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
        private Stream _fileStream;
        private EndianStream _stream;
        private ThirdGenVersionInfo _version;
        private XDocument _supportedBuilds;
        private BuildInfoLoader _layoutLoader;
        private BuildInformation _buildInfo;
        private ThirdGenCacheFile _cache;
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
        public Stream FileStream { get { return _fileStream; } set { _fileStream = value; } }
        public TagHierarchy TagHierarch { get { return _hierarchy; } set { _hierarchy = value; } }
        public EndianStream Stream { get { return _stream; } set { _stream = value; } }
        public ThirdGenVersionInfo Version { get { return _version; } set { _version = value; } }
        public XDocument SupportedBuilds { get { return _supportedBuilds; } set { _supportedBuilds = value; } }
        public BuildInfoLoader LayoutLoader { get { return _layoutLoader; } set { _layoutLoader = value; } }
        public BuildInformation BuildInformation { get { return _buildInfo; } set { _buildInfo = value; } }
        public ThirdGenCacheFile Cache { get { return _cache; } set { _cache = value; } }
        public string CacheLocation { get { return _cacheLocation; } set { _cacheLocation = value; } }
        public TabItem Tab { get { return _tab; } set { _tab = value; } }
        #endregion

        /// <summary>
        /// New Instance of the Halo Map Location
        /// </summary>
        /// <param name="cacheLocation"></param>
        public HaloMap(string cacheLocation, TabItem tab)
        {
            InitializeComponent();
            this.AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(this.CloseTab));

            _tab = tab;
            _cacheLocation = cacheLocation;

            // Update dockpanel location
            UpdateDockPanelLocation();

            // Show UI Pending Stuff
            doingAction.Visibility = Visibility.Visible;

            // Read Settings
            cbShowEmptyTags.IsChecked = Settings.halomapShowEmptyClasses;
            Settings.SettingsChanged += SettingsChanged;

            BackgroundWorker initalLoadBackgroundWorker = new BackgroundWorker();
            initalLoadBackgroundWorker.DoWork += initalLoadBackgroundWorker_DoWork;
            initalLoadBackgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

            initalLoadBackgroundWorker.RunWorkerAsync();
        }

        void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            doingAction.Visibility = Visibility.Hidden;
            if (e.Error != null)
            {
                // Close Tab
                Settings.homeWindow.ExternalTabClose(_tab);

                MetroException.Show(e.Error);

                if (_stream != null)
                    _stream.Close();
            }
        }

        void initalLoadBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            InitalizeMap();
        }
        
        public void InitalizeMap()
        {
            _fileStream = new FileStream(_cacheLocation, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            _stream = new EndianStream(_fileStream, Endian.BigEndian);
            Dispatcher.Invoke(new Action(delegate { StatusUpdater.Update("Opened File"); }));

            _version = new ThirdGenVersionInfo(_stream);
            _supportedBuilds = XDocument.Load(VariousFunctions.GetApplicationLocation() + @"Formats\SupportedBuilds.xml");
            _layoutLoader = new BuildInfoLoader(_supportedBuilds, VariousFunctions.GetApplicationLocation() + @"Formats\");
            _buildInfo = _layoutLoader.LoadBuild(_version.BuildString);

            Dispatcher.Invoke(new Action(delegate { StatusUpdater.Update("Loaded Build Definitions"); }));

            if (_buildInfo == null)
            {
                Dispatcher.Invoke(new Action(delegate
                    {
                        if (!_0xabad1dea.IWff.Heman(_stream))
                        {
                            StatusUpdater.Update("Not a supported cache build");
                            MetroMessageBox.Show("Unable to open cache file", "Unsupported blam engine build \"" + _version.BuildString + "\". Why not add support in the 'Formats' folder?");
                        }
                        else
                        {
                            StatusUpdater.Update("HEYYEYAAEYAAAEYAEYAA");
                        }

                        Settings.homeWindow.ExternalTabClose((TabItem)this.Parent);
                    }));
                return;
            }
            Dispatcher.Invoke(new Action(delegate
            {
                if (Settings.startpageHideOnLaunch)
                    Settings.homeWindow.ExternalTabClose(Windows.Home.TabGenre.StartPage);
            }));

            // Load Blam Cache
            _cache = new ThirdGenCacheFile(_stream, _buildInfo, _version.BuildString);
            Dispatcher.Invoke(new Action(delegate { StatusUpdater.Update("Loaded Cache File"); }));

            // Add to Recents
            Dispatcher.Invoke(new Action(delegate 
            {
                RecentFiles.AddNewEntry(new FileInfo(_cacheLocation).Name, _cacheLocation, _buildInfo.ShortName, Settings.RecentFileType.Cache);
                StatusUpdater.Update("Added To Recents"); 
            }));

            LoadHeader();
            LoadTags();
            LoadLocales();
        }
        private void LoadHeader()
        {
            Dispatcher.Invoke(new Action(delegate
            {
                FileInfo fi = new FileInfo(_cacheLocation);
                _tab.Header = fi.Name.Replace("_", "__"); Settings.homeWindow.UpdateTitleText(fi.Name.Replace(fi.Extension, ""));
                lblMapName.Text = _cache.Info.InternalName;

                lblMapHeader.Text = "Map Header;";
                listMapHeader.Children.Clear();
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Game:", _buildInfo.GameName));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Build:", _cache.Info.BuildString.ToString()));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Type:", _cache.Info.Type.ToString()));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Internal Name:", _cache.Info.InternalName));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Scenario Name:", _cache.Info.ScenarioName));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Virtual Base:", "0x" + _cache.Info.MetaBase.AsAddress().ToString("X8")));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Virtual Size:", "0x" + _cache.Info.MetaSize.ToString("X")));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("SDK Version:", _cache.Info.XDKVersion.ToString()));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Raw Table Offset:", "0x" + _cache.Info.RawTableOffset.ToString("X8")));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Raw Table Size:", "0x" + _cache.Info.RawTableSize.ToString("X")));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Index Header Address:", "0x" + _cache.Info.IndexHeaderLocation.AsAddress().ToString("X8")));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Index Offset Magic:", "0x" + _cache.Info.LocaleOffsetMask.ToString("X")));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Map Magic:", "0x" + _cache.Info.AddressMask.ToString("X8")));

                StatusUpdater.Update("Loaded Header Info");
            }));
        }
        private void LoadTags()
        {            
            // Load all the tag classes into data
            List<TagClass> classes = new List<TagClass>();
            Dictionary<ITagClass, TagClass> classWrappers = new Dictionary<ITagClass, TagClass>();
            Dispatcher.Invoke(new Action(() =>
                {
                    foreach (ITagClass tagClass in _cache.TagClasses)
                    {
                        TagClass wrapper = new TagClass(tagClass, CharConstant.ToString(tagClass.Magic));
                        classes.Add(wrapper);
                        classWrappers[tagClass] = wrapper;
                    }
                }));

            Dispatcher.Invoke(new Action(delegate { StatusUpdater.Update("Loaded Tag Classes"); }));

            // Load all the tags into the treeview (into their class categoies)
            _hierarchy.Entries = new List<TagEntry>();
            for (int i = 0; i < _cache.Tags.Count; i++)
            {
                ITag tag = _cache.Tags[i];
                if (tag.Index.IsValid)
                {
                    string fileName = _cache.FileNames.FindTagName(tag);
                    if (fileName == null || fileName.Trim() == "")
                        fileName = tag.Index.ToString();

                    TagClass parentClass = classWrappers[tag.Class];
                    TagEntry entry = new TagEntry(tag, parentClass, fileName);
                    parentClass.Children.Add(entry);
                    _hierarchy.Entries.Add(entry);
                }
                else
                    _hierarchy.Entries.Add(null);
            }
            
            foreach (TagClass tagClass in classes)
                tagClass.Children.Sort((x, y) => string.Compare(x.TagFileName, y.TagFileName, true));


            //// Taglist Generation
            string taglistPath = @"C:\" + _cache.Info.InternalName.ToLower() + ".taglist";
            List<string> taglist = new List<string>();
            taglist.Add("<scenario=\"" + _cache.Info.ScenarioName + "\">");
            for (int i = 0; i < _cache.Tags.Count; i++)
            {
                ITag tag = _cache.Tags[i];
                if (tag.Index.IsValid)
                    taglist.Add(string.Format("\t<tag id=\"{0}\" class=\"{1}\">{2}</tag>", tag.Index.ToString(), ExtryzeDLL.Util.CharConstant.ToString(tag.Class.Magic) ,_cache.FileNames.FindTagName(tag.Index)));
            }
            taglist.Add("</scenario>");
            File.WriteAllLines(taglistPath, taglist.ToArray<string>());


            Dispatcher.Invoke(new Action(delegate { StatusUpdater.Update("Loaded Tags"); }));

            classes.Sort((x, y) => string.Compare(x.TagClassMagic, y.TagClassMagic, true));
            Dispatcher.Invoke(new Action(() =>
                {
                    _tagsComplete = new ObservableCollection<TagClass>(classes);

                    // Load un-populated tags
                    foreach (TagClass tagClass in _tagsComplete)
                        if (tagClass.Children.Count > 0)
                            _tagsPopulated.Add(tagClass);
                    _hierarchy.Classes = _tagsPopulated;
                }));

            // Add to the treeview
            Dispatcher.Invoke(new Action(delegate { UpdateEmptyTags((bool)cbShowEmptyTags.IsChecked); }));
        }
        private void LoadLocales()
        {
            //Dispatcher.Invoke(new Action(delegate { cbLocaleLanguages.Items.Clear(); }));
            /*int totalStrings = 0;
            foreach (ILanguage language in _cache.Languages)
                totalStrings += language.StringCount;
            Dispatcher.Invoke(new Action(delegate { lblLocaleTotalCount.Text = totalStrings.ToString(); }));*/

            AddLanguage("English", LocaleLanguage.English);
            AddLanguage("Chinese", LocaleLanguage.Chinese);
            AddLanguage("French", LocaleLanguage.French);
            AddLanguage("German", LocaleLanguage.German);
            AddLanguage("Italian", LocaleLanguage.Italian);
            AddLanguage("Japanese", LocaleLanguage.Japanese);
            AddLanguage("Korean", LocaleLanguage.Korean);
            AddLanguage("Polish", LocaleLanguage.Polish);
            AddLanguage("Portuguese", LocaleLanguage.Portuguese);
            AddLanguage("Spanish", LocaleLanguage.Spanish);
            AddLanguage("Spanish (Latin American)", LocaleLanguage.LatinAmericanSpanish);
            AddLanguage("Unknown", LocaleLanguage.Unknown);

            Dispatcher.Invoke(new Action(delegate { lbLanguages.ItemsSource = _languages; }));
            Dispatcher.Invoke(new Action(delegate { StatusUpdater.Update("Initialized Languages"); }));
        }

        private void AddLanguage(string name, int index)
        {
            if (index >= 0 && index < _cache.Languages.Count)
            {
                ILanguage baseLang = _cache.Languages[index];
                if (baseLang.StringCount > 0)
                    _languages.Add(new LanguageEntry(name, index, baseLang));
            }
        }
        
        private void CloseTab(object source, RoutedEventArgs args)
        {
            TabItem tabItem = args.OriginalSource as TabItem;
            if (tabItem != null)
            {
                TabControl tabControl = tabItem.Parent as TabControl;
                if (tabControl != null)
                    tabControl.Items.Remove(tabItem);
            }
        }

        /// <summary>
        /// Close the Stream nao
        /// </summary>
        public bool Close()
        {
            _stream.Close();
            return true;
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

        private void cbShowEmptyTags_Altered(object sender, System.Windows.RoutedEventArgs e) { UpdateEmptyTags((bool)cbShowEmptyTags.IsChecked); }
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

        #region Editors
        private void btnEditorsString_Click(object sender, RoutedEventArgs e)
        {
            if (IsTagOpen("StringID Viewer"))
                SelectTabFromTitle("StringID Viewer");
            else
            {
                CloseableTabItem tab = new CloseableTabItem();
                tab.Header = "StringID Viewer";
                tab.Content = new Components.Editors.StringEditor(_cache);

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
            foreach (CloseableTabItem tab in contentTabs.Items)
                if (tab.Header.ToString().ToLower() == tabTitle.ToLower())
                    return true;

            return false;
        }
        /// <summary>
        /// Check to see if a tag is already open in the Editor Pane
        /// </summary>
        /// <param name="tag">The tag to search for</param>
        private bool IsTagOpen(TagEntry tag)
        {
            foreach (CloseableTabItem tab in contentTabs.Items)
                if (tab.Tag == tag)
                    return true;

            return false;
        }

        /// <summary>
        /// Select a tab based on a Tag Title
        /// </summary>
        /// <param name="tabTitle">The tag title to search for</param>
        private void SelectTabFromTitle(string tabTitle)
        {
            CloseableTabItem tab = null;

            foreach (CloseableTabItem tabb in contentTabs.Items)
                if (tabb.Header.ToString().ToLower() == tabTitle.ToLower())
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

            foreach (CloseableTabItem tabb in contentTabs.Items)
                if (tabb.Tag == tag)
                    tab = tabb;

            if (tab != null)
                contentTabs.SelectedItem = tab;
        }

        public void OpenTag(TagEntry tag)
        {
            if (!IsTagOpen(tag))
                {
                    contentTabs.Items.Add(new CloseableTabItem()
                    {
                        Header = string.Format("{0}.{1}", tag.TagFileName.Substring(tag.TagFileName.LastIndexOf('\\') + 1).Replace("_", "__"), CharConstant.ToString(tag.RawTag.Class.Magic)),
                        Tag = tag,
                        Content = new MetaContainer(_buildInfo, tag, _hierarchy, _cache, _stream)
                    });
                }

                SelectTabFromTag(tag);
        }
        #endregion
        private void tvTagList_SelectedTagChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Check it's actually a tag, and not a class the user clicked
            if (((TreeView)sender).SelectedItem is TagEntry)
                OpenTag((TagEntry)((TreeView)sender).SelectedItem);
        }

        private void tvTagList_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item = e.Source as TreeViewItem;
            if (item != null)
            {
                TagEntry entry = item.DataContext as TagEntry;
                if (entry != null)
                    OpenTag(entry);
            }
        }

        private void LocaleButtonClick(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            LanguageEntry language = (LanguageEntry)element.DataContext;
            string tabName = language.Name + " Locales";
            if (IsTagOpen(tabName))
            {
                SelectTabFromTitle(tabName);
            }
            else
            {
                CloseableTabItem tab = new CloseableTabItem();
                tab.Header = tabName;
                tab.Content = new Components.Editors.LocaleEditor(_cache, _stream, language.Index);

                contentTabs.Items.Add(tab);
                contentTabs.SelectedItem = tab;
            }
        }

        private void btnEditorsScript_Click(object sender, RoutedEventArgs e)
        {
            string tabName = _cache.Info.InternalName.Replace("_", "__") + ".hsc";
            if (IsTagOpen(tabName))
            {
                SelectTabFromTitle(tabName);
            }
            else
            {
                CloseableTabItem tab = new CloseableTabItem();
                tab.Header = tabName;
                tab.Content = new Components.Editors.ScriptEditor(_cache, _buildInfo.ScriptDefinitionsFilename);

                contentTabs.Items.Add(tab);
                contentTabs.SelectedItem = tab;
            }
        }
    }
}