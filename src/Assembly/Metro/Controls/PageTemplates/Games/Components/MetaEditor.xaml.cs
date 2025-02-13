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
using System.CodeDom.Compiler;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	internal class SearchResult
	{
		/// <summary>
		///     Constructs a new search result holder.
		/// </summary>
		/// <param name="foundField">The field that was found and highlighted.</param>
		/// <param name="listField">
		///     The top-level field in the field list. For tag block elements, this is the topmost wrapper
		///     field, otherwise, this may be the same as foundField.
		/// </param>
		/// <param name="parent">The block that the field is in. Can be null.</param>
		public SearchResult(MetaField foundField, MetaField listField, TagBlockData parent)
		{
			ListField = listField;
			Field = foundField;
			TagBlock = parent;
		}

		public MetaField Field { get; private set; }
		public MetaField ListField { get; private set; }
		public TagBlockData TagBlock { get; private set; }
	}

	/// <summary>
	///     Interaction logic for MetaEditor.xaml
	/// </summary>
	public partial class MetaEditor : UserControl
	{
		public static RoutedCommand ViewValueAsCommand = new RoutedCommand();
		public static RoutedCommand ContentViewValueAsCommand = new RoutedCommand();
		public static RoutedCommand GoToPlugin = new RoutedCommand();
		private readonly EngineDescription _buildInfo;
		private readonly ICacheFile _cache;
		private readonly IStreamManager _fileManager;
		private readonly MetaContainer _parentMetaContainer;
		private readonly Dictionary<MetaField, int> _resultIndices = new Dictionary<MetaField, int>();
		private readonly RTEProvider _rteProvider;
		private readonly Timer _searchTimer;
		private readonly Trie _stringIdTrie;
		private readonly TagHierarchy _tags;
		private readonly bool hasInitFinished;

		private FieldChangeTracker _changeTracker;
		private FieldChangeSet _fileChanges;
		private TagBlockFlattener _flattener;
		private FieldChangeSet _memoryChanges;
		private string _pluginPath;
		private AssemblyPluginVisitor _pluginVisitor;
		private ObservableCollection<SearchResult> _searchResults;
		private TagEntry _tag;
		private FileSegmentGroup _srcSegmentGroup;
		private TagDataCommandState _tagCommandState;

		public MetaEditor(EngineDescription buildInfo, TagEntry tag, MetaContainer parentContainer, TagHierarchy tags,
			ICacheFile cache, IStreamManager streamManager, RTEProvider rteProvider, Trie stringIDTrie)
		{
			InitializeComponent();

			_parentMetaContainer = parentContainer;
			_tags = tags;
			_buildInfo = buildInfo;
			_cache = cache;
			_fileManager = streamManager;
			_rteProvider = rteProvider;
			_searchTimer = new Timer(SearchTimer);
			_stringIdTrie = stringIDTrie;
			_srcSegmentGroup = tag.RawTag.MetaLocation.BaseGroup;

			LoadNewTagEntry(tag);

			// Set init finished
			hasInitFinished = true;
		}

		private TagDataCommandState CheckTagDataCommand()
		{
			if (_cache.Engine == EngineType.Eldorado)
				return TagDataCommandState.Eldorado;
			else if (_tag.RawTag.Source != TagSource.MetaArea)
				return TagDataCommandState.NotMainArea;
			else if (_cache.Engine < EngineType.SecondGeneration)
				return TagDataCommandState.FirstGenCache;
			else if (_cache.Engine == EngineType.ThirdGeneration && _cache.HeaderSize == 0x800)
				return TagDataCommandState.EarlyThirdGenCache;
			else
				return TagDataCommandState.None;
		}

		public void RefreshEditor(MetaReader.LoadType type)
		{
			// Load Plugin Path
			string groupName = VariousFunctions.SterilizeTagGroupName(CharConstant.ToString(_tag.RawTag.Group.Magic)).Trim();
			_pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
				_buildInfo.Settings.GetSetting<string>("plugins"), groupName);

			if (!File.Exists(_pluginPath) && _buildInfo.Settings.PathExists("fallbackPlugins"))
				_pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
					_buildInfo.Settings.GetSetting<string>("fallbackPlugins"), groupName);

			if (_pluginPath == null || !File.Exists(_pluginPath))
			{
				UpdateMetaButtons(false);
				StatusUpdater.Update("Plugin doesn't exist. It can't be loaded for this tag.");
				return;
			}

			// Store the current search selection so it can be restored
			int searchSelectedItem = comboSearchResults.SelectedIndex;

			// Set the stream manager and base offset to use based upon the LoadType
			IStreamManager streamManager = null;
			long baseOffset = 0;
			switch (type)
			{
				case MetaReader.LoadType.File:
					streamManager = _fileManager;
					baseOffset = (uint) _tag.RawTag.MetaLocation.AsOffset();
					break;

				case MetaReader.LoadType.Memory:
					if (_rteProvider == null)
						goto default;

					if (_rteProvider.GetCacheStream(_cache, _tag.RawTag) == null)
					{
						ShowConnectionError();
						return;
					}

					streamManager = new RTEStreamManager(_rteProvider, _cache, _tag.RawTag);
					baseOffset = _tag.RawTag.MetaLocation.AsPointer();
					break;

				default:
					MetroMessageBox.Show("Not Supported", "That feature is not supported for this game.");
					return;
			}

			// Load Plugin File
			using (XmlReader xml = XmlReader.Create(_pluginPath))
			{
				_pluginVisitor = new AssemblyPluginVisitor(_tags, _stringIdTrie, _srcSegmentGroup,
					App.AssemblyStorage.AssemblySettings.PluginsShowInvisibles, _tagCommandState, _buildInfo.Engine < EngineType.ThirdGeneration);
				AssemblyPluginLoader.LoadPlugin(xml, _pluginVisitor);
			}

			_changeTracker = new FieldChangeTracker();
			_fileChanges = new FieldChangeSet();
			_memoryChanges = new FieldChangeSet();

			var metaReader = new MetaReader(streamManager, baseOffset, _cache, _buildInfo, type, _fileChanges, _srcSegmentGroup);
			_flattener = new TagBlockFlattener(metaReader, _changeTracker, _fileChanges);
			_flattener.Flatten(_pluginVisitor.Values);
			metaReader.ReadFields(_pluginVisitor.Values);
			panelMetaComponents.ItemsSource = _pluginVisitor.Values;

			// Start monitoring fields for changes
			_changeTracker.RegisterChangeSet(_fileChanges);
			_changeTracker.RegisterChangeSet(_memoryChanges);
			_changeTracker.Attach(_pluginVisitor.Values);

			// Update Meta Toolbar
			UpdateMetaButtons(true);

			// Refresh search if needed
			if (searchSelectedItem != -1)
			{
				SearchTimer(null);
				if (searchSelectedItem <= (comboSearchResults.Items.Count - 1))
					comboSearchResults.SelectedIndex = searchSelectedItem;
			}
		}

		private void RevisionViewer()
		{
			if (_pluginVisitor != null && _pluginVisitor.PluginRevisions != null)
				MetroPluginRevisionViewer.Show(_pluginVisitor.PluginRevisions, CharConstant.ToString(_tag.RawTag.Group.Magic));
			else
				MetroMessageBox.Show("Press RB to...wait...how'd you do that?",
					"How did you load the plugin revision viewer before you loaded a plugin? wat.");
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
			if (type == MetaWriter.SaveType.File)
			{
				if (!ConfirmNewStringIds())
					return;

				using (IStream stream = _fileManager.OpenReadWrite())
				{
#if DEBUG_SAVE_ALL
                    MetaWriter metaUpdate = new MetaWriter(writer, _tag.RawTag.MetaLocation.AsOffset(), _cache, _buildInfo, type, null, _stringIdTrie);
#else
					var metaUpdate = new MetaWriter(stream, _tag.RawTag.MetaLocation.AsOffset(), _cache, _buildInfo, type,
						_fileChanges, _stringIdTrie, _srcSegmentGroup);
#endif
					metaUpdate.WriteFields(_pluginVisitor.Values);
					_cache.SaveChanges(stream);
					_fileChanges.MarkAllUnchanged();
				}

				if (showActionDialog)
					MetroMessageBox.Show("Tag Saved", "The changes have been saved back to the original file." +
						(_buildInfo.Compression != Blamite.Compression.CompressionType.None ? "\r\n\r\nNote: This file may need to be compressed from the Tools menu before attempting to load ingame." : ""));
			}
			else if (_rteProvider != null)
			{
				var rteProvider = _rteProvider;
				if (App.AssemblyStorage.AssemblyNetworkPoke.NetworkRteProvider != null)
				{
					rteProvider = App.AssemblyStorage.AssemblyNetworkPoke.NetworkRteProvider;
				}

				using (IStream metaStream = rteProvider.GetCacheStream(_cache, _tag.RawTag))
				{
					if (metaStream != null)
					{
						FieldChangeSet changes = onlyUpdateChanged ? _memoryChanges : null;
						var metaUpdate = new MetaWriter(metaStream, _tag.RawTag.MetaLocation.AsPointer(), _cache, _buildInfo, type,
							changes, _stringIdTrie, _srcSegmentGroup);
						metaUpdate.WriteFields(_pluginVisitor.Values);

						if (showActionDialog)
						{
							if (onlyUpdateChanged)
								StatusUpdater.Update("All changed fields have been poked to the game.");
							else
								StatusUpdater.Update("The changes have been poked to the game.");
						}
					}
					else
					{
						ShowConnectionError();
					}
				}
			}
		}

		private void ShowConnectionError()
		{
			switch (_rteProvider.ConnectionType)
			{
				case RTEConnectionType.ConsoleXbox:
					MetroMessageBox.Show("Connection Error",
						"Unable to connect to your Xbox console. Make sure that XBDM is enabled and that your console's IP has been set correctly.");
					break;

				case RTEConnectionType.ConsoleXbox360:
					MetroMessageBox.Show("Connection Error",
						"Unable to connect to your Xbox 360 console. Make sure that XBDM is enabled and that your console's IP has been set correctly.");
					break;

				case RTEConnectionType.LocalProcess32:
				case RTEConnectionType.LocalProcess64:
					MetroMessageBox.Show("Connection Error",
						_rteProvider.ErrorMessage);
					break;
			}
		}

		public void LoadNewTagEntry(TagEntry tag)
		{
			_tag = tag;
			_tagCommandState = CheckTagDataCommand();

			// Set Option boxes
			cbShowInvisibles.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowInvisibles;
			cbShowComments.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowComments;
			cbShowInformation.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowInformation;
			cbShowDataRefNotice.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowDataRefNotice;

			cbEnumPrefix.SelectedIndex = (int)App.AssemblyStorage.AssemblySettings.PluginsEnumPrefix;

			// Load Meta
			RefreshEditor(MetaReader.LoadType.File);

			// Load Info
			lblTagName.Text = tag.TagFileName != null
				? tag.TagFileName + "." + tag.GroupName
				: "0x" + tag.RawTag.Index.Value.ToString("X");

			lblDatum.Text = string.Format("{0}", tag.RawTag.Index);
			lblAddress.Text = string.Format("0x{0:X8}", tag.RawTag.MetaLocation.AsPointer());
			lblOffset.Text = string.Format("0x{0:X}", tag.RawTag.MetaLocation.AsOffset());
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

		private void cbEnumPrefix_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!hasInitFinished || cbEnumPrefix == null || cbEnumPrefix.SelectedIndex < 0) return;

			App.AssemblyStorage.AssemblySettings.PluginsEnumPrefix = (Settings.EnumPrefix)cbEnumPrefix.SelectedIndex;
		}

		private void cbShowInformation_Altered(object sender, RoutedEventArgs e)
		{
			if (!hasInitFinished) return;


			App.AssemblyStorage.AssemblySettings.PluginsShowInformation = (bool)cbShowInformation.IsChecked;
			btnOptions.IsChecked = false;
		}

		private void cbShowDataRefNotice_Altered(object sender, RoutedEventArgs e)
		{
			if (!hasInitFinished) return;

			App.AssemblyStorage.AssemblySettings.PluginsShowDataRefNotice = (bool)cbShowDataRefNotice.IsChecked;
			btnOptions.IsChecked = false;
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
					btnPluginSave.Focus();
					UpdateMeta(MetaWriter.SaveType.File, false);
					break;

				case Key.P:
					sbPluginPoke.Focus();
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

				case Key.F:
					txtSearch.Focus();
					break;
			}

			e.Handled = true;
		}

		private void btnDumpTag_Click(object sender, RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog
			{
				Title = "Save Tag Dump",
				Filter = "JSON Files|*.json|Text Files|*.txt"
			};
			bool? result = sfd.ShowDialog();
			if (!result.Value)
				return;

			HandleDump(sfd.FileName, sfd.FilterIndex);
			MetroMessageBox.Show("Tag dumped!");
		}

		private void HandleDump(string output, int filterIndex)
		{
			using (StringWriter sw = new StringWriter())
			{
				MetaField[] fields = new MetaField[panelMetaComponents.Items.Count];
				panelMetaComponents.Items.CopyTo(fields, 0);

				if (filterIndex == 1)
				{
					using (JsonWriter writer = new JsonTextWriter(sw))
					{
						writer.Formatting = Newtonsoft.Json.Formatting.Indented;
						writer.WriteStartObject();
						writer.WritePropertyName("TagName");
						writer.WriteValue(_tag.TagFileName);

						writer.WritePropertyName("Data");
						DumpFieldsToJSON(writer, fields);

						writer.WriteEndObject();
					}
				}
				else if (filterIndex == 2)
				{
					IndentedTextWriter itw = new IndentedTextWriter(sw);
					itw.WriteLine(_tag.TagFileName);
					itw.WriteLine();

					DumpFieldsToText(itw, fields);
				}
				else return;

				Directory.CreateDirectory(Path.GetDirectoryName(output));
				File.WriteAllText(output, sw.ToString());
			}
		}

		private void WriteJSONContent(JsonWriter writer, object content)
		{
			if (content is IDictionary<string, object>)
			{
				writer.WriteStartObject();
				foreach (KeyValuePair<string, object> entry in content as IDictionary<string, object>)
				{
					writer.WritePropertyName(entry.Key);
					WriteJSONContent(writer, entry.Value);
				}
				writer.WriteEndObject();
			}
			else if (content is IList<string>)
			{
				writer.WriteStartArray();
				foreach (string str in content as List<string>)
					writer.WriteValue(str);
				writer.WriteEndArray();
			}
			else if (content is IList<object>)
			{
				writer.WriteStartArray();
				foreach (object val in content as List<object>)
					WriteJSONContent(writer, val);
				writer.WriteEndArray();
			}
			else
				writer.WriteValue(content);
		}

		private void DumpFieldsToJSON(JsonWriter writer, MetaField[] fields)
		{
			writer.WriteStartObject();
			foreach (MetaField field in fields)
			{
				if (field is TagBlockData)
				{
					TagBlockData block = field as TagBlockData;
					block.ResetPages();
					int oldIndex = block.CurrentIndex;
					writer.WritePropertyName(block.Name);
					writer.WriteStartArray();
					for (int i = 0; i < block.Length; i++)
					{
						block.CurrentIndex = i;

						MetaField[] subFields = new MetaField[block.Template.Count];
						block.Template.CopyTo(subFields, 0);
						DumpFieldsToJSON(writer, subFields);

					}
					block.CurrentIndex = oldIndex;
					writer.WriteEnd();
				}
				else if (field is ValueField)
				{
					if (field is RawData)
					{
						RawData raw = field as RawData;
						raw.ShowingNotice = false;
					}

					ValueField valueField = field as ValueField;
					writer.WritePropertyName(valueField.Name);
					WriteJSONContent(writer, valueField.GetAsJson());
				}
			}
			writer.WriteEndObject();
		}

		private void DumpFieldsToText(IndentedTextWriter itw, MetaField[] fields)
		{
			foreach (MetaField field in fields)
			{
				//have to handle blocks here
				if (field is TagBlockData)
				{
					TagBlockData block = field as TagBlockData;
					block.ResetPages();

					itw.WriteLine(field.AsString());
					itw.Indent++;
					itw.WriteLine("{");
					int oldIndex = block.CurrentIndex;
					for (int i = 0; i < block.Length; i++)
					{
						itw.WriteLine("[element " + i + "]");
						block.CurrentIndex = i;

						MetaField[] subFields = new MetaField[block.Template.Count];
						block.Template.CopyTo(subFields, 0);
						DumpFieldsToText(itw, subFields);
						itw.WriteLineNoTabs("");
					}
					block.CurrentIndex = oldIndex;
					itw.WriteLine("}");
					itw.Indent--;
				}
				else if (!(field is WrappedTagBlockEntry))
				{
					if (field is RawData)
					{
						RawData raw = field as RawData;
						raw.ShowingNotice = false;
					}

					itw.WriteLine(field.AsString());
				}
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
				var offset = _srcSegmentGroup.PointerToOffset(field.FieldAddress);
				MetroViewValueAs.Show(_cache, _buildInfo, _fileManager, viewValueAsFields, offset, _tag);
			}
		}

		private void ContentViewValueAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			ValueField field = GetValueField(e.Source);
			e.CanExecute = false;
			if (field != null)
			{
				Type type = field.GetType();
				if (type == typeof(TagBlockData) ||
					type == typeof(DataRef))
					e.CanExecute = true;
			}
		}

		private void ContentViewValueAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ValueField field = GetValueField(e.Source);
			if (field != null)
			{
				IList<MetaField> viewValueAsFields = LoadViewValueAsPlugin();
				Type type = field.GetType();
				long address;

				if (type == typeof(TagBlockData))
				{
					address = ((TagBlockData)field).FirstElementAddress;
				}
				else if (type == typeof(DataRef))
				{
					address = ((DataRef)field).DataAddress;
				}
				else
				{
					// this should never occur but might as well default to the field address
					address = field.FieldAddress;
				}

				if (!_srcSegmentGroup.ContainsPointer(address))
				{
					MetroMessageBox.Show("View Value As", "This field has an invalid address. Cannot open a View Value As window for it.");
					return;
				}

				var offset = _srcSegmentGroup.PointerToOffset(address);
				MetroViewValueAs.Show(_cache, _buildInfo, _fileManager, viewValueAsFields, offset, _tag);
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
			WrappedTagBlockEntry wrapper = null;
			while (true)
			{
				wrapper = field as WrappedTagBlockEntry;
				if (wrapper == null)
					return field;
				field = wrapper.WrappedField;
			}
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
			if (valueField == null)
			{
				var wrapper = field as WrappedTagBlockEntry;
				if (wrapper != null)
					valueField = GetWrappedField(wrapper) as ValueField;
			}
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

			var plugin = new AssemblyPluginVisitor(_tags, _stringIdTrie, _srcSegmentGroup, true, _tagCommandState, _buildInfo.Engine < EngineType.ThirdGeneration, true);
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
				EnableResetButton(Visibility.Visible);
			}
		}

		private void FilterAndHighlightMeta(string text)
		{
			_searchResults = new ObservableCollection<SearchResult>();
			_resultIndices.Clear();
			var filterer = new MetaFilterer(_flattener, MetaFilterer_CollectResult, MetaFilterer_HighlightField);
			filterer.FilterFields(_pluginVisitor.Values, text);
		}

		private void MetaFilterer_CollectResult(MetaField foundField, MetaField listField, TagBlockData parent)
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
			EnableResetButton(Visibility.Hidden);
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
			foreach (MetaField field in _pluginVisitor.Values)
				ShowField(field);
		}

		private void ShowField(MetaField field)
		{
			field.Opacity = 1.0f;

			// If the field is a block, recursively set the opacity of its children
			var block = field as TagBlockData;
			if (block != null)
			{
				// Show wrappers
				_flattener.EnumWrappers(block, ShowField);

				// Show template fields
				foreach (MetaField child in block.Template)
					ShowField(child);

				// Show modified fields
				foreach (TagBlockPage page in block.Pages)
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
		private void EnableResetButton(Visibility visibility)
		{
			Dispatcher.Invoke(new Action(delegate { btnResetSearch.Visibility = visibility; }));
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
			TagBlockData block = result.TagBlock;
			if (block != null)
				_flattener.ForceVisible(block);
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
					// Disallow selecting filtered items and tag block wrappers
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

		private void ReallocateBlockCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _tagCommandState == TagDataCommandState.None;
		}

		private void ReallocateBlockCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (_tagCommandState != TagDataCommandState.None)
			{
				MetroMessageBox.Show("Tag Block Reallocator", TagDataCommandStateResolver.GetStateDescription(_tagCommandState));
				return;
			}

			var field = GetWrappedField(e.OriginalSource) as TagBlockData;
			if (field == null)
				return;
			var oldCount = field.Length;
			var newCount = MetroTagBlockReallocator.Show(_cache, field);
			if (newCount == null || newCount == oldCount)
				return; // Canceled

			var oldAddress = field.FirstElementAddress;
			var oldSize = field.Length * field.ElementSize;
			var newSize = (int)newCount * field.ElementSize;
			long newAddress;
			using (var stream = _fileManager.OpenReadWrite())
			{
				// Reallocate the block
				newAddress = _cache.Allocator.Reallocate(oldAddress, (uint)oldSize, (uint)newSize, (uint)field.Align, stream);
				_cache.SaveChanges(stream);

				// If the block was made larger, zero extra data and null tagrefs
				if (newAddress != 0 && newSize > oldSize)
				{
					stream.SeekTo(_srcSegmentGroup.PointerToOffset(newAddress) + oldSize);
					StreamUtil.Fill(stream, 0, (uint)(newSize - oldSize));

					var tagRefLayout = _buildInfo.Layouts.GetLayout("tag reference");
					var groupOffset = tagRefLayout.GetFieldOffset("tag group magic");
					var datumOffset = tagRefLayout.GetFieldOffset("datum index");

					//go through each new page and write null for any tagrefs
					for (int i = oldCount; i < newCount; i++)
					{
						var entryStart = _srcSegmentGroup.PointerToOffset(newAddress) + (field.ElementSize * i);
						foreach (MetaField mf in field.Template)
						{
							if (mf.GetType() != typeof(TagRefData))
								continue;
							var tagref = (TagRefData)mf;
							if (tagref.WithGroup)
							{
								stream.SeekTo(entryStart + tagref.Offset + groupOffset);
								stream.WriteInt32(-1);
								stream.SeekTo(entryStart + tagref.Offset + datumOffset);
								stream.WriteInt32(-1);
							}
							else
							{
								//no group, write to field offset without adding anything
								stream.SeekTo(entryStart + tagref.Offset);
								stream.WriteInt32(-1);
							}
						}
					}
				}
			}

			// Changing these causes a read from the file, so the stream has to be closed first
			field.Length = (int)newCount;
			field.FirstElementAddress = newAddress;

			using (var stream = _fileManager.OpenReadWrite())
			{
				// Force a save back to the file
				var changes = new FieldChangeSet();
				changes.MarkChanged(field);
				var metaUpdate = new MetaWriter(stream, _tag.RawTag.MetaLocation.AsOffset(), _cache, _buildInfo,
					MetaWriter.SaveType.File, changes, _stringIdTrie, _srcSegmentGroup);
				metaUpdate.WriteFields(_pluginVisitor.Values);
				_fileChanges.MarkUnchanged(field);

				_cache.SaveChanges(stream);
			}
			if (newAddress == oldAddress)
			{
				MetroMessageBox.Show("Tag Block Reallocator - Assembly",
					"The tag block was resized successfully. Its address did not change.");
			}
			else if (oldAddress == 0)
			{
				MetroMessageBox.Show("Tag Block Reallocator - Assembly",
					"The tag block was allocated successfully. Its address is 0x" + newAddress.ToString("X8") + ".");
			}
			else if (newAddress != 0)
			{
				MetroMessageBox.Show("Tag Block Reallocator - Assembly",
					"The tag block was reallocated successfully. Its new address is 0x" + newAddress.ToString("X8") + ".");
			}
			else
			{
				MetroMessageBox.Show("Tag Block Reallocator - Assembly",
					"The tag block was freed successfully.");
			}
		}

		private void IsolateBlockCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _tagCommandState == TagDataCommandState.None;
		}

		private void IsolateBlockCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (_tagCommandState != TagDataCommandState.None)
			{
				MetroMessageBox.Show("Tag Block Isolation", TagDataCommandStateResolver.GetStateDescription(_tagCommandState));
				return;
			}

			var field = GetWrappedField(e.OriginalSource) as TagBlockData;
			if (field == null)
				return;

			var oldAddress = field.FirstElementAddress;
			int size = field.Length * (int)field.ElementSize;
			if (oldAddress == 0 || size == 0)
			{
				MetroMessageBox.Show("Tag Block Isolation", "Cannot isolate: block is null.");
				return;
			}

			var result = MetroMessageBox.Show("Tag Block Isolation", "Are you sure you want to copy this block to a new location?", MetroMessageBox.MessageBoxButtons.OkCancel);
			if (result != MetroMessageBox.MessageBoxResult.OK)
				return;

			long newAddress;
			using (var stream = _fileManager.OpenReadWrite())
			{
				// Reallocate the block
				newAddress = _cache.Allocator.Allocate((uint)size, stream);
				_cache.SaveChanges(stream);

				//copy data
				stream.SeekTo(_srcSegmentGroup.PointerToOffset(oldAddress));
				var data = stream.ReadBlock(size);

				stream.SeekTo(_srcSegmentGroup.PointerToOffset(newAddress));
				stream.WriteBlock(data);
			}

			// Changing these causes a read from the file, so the stream has to be closed first
			field.FirstElementAddress = newAddress;

			using (var stream = _fileManager.OpenReadWrite())
			{
				// Force a save back to the file
				var changes = new FieldChangeSet();
				changes.MarkChanged(field);
				var metaUpdate = new MetaWriter(stream, _tag.RawTag.MetaLocation.AsOffset(), _cache, _buildInfo,
					MetaWriter.SaveType.File, changes, _stringIdTrie, _srcSegmentGroup);
				metaUpdate.WriteFields(_pluginVisitor.Values);
				_fileChanges.MarkUnchanged(field);

				_cache.SaveChanges(stream);
			}
			MetroMessageBox.Show("Tag Block Isolation - Assembly",
					"The tag block was isolated successfully.");
		}

		private void AllocateDataRefCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _tagCommandState == TagDataCommandState.None;
		}

		private void AllocateDataRefCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (_tagCommandState != TagDataCommandState.None)
			{
				MetroMessageBox.Show("Data Reference Allocator", TagDataCommandStateResolver.GetStateDescription(_tagCommandState));
				return;
			}

			var field = GetWrappedField(e.OriginalSource) as DataRef;
			if (field == null)
				return;
			var oldLength = field.Length;
			var newLength = MetroDataRefAllocator.Show(_cache, field);
			if (!newLength.HasValue)
				return; // Canceled

			var oldAddress = field.DataAddress;
			long newAddress;
			using (var stream = _fileManager.OpenReadWrite())
			{
				// Allocate a new block as to not potentially disturb shared uses
				newAddress = _cache.Allocator.Allocate((uint)newLength.Value, stream);
				_cache.SaveChanges(stream);
			}

			// Changing these causes a read from the file, so the stream has to be closed first
			field.Length = newLength.Value;
			field.DataAddress = newAddress;

			using (var stream = _fileManager.OpenReadWrite())
			{
				// Force a save back to the file
				var changes = new FieldChangeSet();
				changes.MarkChanged(field);
				var metaUpdate = new MetaWriter(stream, _tag.RawTag.MetaLocation.AsOffset(), _cache, _buildInfo,
					MetaWriter.SaveType.File, changes, _stringIdTrie, _srcSegmentGroup);
				metaUpdate.WriteFields(_pluginVisitor.Values);
				_fileChanges.MarkUnchanged(field);

				_cache.SaveChanges(stream);
			}

			MetroMessageBox.Show("Data Reference Allocator - Assembly",
				"The data was allocated successfully. Its address is 0x" + newAddress.ToString("X8") + ".");
		}

		private void IsolateDataRefCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _tagCommandState == TagDataCommandState.None;
		}
		
		private void IsolateDataRefCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (_tagCommandState != TagDataCommandState.None)
			{
				MetroMessageBox.Show("Data Reference Isolation", TagDataCommandStateResolver.GetStateDescription(_tagCommandState));
				return;
			}

			var field = GetWrappedField(e.OriginalSource) as DataRef;
			if (field == null)
				return;
		
			var oldAddress = field.DataAddress;
			int size = field.Length;
			if (oldAddress == 0 || size == 0)
			{
				MetroMessageBox.Show("Data Reference Isolation", "Cannot isolate: data is null.");
				return;
			}
		
			var result = MetroMessageBox.Show("Data Reference Isolation", "Are you sure you want to copy this data to a new location?", MetroMessageBox.MessageBoxButtons.OkCancel);
			if (result != MetroMessageBox.MessageBoxResult.OK)
				return;
		
			long newAddress;
			using (var stream = _fileManager.OpenReadWrite())
			{
				// Allocate the data
				newAddress = _cache.Allocator.Allocate((uint)size, stream);
				_cache.SaveChanges(stream);
		
				//copy data
				stream.SeekTo(_srcSegmentGroup.PointerToOffset(oldAddress));
				var data = stream.ReadBlock(size);
		
				stream.SeekTo(_srcSegmentGroup.PointerToOffset(newAddress));
				stream.WriteBlock(data);
			}
		
			// Changing these causes a read from the file, so the stream has to be closed first
			field.DataAddress = newAddress;
		
			using (var stream = _fileManager.OpenReadWrite())
			{
				// Force a save back to the file
				var changes = new FieldChangeSet();
				changes.MarkChanged(field);
				var metaUpdate = new MetaWriter(stream, _tag.RawTag.MetaLocation.AsOffset(), _cache, _buildInfo,
					MetaWriter.SaveType.File, changes, _stringIdTrie, _srcSegmentGroup);
				metaUpdate.WriteFields(_pluginVisitor.Values);
				_fileChanges.MarkUnchanged(field);

				_cache.SaveChanges(stream);
			}
			MetroMessageBox.Show("Data Reference Isolation - Assembly",
					"The data reference was isolated successfully.");
		}

		private void InfoValueData_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
				Clipboard.SetText(((System.Windows.Documents.Run)e.OriginalSource).Text);
		}

		public void ExternalSave()
		{
			if (_pluginPath != null && File.Exists(_pluginPath))
				UpdateMeta(MetaWriter.SaveType.File, true, false);
		}

		public void ExternalDump(string basePath)
		{
			HandleDump(Path.Combine(basePath, _tag.TagFileName + "[" + _tag.GroupName + "]" + ".json"), 1);
		}
	}
}