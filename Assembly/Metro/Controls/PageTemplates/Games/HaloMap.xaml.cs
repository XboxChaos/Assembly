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
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components;
using System.Collections;
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
            this.AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(this.CloseTab));

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

            BackgroundWorker initalLoadBackgroundWorker = new BackgroundWorker();
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
            if (e.Error != null)
            {
                // Close Tab
                Settings.homeWindow.ExternalTabClose(_tab);

                MetroException.Show(e.Error);
            }
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
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, 4);

            if (buffer[0] == 'h' && buffer[1] == 'e' && buffer[2] == 'a' && buffer[3] == 'd')
                return Endian.BigEndian;
            else
                return Endian.LittleEndian;
        }
        
        public void InitalizeMap()
        {
            using (Stream fileStream = File.OpenRead(_cacheLocation))
            {
                Endian endianness = GetEndianness(fileStream);
                _mapManager = new FileStreamManager(_cacheLocation, endianness);
                EndianReader reader = new EndianReader(fileStream, endianness);
                Dispatcher.Invoke(new Action(delegate { StatusUpdater.Update("Opened File"); }));

                _version = new CacheFileVersionInfo(reader);
                _supportedBuilds = XDocument.Load(VariousFunctions.GetApplicationLocation() + @"Formats\SupportedBuilds.xml");
                _layoutLoader = new BuildInfoLoader(_supportedBuilds, VariousFunctions.GetApplicationLocation() + @"Formats\");
                _buildInfo = _layoutLoader.LoadBuild(_version.BuildString);

                Dispatcher.Invoke(new Action(delegate { StatusUpdater.Update("Loaded Build Definitions"); }));

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

                            Settings.homeWindow.ExternalTabClose((TabItem)this.Parent);
                        }));
                    return;
                }
                Dispatcher.Invoke(new Action(delegate
                {
                    if (Settings.startpageHideOnLaunch)
                        Settings.homeWindow.ExternalTabClose(Windows.Home.TabGenre.StartPage);
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
                Dispatcher.Invoke(new Action(delegate { StatusUpdater.Update("Loaded Cache File"); }));

                // Add to Recents
                Dispatcher.Invoke(new Action(delegate
                {
                    RecentFiles.AddNewEntry(System.IO.Path.GetFileName(_cacheLocation), _cacheLocation, _buildInfo.ShortName, Settings.RecentFileType.Cache);
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
                FileInfo fi = new FileInfo(_cacheLocation);
                _tab.Header = fi.Name.Replace("_", "__"); Settings.homeWindow.UpdateTitleText(fi.Name.Replace(fi.Extension, ""));
                lblMapName.Text = _cacheFile.Info.InternalName;

                lblMapHeader.Text = "Map Header;";
                listMapHeader.Children.Clear();
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Game:", _buildInfo.GameName));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Build:", _cacheFile.Info.BuildString.ToString()));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Type:", _cacheFile.Info.Type.ToString()));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Internal Name:", _cacheFile.Info.InternalName));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Scenario Name:", _cacheFile.Info.ScenarioName));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Virtual Base:", "0x" + _cacheFile.Info.VirtualBaseAddress.ToString("X8")));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Virtual Size:", "0x" + _cacheFile.Info.MetaSize.ToString("X")));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("SDK Version:", _cacheFile.Info.XDKVersion.ToString()));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Raw Table Offset:", "0x" + _cacheFile.Info.RawTableOffset.ToString("X8")));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Raw Table Size:", "0x" + _cacheFile.Info.RawTableSize.ToString("X")));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Index Header Address:", PointerAddressString(_cacheFile.Info.IndexHeaderLocation)));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Index Offset Magic:", "0x" + _cacheFile.Info.LocaleOffsetMask.ToString("X")));
                listMapHeader.Children.Add(new Components.MapHeaderEntry("Map Magic:", "0x" + _cacheFile.Info.AddressMask.ToString("X8")));

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
            List<TagClass> classes = new List<TagClass>();
            Dictionary<ITagClass, TagClass> classWrappers = new Dictionary<ITagClass, TagClass>();
            Dispatcher.Invoke(new Action(() =>
                {
                    foreach (ITagClass tagClass in _cacheFile.TagClasses)
                    {
                        TagClass wrapper = new TagClass(tagClass, CharConstant.ToString(tagClass.Magic), _cacheFile.StringIDs.GetString(tagClass.Description));
                        classes.Add(wrapper);
                        classWrappers[tagClass] = wrapper;
                    }
                }));

            Dispatcher.Invoke(new Action(delegate { StatusUpdater.Update("Loaded Tag Classes"); }));

            // Load all the tags into the treeview (into their class categoies)
            _hierarchy.Entries = new List<TagEntry>();
            for (int i = 0; i < _cacheFile.Tags.Count; i++)
            {
                ITag tag = _cacheFile.Tags[i];
                if (!tag.MetaLocation.IsNull)
                {
                    string fileName = _cacheFile.FileNames.FindTagName(tag);
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
            if (_buildInfo.ScriptDefinitionsFilename != null)
            {
                // TODO: Actually handle this properly for H4
                List<string> scripts = new List<string>();
                scripts.Add(_cacheFile.Info.InternalName + ".hsc");

                Dispatcher.Invoke(new Action(delegate
                    {
                        tabScripts.Visibility = Visibility.Visible;
                        lbScripts.ItemsSource = scripts;
                        StatusUpdater.Update("Initialized Scripts");
                    }
                ));
            }
        }

        private void AddLanguage(string name, int index)
        {
            if (index >= 0 && index < _cacheFile.Languages.Count)
            {
                ILanguage baseLang = _cacheFile.Languages[index];
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
                tab.Content = new Components.Editors.StringEditor(_cacheFile);

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
                        Content = new MetaContainer(_buildInfo, tag, _hierarchy, _cacheFile, _mapManager)
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
                tab.Content = new Components.Editors.LocaleEditor(_cacheFile, _mapManager, language.Index);

                contentTabs.Items.Add(tab);
                contentTabs.SelectedItem = tab;
            }
        }

        private void ScriptButtonClick(object sender, RoutedEventArgs e)
        {
            string tabName = _cacheFile.Info.InternalName.Replace("_", "__") + ".hsc";
            if (IsTagOpen(tabName))
            {
                SelectTabFromTitle(tabName);
            }
            else
            {
                CloseableTabItem tab = new CloseableTabItem();
                tab.Header = tabName;
                tab.Content = new Components.Editors.ScriptEditor(_cacheFile, _buildInfo.ScriptDefinitionsFilename);

                contentTabs.Items.Add(tab);
                contentTabs.SelectedItem = tab;
            }
        }

        private void DumpClassTagList(object sender, RoutedEventArgs e)
        {
            // Get the menu item and the tag class
            MenuItem item = e.Source as MenuItem;
            if (item == null)
                return;
            TagClass tagClass = item.DataContext as TagClass;
            if (tagClass == null)
                return;

            // Ask the user where to save the dump
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save Tag List";
            sfd.Filter = "Text Files|*.txt|Tag Lists|*.taglist|All Files|*.*";
            bool? result = sfd.ShowDialog();
            if (!result.HasValue || !result.Value)
                return;

            // Dump all of the tags that belong to the class
            using (StreamWriter writer = new StreamWriter(sfd.FileName))
            {
                foreach (ITag tag in _cacheFile.Tags)
                {
                    if (tag != null && tag.Class == tagClass.RawClass)
                    {
                        string name = _cacheFile.FileNames.FindTagName(tag);
                        if (name != null)
                            writer.WriteLine("{0}={1}", tag.Index, name);
                    }
                }
            }

            MetroMessageBox.Show("Dump Successful", "Tag list dumped successfully.");
        }
    }
}