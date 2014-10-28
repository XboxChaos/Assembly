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
using Blamite.Blam;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.RTE;
using Blamite.Util;
using Assembly.Metro.Dialogs.ControlDialogs;
using Assembly.Helpers.TagEditor.Buffering;
using Assembly.Helpers.TagEditor;
using Assembly.Helpers.TagEditor.Fields;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	internal class SearchResult
	{
		/// <summary>
		///     Constructs a new search result holder.
		/// </summary>
		/// <param name="foundField">The field that was found and highlighted.</param>
		/// <param name="listField">
		///     The top-level field in the field list. For reflexive entries, this is the topmost wrapper
		///     field, otherwise, this may be the same as foundField.
		/// </param>
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
	///     Interaction logic for MetaEditor.xaml
	/// </summary>
	public partial class MetaEditor : UserControl
	{
		public static RoutedCommand ViewValueAsCommand = new RoutedCommand();
		public static RoutedCommand GoToPlugin = new RoutedCommand();
		private readonly EngineDescription _buildInfo;
		private readonly ICacheFile _cache;
		private readonly IStreamManager _fileManager;
		private readonly MetaContainer _parentMetaContainer;
		private readonly Dictionary<MetaField, int> _resultIndices = new Dictionary<MetaField, int>();
		private readonly IRTEProvider _rteProvider;
		private readonly Timer _searchTimer;
		private readonly Trie _stringIdTrie;
		private readonly TagHierarchy _tags;
		private readonly bool hasInitFinished;

		private FieldChangeTracker _changeTracker;
		private FieldChangeSet _fileChanges;
		private FieldChangeSet _memoryChanges;
		private string _pluginPath;
		private PluginFieldGenerator _fieldGenerator;
		private ObservableCollection<SearchResult> _searchResults;
		private TagEntry _tag;

		public MetaEditor(EngineDescription buildInfo, TagEntry tag, MetaContainer parentContainer, TagHierarchy tags,
			ICacheFile cache, IStreamManager streamManager, IRTEProvider rteProvider, Trie stringIDTrie)
		{
			InitializeComponent();

			_parentMetaContainer = parentContainer;
			_tag = tag;
			_tags = tags;
			_buildInfo = buildInfo;
			_cache = cache;
			_fileManager = streamManager;
			_rteProvider = rteProvider;
			_searchTimer = new Timer(SearchTimer);
			_stringIdTrie = stringIDTrie;

			// Load Plugin Path
			string className = VariousFunctions.SterilizeTagClassName(CharConstant.ToString(tag.RawTag.Class.Magic)).Trim();
			_pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
				_buildInfo.Settings.GetSetting<string>("plugins"), className);

			// Set Option boxes
			cbShowInvisibles.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowInvisibles;
			cbShowComments.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowComments;
			cbShowEnumIndex.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowEnumIndex;
			cbShowInformation.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowInformation;

			// Load Meta
			RefreshEditor(MetaReader.LoadType.File);

			// Set init finished
			hasInitFinished = true;
		}

		public void RefreshEditor(MetaReader.LoadType type)
		{
			if (!File.Exists(_pluginPath))
			{
				UpdateMetaButtons(false);
				StatusUpdater.Update("Plugin doesn't exist. It can't be loaded for this tag.");
				return;
			}

			// Set the stream manager and base offset to use based upon the LoadType
			IStreamManager streamManager = null;
			SegmentPointer baseLocation;
			switch (type)
			{
				case MetaReader.LoadType.File:
					streamManager = _fileManager;
					baseLocation = _tag.RawTag.MetaLocation;
					break;

				case MetaReader.LoadType.Memory:
					if (_rteProvider == null)
						goto default;

					if (_rteProvider.GetMetaStream(_cache) == null)
					{
						ShowConnectionError();
						return;
					}

					streamManager = new RTEStreamManager(_rteProvider, _cache);
					baseLocation = _tag.RawTag.MetaLocation;
					break;

				default:
					MetroMessageBox.Show("Not Supported", "That feature is not supported for this game.");
					return;
			}

			// Load Plugin File
			using (XmlReader xml = XmlReader.Create(_pluginPath))
			{
				_fieldGenerator = new PluginFieldGenerator((baseSize) => new StreamTagBufferSource(streamManager, baseLocation, (uint)baseSize));
				AssemblyPluginLoader.LoadPlugin(xml, (IPluginVisitor)_fieldGenerator);
			}

			_changeTracker = new FieldChangeTracker();
			_fileChanges = new FieldChangeSet();
			_memoryChanges = new FieldChangeSet();

			var reader = new TagDataReader();
			/*_flattener = new ReflexiveFlattener(metaReader, _changeTracker, _fileChanges);
			_flattener.Flatten(_pluginVisitor.Values);*/
			reader.ReadFields(_fieldGenerator.Fields);
			panelMetaComponents.ItemsSource = _fieldGenerator.Fields;

			// Start monitoring fields for changes
			_changeTracker.RegisterChangeSet(_fileChanges);
			_changeTracker.RegisterChangeSet(_memoryChanges);
			//_changeTracker.Attach(_pluginVisitor.Values);

			// Update Meta Toolbar
			UpdateMetaButtons(true);
		}

		private void RevisionViewer()
		{
			/*if (_pluginVisitor != null && _pluginVisitor.PluginRevisions != null)
				MetroPluginRevisionViewer.Show(_pluginVisitor.PluginRevisions, CharConstant.ToString(_tag.RawTag.Class.Magic));
			else
				MetroMessageBox.Show("Press RB to...wait...how'd you do that?",
					"How did you load the plugin revision viewer before you loaded a plugin? wat.");*/
		}

		private void UpdateMetaButtons(bool pluginExists)
		{
			if (pluginExists)
			{
				gridSearch.Visibility = Visibility.Visible;
				cbShowInvisibles.Visibility = Visibility.Visible;
				btnPluginSave.Visibility = Visibility.Visible;

				// Only enable poking if RTE support is available
				if (_rteProvider != null)
				{
					sbPluginPoke.Visibility = Visibility.Visible;
					miPluginRefreshMem.Visibility = Visibility.Visible;
				}
				else
				{
					sbPluginPoke.Visibility = Visibility.Collapsed;
					miPluginRefreshMem.Visibility = Visibility.Collapsed;
				}

				btnPluginRevisionViewer.Visibility = Visibility.Visible;
				sbPluginRefresh.Visibility = Visibility.Visible;
			}
			else
			{
				gridSearch.Visibility = Visibility.Collapsed;

				cbShowInvisibles.Visibility = Visibility.Collapsed;
				btnPluginSave.Visibility = Visibility.Collapsed;
				sbPluginPoke.Visibility = Visibility.Collapsed;
				btnPluginRevisionViewer.Visibility = Visibility.Collapsed;
				miPluginRefreshMem.Visibility = Visibility.Collapsed;

				sbPluginRefresh.Visibility = Visibility.Visible;
			}
		}

		private void UpdateMeta(MetaWriter.SaveType type, bool onlyUpdateChanged, bool showActionDialog = true)
		{
			/*if (type == MetaWriter.SaveType.File)
			{
				if (!ConfirmNewStringIds())
					return;

				using (IStream stream = _fileManager.OpenReadWrite())
				{
#if DEBUG_SAVE_ALL
                    MetaWriter metaUpdate = new MetaWriter(writer, (uint)_tag.RawTag.MetaLocation.AsOffset(), _cache, _buildInfo, type, null, _stringIdTrie);
#else
					var metaUpdate = new MetaWriter(stream, (uint) _tag.RawTag.MetaLocation.AsOffset(), _cache, _buildInfo, type,
						_fileChanges, _stringIdTrie);
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
						var metaUpdate = new MetaWriter(metaStream, _tag.RawTag.MetaLocation.AsPointer(), _cache, _buildInfo, type,
							changes, _stringIdTrie);
						metaUpdate.WriteFields(_pluginVisitor.Values);

						if (showActionDialog)
						{
							if (onlyUpdateChanged)
								MetroMessageBox.Show("Meta Poked", "All changed metadata has been poked to the game.");
							else
								MetroMessageBox.Show("Meta Poked", "The metadata has been poked to the game.");
						}
					}
					else
					{
						ShowConnectionError();
					}
				}
			}*/
		}

		private void ShowConnectionError()
		{
			switch (_rteProvider.ConnectionType)
			{
				case RTEConnectionType.ConsoleX360:
					MetroMessageBox.Show("Connection Error",
						"Unable to connect to your Xbox 360 console. Make sure that XBDM is enabled, you have the Xbox 360 SDK installed, and that your console's IP has been set correctly.");
					break;

				case RTEConnectionType.LocalProcess:
					MetroMessageBox.Show("Connection Error",
						"Unable to connect to the game. Make sure that it is running on your computer and that the map you are poking to is currently loaded.");
					break;
			}
		}

		public void LoadNewTagEntry(TagEntry tag)
		{
			_tag = tag;

			// Load Plugin Path
			string className = VariousFunctions.SterilizeTagClassName(CharConstant.ToString(_tag.RawTag.Class.Magic)).Trim();
			_pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
				_buildInfo.Settings.GetSetting<string>("plugins"), className);

			// Set Option boxes
			cbShowInvisibles.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowInvisibles;
			cbShowComments.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowComments;
			cbShowEnumIndex.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowEnumIndex;
			cbShowInformation.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowInformation;

			// Load Meta
			RefreshEditor(MetaReader.LoadType.File);
		}

		private void btnPluginRefresh_Click(object sender, RoutedEventArgs e)
		{
			RefreshEditor(MetaReader.LoadType.File);
			sbPluginRefresh.IsOpen = false;
		}

		private void btnPluginRefreshFromMemory_Click(object sender, RoutedEventArgs e)
		{
			RefreshEditor(MetaReader.LoadType.Memory);
			sbPluginRefresh.IsOpen = false;
		}

		private void btnPluginRevisionViewer_Click(object sender, RoutedEventArgs e)
		{
			RevisionViewer();
		}

	//	private void btnOptions_Click(object sender, RoutedEventArgs e)
	//	{
	//		popOptions.IsOpen = true;
	//	}

		private void cbShowInvisibles_Altered(object sender, RoutedEventArgs e)
		{
			if (!hasInitFinished) return;

			
				App.AssemblyStorage.AssemblySettings.PluginsShowInvisibles = (bool) cbShowInvisibles.IsChecked;
			RefreshEditor(MetaReader.LoadType.File);
			btnOptions.IsChecked = false;
		}

		private void cbShowComments_Altered(object sender, RoutedEventArgs e)
		{
			if (!hasInitFinished) return;


			App.AssemblyStorage.AssemblySettings.PluginsShowComments = (bool)cbShowComments.IsChecked;
			RefreshEditor(MetaReader.LoadType.File);
			btnOptions.IsChecked = false;
		}

		private void cbShowEnumIndex_Altered(object sender, RoutedEventArgs e)
		{
			if (!hasInitFinished) return;


			App.AssemblyStorage.AssemblySettings.PluginsShowEnumIndex = (bool)cbShowEnumIndex.IsChecked;
			btnOptions.IsChecked = false;
		}

		private void cbShowInformation_Altered(object sender, RoutedEventArgs e)
		{
			if (!hasInitFinished) return;


			App.AssemblyStorage.AssemblySettings.PluginsShowInformation = (bool)cbShowInformation.IsChecked;
			btnOptions.IsChecked = false;
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
					else if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
					{
						// Refresh Plugin
						RefreshEditor(MetaReader.LoadType.Memory);
					}
					else
					{
						// Refresh Plugin
						RefreshEditor(MetaReader.LoadType.File);
					}
					break;
			}
		}

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
				var offset = (uint) _cache.MetaArea.PointerToOffset(field.FieldAddress);
				MetroViewValueAs.Show(_cache, _buildInfo, _fileManager, viewValueAsFields, offset);
			}
		}

		private void GoToPlugin_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			MetaField field = GetWrappedField(e.Source);
			e.CanExecute = (field != null && field.PluginLine > 0);
		}

		private void GoToPlugin_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			MetaField field = GetWrappedField(e.Source);
			if (field == null) return;

			_parentMetaContainer.GoToRawPluginLine((int) field.PluginLine);
		}

		private static MetaField GetWrappedField(MetaField field)
		{
			/*WrappedReflexiveEntry wrapper = null;
			while (true)
			{
				wrapper = field as WrappedReflexiveEntry;
				if (wrapper == null)
					return field;
				field = wrapper.WrappedField;
			}*/
			return field;
		}

		private static MetaField GetWrappedField(object elem)
		{
			// Get the FrameworkElement
			var source = elem as FrameworkElement;
			if (source == null)
				return null;

			// Get the field
			var field = source.DataContext as MetaField;
			if (field == null)
				return null;

			return GetWrappedField(field);
		}

		/// <summary>
		///     Given a source element, retrieves the ValueField it represents.
		/// </summary>
		/// <param name="elem">The FrameworkElement to get the ValueField for.</param>
		/// <returns>The ValueField if elem's data context is set to one, or null otherwise.</returns>
		private static ValueField GetValueField(object elem)
		{
			MetaField field = GetWrappedField(elem);
			var valueField = field as ValueField;
			/*if (valueField == null)
			{
				var wrapper = field as WrappedReflexiveEntry;
				if (wrapper != null)
					valueField = GetWrappedField(wrapper) as ValueField;
			}*/
			return valueField;
		}

		/// <summary>
		///     Loads the example "view value as" plugin.
		/// </summary>
		/// <returns>The fields created from the "view value as" plugin.</returns>
		private IList<MetaField> LoadViewValueAsPlugin()
		{
			string path = string.Format("{0}\\Examples\\ThirdGenExample.xml",
				VariousFunctions.GetApplicationLocation() + @"Plugins");
			XmlReader reader = XmlReader.Create(path);

			var plugin = new ThirdGenPluginVisitor(_tags, _stringIdTrie, _cache.MetaArea, true);
			AssemblyPluginLoader.LoadPlugin(reader, plugin);
			reader.Close();

			return plugin.Values;
		}

		private bool ConfirmNewStringIds()
		{
			var newStrings = new List<string>();
			foreach (MetaField field in _fileChanges)
			{
				var stringIdField = field as StringIDData;
				if (stringIdField != null)
				{
					if (!_stringIdTrie.Contains(stringIdField.Value))
						newStrings.Add(stringIdField.Value);
				}
			}
			if (newStrings.Count > 0)
				return MetroMessageBoxList.Show("New StringIDs",
					"The following stringID(s) do not currently exist in the cache file and will be added.\r\nContinue?", newStrings);
			return true;
		}

		private void EditTagBlockCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void EditTagBlockCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var dialog = new TagBlockEditor((ReflexiveData)((FrameworkElement)e.OriginalSource).DataContext, null);
			dialog.ShowDialog();
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
				Dispatcher.Invoke(new Action(delegate { text = txtSearch.Text; }));

				if (string.IsNullOrWhiteSpace(text))
				{
					ResetSearch();
					return;
				}

				// Search for everything
				FilterAndHighlightMeta(text);
				Dispatcher.Invoke(new Action(delegate { comboSearchResults.ItemsSource = _searchResults; }));

				SelectFirstResult();
				EnableResetButton(true);
			}
		}

		private void FilterAndHighlightMeta(string text)
		{
			/*_searchResults = new ObservableCollection<SearchResult>();
			_resultIndices.Clear();
			var filterer = new MetaFilterer(MetaFilterer_CollectResult, MetaFilterer_HighlightField);
			filterer.FilterFields(_pluginVisitor.Values, text);*/
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
						if (panelMetaComponents.SelectedItem == null ||
						    FindResultByListField(panelMetaComponents.SelectedItem as MetaField) == -1)
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
		///     Makes every field visible.
		/// </summary>
		private void ShowAll()
		{
			foreach (var field in _fieldGenerator.Fields)
				ShowField(field);
		}

		private void ShowField(TagField field)
		{
			field.Opacity = 1.0f;

			// If the field is a reflexive, recursively set the opacity of its children
			/*var reflexive = field as ReflexiveData;
			if (reflexive != null)
			{
				// Show wrappers
				_flattener.EnumWrappers(reflexive, ShowField);

				// Show template fields
				foreach (MetaField child in reflexive.Template)
					ShowField(child);

				// Show modified fields
				foreach (ReflexivePage page in reflexive.Pages)
				{
					foreach (MetaField child in page.Fields)
					{
						if (child != null)
							ShowField(child);
					}
				}
			}*/
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
			/*ReflexiveData reflexive = result.Reflexive;
			if (reflexive != null)
				_flattener.ForceVisible(reflexive);*/
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
				var field = panelMetaComponents.SelectedItem as MetaField;
				if (field != null && e.RemovedItems.Count > 0 && FindResultByListField(field) == -1)
				{
					// Disallow selecting filtered items and reflexive wrappers
					// as long as this wouldn't cause an infinite loop of selection changes
					var oldField = e.RemovedItems[0] as MetaField;
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
			var selectedResult = comboSearchResults.SelectedItem as SearchResult;
			if (selectedResult != null)
				SelectResult(selectedResult);
		}

		#endregion
	}
}