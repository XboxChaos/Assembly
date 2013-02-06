using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Assembly.Helpers;
using Assembly.Helpers.Plugins;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Assembly.Metro.Dialogs;
using Assembly.Windows;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;
using ExtryzeDLL.Plugins;
using ExtryzeDLL.RTE;
using ExtryzeDLL.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
    class SearchResult
    {
        /// <summary>
        /// Constructs a new search result holder.
        /// </summary>
        /// <param name="foundField">The field that was found and highlighted.</param>
        /// <param name="listField">The top-level field in the field list. For reflexive entries, this is the topmost wrapper field, otherwise, this may be the same as foundField.</param>
        /// <param name="parent">The reflexive that the field is in. Can be null.</param>
        public SearchResult(MetaField foundField, MetaField listField, ReflexiveData parent)
        {
            ListField = listField;
            Field = foundField;
            Reflexive = parent;
        }

        public MetaField Field { get; private set; }
        public MetaField ListField { get; private set; }
        public ReflexiveData Reflexive { get; private set; }
    }

    /// <summary>
    /// Interaction logic for MetaEditor.xaml
    /// </summary>
    public partial class MetaEditor : UserControl
    {
	    private MetaContainer _parentMetaContainer;
        private IStreamManager _streamManager;
        private TagEntry _tag;
        private TagHierarchy _tags;
        private BuildInformation _buildInfo;
        private ICacheFile _cache;
        private string _pluginPath;
        private ThirdGenPluginVisitor _pluginVisitor;
        private bool hasInitFinished = false;
        private ReflexiveFlattener _flattener;
        private IRTEProvider _rteProvider;

        private ObservableCollection<SearchResult> _searchResults;
        private Dictionary<MetaField, int> _resultIndices = new Dictionary<MetaField, int>();
        private Timer _searchTimer;

        private FieldChangeTracker _changeTracker;
        private FieldChangeSet _fileChanges;
        private FieldChangeSet _memoryChanges;

		public static RoutedCommand ViewValueAsCommand = new RoutedCommand();
		public static RoutedCommand GoToPlugin = new RoutedCommand();

		public MetaEditor(BuildInformation buildInfo, TagEntry tag, MetaContainer parentContainer, TagHierarchy tags, ICacheFile cache, IStreamManager streamManager, IRTEProvider rteProvider)
        {
            InitializeComponent();

	        _parentMetaContainer = parentContainer;
            _tag = tag;
            _tags = tags;
            _buildInfo = buildInfo;
            _cache = cache;
            _streamManager = streamManager;
            _rteProvider = rteProvider;
            _searchTimer = new Timer(SearchTimer);

            // Load Plugin Path
            string className = VariousFunctions.SterilizeTagClassName(CharConstant.ToString(tag.RawTag.Class.Magic));
            _pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins", _buildInfo.PluginFolder, className);

            // Set Invisibility box
            cbShowInvisibles.IsChecked = Settings.pluginsShowInvisibles;

            // Load Meta
            RefreshEditor();

            // Set init finished
            hasInitFinished = true;
        }

        public void RefreshEditor()
        {
            if (File.Exists(_pluginPath))
            {
                // Load Plugin File
                XmlReader xml = XmlReader.Create(_pluginPath);

                // Load Meta from Plugin
                //try
                {
                    _pluginVisitor = new ThirdGenPluginVisitor(_tags, Settings.pluginsShowInvisibles);
                    AssemblyPluginLoader.LoadPlugin(xml, _pluginVisitor);

                    _changeTracker = new FieldChangeTracker();
                    _fileChanges = new FieldChangeSet();
                    _memoryChanges = new FieldChangeSet();

                    uint baseOffset = _tag.RawTag.MetaLocation.AsOffset();
                    MetaReader metaReader = new MetaReader(_streamManager, baseOffset, _cache, _buildInfo, _fileChanges);
                    _flattener = new ReflexiveFlattener(metaReader, _changeTracker, _fileChanges);
                    _flattener.Flatten(_pluginVisitor.Values);
                    metaReader.ReadFields(_pluginVisitor.Values);

                    panelMetaComponents.ItemsSource = _pluginVisitor.Values;

                    // Start monitoring fields for changes
                    _changeTracker.RegisterChangeSet(_fileChanges);
                    _changeTracker.RegisterChangeSet(_memoryChanges);
                    _changeTracker.Attach(_pluginVisitor.Values);

                    // Update Meta Toolbar
                    UpdateMetaButtons(true);
                }
                //catch (Exception ex)
                //{
                //    MetroException.Show(ex);
                //}
            }
            else
            {
                UpdateMetaButtons(false);
                StatusUpdater.Update("Plugin doesn't exist. It can't be loaded for this tag.");
            }
        }

        private void RevisionViewer()
        {
            if (_pluginVisitor != null && _pluginVisitor.PluginRevisions != null)
                MetroPluginRevisionViewer.Show(_pluginVisitor.PluginRevisions, CharConstant.ToString(_tag.RawTag.Class.Magic));
            else
                MetroMessageBox.Show("Press RB to...wait...how'd you do that?", "How did you load the plugin revision viewer before you loaded a plugin? wat.");
        }
        private void UpdateMetaButtons(bool pluginExists)
        {
            if (pluginExists)
            {
                gridSearch.Visibility               =   System.Windows.Visibility.Visible;
                cbShowInvisibles.Visibility         =   System.Windows.Visibility.Visible;
                btnPluginSave.Visibility            =   System.Windows.Visibility.Visible;

                // Only enable poking if RTE support is available
                if (_rteProvider != null)
                {
                    btnPluginPokeAll.Visibility     =   System.Windows.Visibility.Visible;
                    btnPluginPokeChanged.Visibility =   System.Windows.Visibility.Visible;
                }
                else
                {
                    btnPluginPokeAll.Visibility     =   System.Windows.Visibility.Collapsed;
                    btnPluginPokeChanged.Visibility =   System.Windows.Visibility.Collapsed;
                }

                btnPluginRevisionViewer.Visibility  =   System.Windows.Visibility.Visible;
                btnPluginRefresh.Visibility         =   System.Windows.Visibility.Visible;
            }
            else
            {
                gridSearch.Visibility               =   System.Windows.Visibility.Collapsed;

                cbShowInvisibles.Visibility         =   System.Windows.Visibility.Collapsed;
                btnPluginSave.Visibility            =   System.Windows.Visibility.Collapsed;
                btnPluginPokeAll.Visibility         =   System.Windows.Visibility.Collapsed;
                btnPluginPokeChanged.Visibility     =   System.Windows.Visibility.Collapsed;
                btnPluginRevisionViewer.Visibility  =   System.Windows.Visibility.Collapsed;

                btnPluginRefresh.Visibility         =   System.Windows.Visibility.Visible;
            }
        }
        private void UpdateMeta(MetaWriter.SaveType type, bool onlyUpdateChanged, bool showActionDialog = true)
        {
            if (type == MetaWriter.SaveType.File)
            {
                using (EndianStream stream = new EndianStream(_streamManager.OpenReadWrite(), _streamManager.SuggestedEndian))
                {
#if DEBUG_SAVE_ALL
                    MetaWriter metaUpdate = new MetaWriter(writer, _tag.RawTag.MetaLocation.AsOffset(), _cache, _buildInfo, type, null);
#else
                    MetaWriter metaUpdate = new MetaWriter(stream, _tag.RawTag.MetaLocation.AsOffset(), _cache, _buildInfo, type, _fileChanges);
#endif
                    metaUpdate.WriteFields(_pluginVisitor.Values);
                    _cache.SaveChanges(stream);
                    _fileChanges.MarkAllUnchanged();
                }

                if (showActionDialog)
                    MetroMessageBox.Show("Meta Saved", "The metadata has been saved back to the original file.");
            }
            else if (_rteProvider != null)
            {
                using (IStream metaStream = _rteProvider.GetMetaStream(_cache))
                {
                    if (metaStream != null)
                    {
                        FieldChangeSet changes = onlyUpdateChanged ? _memoryChanges : null;
                        MetaWriter metaUpdate = new MetaWriter(metaStream, _tag.RawTag.MetaLocation.AsAddress(), _cache, _buildInfo, type, changes);
                        metaUpdate.WriteFields(_pluginVisitor.Values);
                        _memoryChanges.MarkAllUnchanged();

                        if (showActionDialog)
                        {
                            if (onlyUpdateChanged)
                                MetroMessageBox.Show("Meta Poked", "All changed metadata has been poked to your Xbox 360 console.");
                            else
                                MetroMessageBox.Show("Meta Poked", "The metadata has been poked to your Xbox 360 console.");
                        }
                    }
                    else
                    {
                        switch (_rteProvider.ConnectionType)
                        {
                            case RTEConnectionType.Console:
                                MetroMessageBox.Show("Connection Error", "Unable to connect to your Xbox 360 console. Make sure that XBDM is enabled, you have the Xbox 360 SDK installed, and that your console's IP has been set correctly.");
                                break;

                            case RTEConnectionType.LocalProcess:
                                MetroMessageBox.Show("Connection Error", "Unable to connect to the game. Make sure that it is running on your computer and that the map you are poking to is currently loaded.");
                                break;
                        }
                    }
                }
            }
        }


        private void btnPluginRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshEditor();
        }

        private void btnPluginRevisionViewer_Click(object sender, RoutedEventArgs e)
        {
            RevisionViewer();
        }

        private void cbShowInvisibles_Altered(object sender, RoutedEventArgs e)
        {
            if (hasInitFinished)
            {
                Settings.pluginsShowInvisibles = (bool)cbShowInvisibles.IsChecked;
                RefreshEditor();
            }
        }

        private void cbReflexives_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                panelMetaComponents.ScrollIntoView(e.AddedItems[0]);
        }

        private void btnPluginPokeAll_Click(object sender, RoutedEventArgs e)
        {
            UpdateMeta(MetaWriter.SaveType.Memory, false);
        }
        private void btnPluginPokeChanged_Click(object sender, RoutedEventArgs e)
        {
            UpdateMeta(MetaWriter.SaveType.Memory, true);
        }
        private void btnPluginSave_Click(object sender, RoutedEventArgs e)
        {
            UpdateMeta(MetaWriter.SaveType.File, false);
        }
        
        private void metaEditor_KeyDown(object sender, KeyEventArgs e)
        {
            // Require Ctrl to be down
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                return;

            switch (e.Key)
            {
                case Key.S:
                    // Save Meta
                    UpdateMeta(MetaWriter.SaveType.File, false);
                    break;

                case Key.P:
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                    {
                        // Poke All
                        UpdateMeta(MetaWriter.SaveType.Memory, false);
                    }
                    else
                    {
                        // Poke Changed
                        UpdateMeta(MetaWriter.SaveType.Memory, true);
                    }
                    break;

                case Key.R:
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                    {
                        // Show Plugin Revision Viewer
                        RevisionViewer();
                    }
                    else
                    {
                        // Refresh Plugin
                        RefreshEditor();
                    }
                    break;
            }
        }

        #region Searching
        private void txtSearch_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Change(100, Timeout.Infinite);
        }

        // Note: called from a timer thread - make sure to go through the Dispatcher for any UI operations
        private void SearchTimer(object state)
        {
            lock (_searchTimer)
            {
                string text = null;
                Dispatcher.Invoke(new Action(delegate { text = txtSearch.Text; } ));

                if (string.IsNullOrWhiteSpace(text))
                {
                    ResetSearch();
                    return;
                }

                // Search for everything
                FilterAndHighlightMeta(text);
                Dispatcher.Invoke(new Action(delegate { comboSearchResults.ItemsSource = _searchResults; } ));

                SelectFirstResult();
                EnableResetButton(true);
            }
        }

        private void FilterAndHighlightMeta(string text)
        {
            _searchResults = new ObservableCollection<SearchResult>();
            _resultIndices.Clear();
            MetaFilterer filterer = new MetaFilterer(_flattener, MetaFilterer_CollectResult, MetaFilterer_HighlightField);
            filterer.FilterFields(_pluginVisitor.Values, text);
        }

        private void MetaFilterer_CollectResult(MetaField foundField, MetaField listField, ReflexiveData parent)
        {
            _resultIndices[listField] = _searchResults.Count;
            _searchResults.Add(new SearchResult(foundField, listField, parent));
        }

        private void MetaFilterer_HighlightField(MetaField field, bool highlight)
        {
            if (highlight)
                field.Opacity = 1f;
            else
                field.Opacity = .3f;
        }

        private void btnResetSearch_Click_1(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            txtSearch.Focus();
            _searchTimer.Change(Timeout.Infinite, Timeout.Infinite); // Freeze the timer and just do a reset immediately
            ResetSearch();
        }

        // Note: called from a timer thread - make sure to go through the Dispatcher for any UI operations
        private void ResetSearch()
        {
            _searchResults = null;
            _resultIndices.Clear();
            ShowAll();
            DisableMovementButtons();
            EnableResetButton(false);
            SelectField(null);
        }

        // Thread-safe
        private void SelectFirstResult()
        {
            if (_searchResults != null)
            {
                Dispatcher.Invoke(new Action(delegate
                    {
                        if (_searchResults.Count > 0)
                        {
                            if (panelMetaComponents.SelectedItem == null || FindResultByListField(panelMetaComponents.SelectedItem as MetaField) == -1)
                            {
                                SelectResult(_searchResults[0]);
                            }
                            else
                            {
                                panelMetaComponents.ScrollIntoView(panelMetaComponents.SelectedItem);
                                UpdateMovementControls();
                            }
                        }
                        else
                        {
                            panelMetaComponents.SelectedItem = null;
                        }
                    }
                ));
            }
        }

        /// <summary>
        /// Makes every field visible.
        /// </summary>
        private void ShowAll()
        {
            foreach (MetaField field in _pluginVisitor.Values)
                ShowField(field);
        }

        private void ShowField(MetaField field)
        {
            field.Opacity = 1.0f;

            // If the field is a reflexive, recursively set the opacity of its children
            ReflexiveData reflexive = field as ReflexiveData;
            if (reflexive != null)
            {
                foreach (MetaField child in reflexive.Template)
                    ShowField(child);

                // Also set the opacity of modified fields
                foreach (ReflexivePage page in reflexive.Pages)
                {
                    foreach (MetaField child in page.Fields)
                    {
                        if (child != null)
                            ShowField(child);
                    }
                }
            }
        }

        // Thread-safe
        private void EnableResetButton(bool enable)
        {
            Dispatcher.Invoke(new Action(delegate { btnResetSearch.IsEnabled = enable; }));
        }

        private int FindResultByListField(MetaField field)
        {
            int index;
            if (field != null && _resultIndices.TryGetValue(field, out index))
                return index;
            return -1;
        }

        // Thread-safe
        private void UpdateMovementControls()
        {
            Dispatcher.Invoke(new Action(delegate
                {
                    int resultIndex = FindResultByListField(panelMetaComponents.SelectedItem as MetaField);
                    comboSearchResults.SelectedIndex = resultIndex;

                    if (_searchResults != null)
                    {
                        comboSearchResults.IsEnabled = true;
                        btnPreviousResult.IsEnabled = (resultIndex > 0);
                        btnNextResult.IsEnabled = (resultIndex < _searchResults.Count - 1);
                    }
                    else
                    {
                        comboSearchResults.IsEnabled = false;
                        btnPreviousResult.IsEnabled = false;
                        btnNextResult.IsEnabled = false;
                    }
                }
            ));
        }

        // Thread-safe
        private void DisableMovementButtons()
        {
            Dispatcher.Invoke(new Action(delegate
                {
                    btnPreviousResult.IsEnabled = false;
                    btnNextResult.IsEnabled = false;
                    comboSearchResults.IsEnabled = false;
                }
            ));
        }

        // Thread-safe
        private void SelectField(MetaField field)
        {
            Dispatcher.Invoke(new Action(delegate
                {
                    panelMetaComponents.SelectedItem = field;
                    if (field != null)
                        panelMetaComponents.ScrollIntoView(field);
                }
            ));
        }

        // Thread-safe
        private void SelectResult(SearchResult result)
        {
            ReflexiveData reflexive = result.Reflexive;
            if (reflexive != null)
                _flattener.ForceVisible(reflexive);
            SelectField(result.ListField);
        }

        private void btnPreviousResult_Click_1(object sender, RoutedEventArgs e)
        {
            if (comboSearchResults.SelectedIndex > 0)
                SelectResult(_searchResults[comboSearchResults.SelectedIndex - 1]);
        }

        private void btnNextResult_Click_1(object sender, RoutedEventArgs e)
        {
            if (comboSearchResults.SelectedIndex < _searchResults.Count - 1)
                SelectResult(_searchResults[comboSearchResults.SelectedIndex + 1]);
        }

        private void panelMetaComponents_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (_searchResults == null)
            {
                // No select 4 u
                panelMetaComponents.SelectedItem = null;
            }
            else
            {
                MetaField field = panelMetaComponents.SelectedItem as MetaField;
                if (field != null && e.RemovedItems.Count > 0 && FindResultByListField(field) == -1)
                {
                    // Disallow selecting filtered items and reflexive wrappers
                    // as long as this wouldn't cause an infinite loop of selection changes
                    MetaField oldField = e.RemovedItems[0] as MetaField;
                    if (oldField != null && FindResultByListField(oldField) != -1)
                        panelMetaComponents.SelectedItem = oldField;
                    else
                        panelMetaComponents.SelectedItem = null;
                    return;
                }
            }

            UpdateMovementControls();
        }
        
        private void comboSearchResults_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            SearchResult selectedResult = comboSearchResults.SelectedItem as SearchResult;
            if (selectedResult != null)
                SelectResult(selectedResult);
        }
        #endregion

        private void ViewValueAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            ValueField field = GetValueField(e.Source);
            e.CanExecute = (field != null);
        }
        private void ViewValueAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ValueField field = GetValueField(e.Source);
            if (field != null)
            {
                IList<MetaField> viewValueAsFields = LoadViewValueAsPlugin();
                uint offset = _cache.MetaPointerConverter.PointerToOffset(field.FieldAddress);
                MetroViewValueAs.Show(_cache, _buildInfo, _streamManager, viewValueAsFields, offset);
            }
		}
		private void GoToPlugin_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			var field = GetValueField(e.Source);
			e.CanExecute = (field != null);
		}
		private void GoToPlugin_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var field = GetValueField(e.Source);
			if (field == null) return;

			_parentMetaContainer.GoToRawPluginLine((int)field.PluginLine);
		}


        private static MetaField GetWrappedField(MetaField field)
        {
            WrappedReflexiveEntry wrapper = null;
            while (true)
            {
                wrapper = field as WrappedReflexiveEntry;
                if (wrapper == null)
                    return field;
                field = wrapper.WrappedField;
            }
        }

        /// <summary>
        /// Given a source element, retrieves the ValueField it represents.
        /// </summary>
        /// <param name="elem">The FrameworkElement to get the ValueField for.</param>
        /// <returns>The ValueField if elem's data context is set to one, or null otherwise.</returns>
        private static ValueField GetValueField(object elem)
        {
            // Get the FrameworkElement
            FrameworkElement source = elem as FrameworkElement;
            if (source == null)
                return null;

            // Get the field, and if it's a reflexive wrapper,
            // then get the actual field it's wrapping
            ValueField field = source.DataContext as ValueField;
            if (field == null)
            {
                WrappedReflexiveEntry wrapper = source.DataContext as WrappedReflexiveEntry;
                if (wrapper != null)
                    field = GetWrappedField(wrapper) as ValueField;
            }
            return field;
        }

        /// <summary>
        /// Loads the example "view value as" plugin.
        /// </summary>
        /// <returns>The fields created from the "view value as" plugin.</returns>
        private IList<MetaField> LoadViewValueAsPlugin()
        {
            string path = string.Format("{0}\\Examples\\ThirdGenExample.xml", VariousFunctions.GetApplicationLocation() + @"Plugins");
            XmlReader reader = XmlReader.Create(path);

            ThirdGenPluginVisitor plugin = new ThirdGenPluginVisitor(_tags, true);
            AssemblyPluginLoader.LoadPlugin(reader, plugin);
            reader.Close();

            return plugin.Values;
        }
    }
}
