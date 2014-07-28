using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Xml;
using Atlas.Dialogs;
using Atlas.Helpers;
using Atlas.Helpers.Plugins;
using Atlas.Helpers.SyntaxHighlighting;
using Atlas.Helpers.Tags;
using Atlas.Models;
using Atlas.Models.Cache;
using Atlas.Views.Cache;
using Atlas.Views.Cache.TagEditorComponents.Data;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.RTE;
using Blamite.Util;
using ICSharpCode.AvalonEdit.Highlighting;

namespace Atlas.ViewModels.Cache
{
	public class TagEditorViewModel : Base
	{
		public TagEditorViewModel(CachePageViewModel cachePageViewModel, TagHierarchyNode tagHierarchyNode, TagEditor tagEditor)
		{
			CachePageViewModel = cachePageViewModel;
			TagHierarchyNode = tagHierarchyNode;
			TagEditor = tagEditor;
			FileManager = CachePageViewModel.MapStreamManager;
			_searchTimer = new Timer(SearchTimer);

			// Load Plugin Path
			var className = VariousFunctions.SterilizeTagClassName(CharConstant.ToString(TagHierarchyNode.TagClass.Magic)).Trim();
			PluginPath =
				VariousFunctions.GetPluginPath(CachePageViewModel.EngineDescription.Settings.GetSetting<string>("plugins"),
					className);
			PluginContent = File.ReadAllText(PluginPath);
		}

		#region Properties

		public IHighlightingDefinition SyntaxHighlightingDefinition
		{
			get
			{
				return HighlightLoader.LoadEmbeddedDefinition("XmlBlue.xshd");
			}
		}

		public CachePageViewModel CachePageViewModel
		{
			get { return _cachePageViewModel; }
			set { SetField(ref _cachePageViewModel, value); }
		}
		private CachePageViewModel _cachePageViewModel;

		#region Tag Editor Stuff

		public TagHierarchyNode TagHierarchyNode
		{
			get { return _tagHierarchyNode; }
			set { SetField(ref _tagHierarchyNode, value); }
		}
		private TagHierarchyNode _tagHierarchyNode;

		public TagEditor TagEditor
		{
			get { return _tagEditor; }
			set { SetField(ref _tagEditor, value); }
		}
		private TagEditor _tagEditor;

		public FieldChangeTracker ChangeTracker
		{
			get { return _changeTracker; }
			set { SetField(ref _changeTracker, value); }
		}
		private FieldChangeTracker _changeTracker;

		public IStreamManager FileManager
		{
			get { return _fileManager; }
			set { SetField(ref _fileManager, value); }
		}
		private IStreamManager _fileManager;

		public FieldChangeSet FieldChanges
		{
			get { return _fieldChanges; }
			set { SetField(ref _fieldChanges, value); }
		}
		private FieldChangeSet _fieldChanges;

		public TagBlockFlattener Flattener
		{
			get { return _flattener; }
			set { SetField(ref _flattener, value); }
		}
		private TagBlockFlattener _flattener;

		public FieldChangeSet MemoryChanges
		{
			get { return _memoryChanges; }
			set { SetField(ref _memoryChanges, value); }
		}
		private FieldChangeSet _memoryChanges;

		#endregion

		#region Plugin Stuff

		public string PluginPath
		{
			get { return _pluginPath; }
			set { SetField(ref _pluginPath, value); }
		}
		private string _pluginPath;

		public ThirdGenPluginVisitor PluginVisitor
		{
			get { return _pluginVisitor; }
			set { SetField(ref _pluginVisitor, value); }
		}
		private ThirdGenPluginVisitor _pluginVisitor;

		public bool PluginExists
		{
			get { return _pluginExists; }
			set { SetField(ref _pluginExists, value); }
		}
		private bool _pluginExists = true;

		public string PluginContent
		{
			get { return _pluginContent; }
			set { SetField(ref _pluginContent, value); }
		}
		private string _pluginContent;

		#endregion

		#region Search

		public string SearchQuery
		{
			get { return _searchQuery; }
			set
			{
				SetField(ref _searchQuery, value);
				_searchTimer.Change(100, Timeout.Infinite);
			}
		}
		private string _searchQuery;
		private readonly Timer _searchTimer;

		public ObservableCollection<TagDataSearchResult> SearchResults
		{
			get { return _searchResults ?? new ObservableCollection<TagDataSearchResult>(); }
			set { SetField(ref _searchResults, value); }
		}
		private ObservableCollection<TagDataSearchResult> _searchResults;

		public TagDataSearchResult SelectedSearchResult
		{
			get { return _selectedSearchResult; }
			set { SetField(ref _selectedSearchResult, value); }
		}
		private TagDataSearchResult _selectedSearchResult;

		public Dictionary<TagDataField, int> ResultIndices
		{
			get { return _resultIndices; }
			set { SetField(ref _resultIndices, value); }
		}
		private Dictionary<TagDataField, int> _resultIndices = new Dictionary<TagDataField, int>();

		#endregion

		#endregion

		#region Transporting Tag Data

		public void LoadTagData(TagDataReader.LoadType type, TagEditor editor)
		{
			if (!File.Exists(PluginPath))
			{
				PluginExists = false;
				MetroMessageBox.Show("Plugin doesn't exist. It can't be loaded for this tag.");
				return;
			}
			PluginExists = true;

			IStreamManager streamManager;
			uint baseOffset;
			switch (type)
			{
				case TagDataReader.LoadType.File:
					streamManager = _fileManager;
					baseOffset = (uint)_tagHierarchyNode.Tag.MetaLocation.AsOffset();
					break;

				case TagDataReader.LoadType.Memory:
					if (CachePageViewModel.RteProvider == null)
						goto default;

					if (CachePageViewModel.RteProvider.GetMetaStream(_cachePageViewModel.CacheFile) == null)
					{
						ShowConnectionError();
						return;
					}

					streamManager = new RteStreamManager(CachePageViewModel.RteProvider, _cachePageViewModel.CacheFile);
					baseOffset = _tagHierarchyNode.Tag.MetaLocation.AsPointer();
					break;

				default:
					MetroMessageBox.Show("Not Supported", "That feature is not supported for this game.");
					return;
			}

			// Load Plugin File
			using (var xml = XmlReader.Create(_pluginPath))
			{
				PluginVisitor = new ThirdGenPluginVisitor(CachePageViewModel.ClassHierarchy, CachePageViewModel.StringIdTrie,
					CachePageViewModel.CacheFile.MetaArea, App.Storage.Settings.TagEditorShowInvisibles);
				AssemblyPluginLoader.LoadPlugin(xml, PluginVisitor);
			}

			ChangeTracker = new FieldChangeTracker();
			FieldChanges = new FieldChangeSet();
			MemoryChanges = new FieldChangeSet();

			var tagDataReader = new TagDataReader(streamManager, baseOffset, CachePageViewModel.CacheFile,
				CachePageViewModel.EngineDescription, type, FieldChanges);
			Flattener = new TagBlockFlattener(tagDataReader, ChangeTracker, FieldChanges);
			Flattener.Flatten(PluginVisitor.Values);
			tagDataReader.ReadFields(PluginVisitor.Values);
			editor.TagDataViewer.ItemsSource = PluginVisitor.Values;

			// Start monitoring fields for changes
			ChangeTracker.RegisterChangeSet(FieldChanges);
			ChangeTracker.RegisterChangeSet(MemoryChanges);
			ChangeTracker.Attach(PluginVisitor.Values);

			if (type == TagDataReader.LoadType.File)
				App.Storage.HomeWindowViewModel.Status = "Successfully loaded tag data from cache file";
			else
				switch (CachePageViewModel.RteProvider.ConnectionType)
				{
					case RteConnectionType.ConsoleX360:
						App.Storage.HomeWindowViewModel.Status =
							"Successfully loaded tag data from Xbox 360 Development Console's memory";
						break;

					case RteConnectionType.LocalProcess:
						App.Storage.HomeWindowViewModel.Status = "Successfully loaded tag data from Local Machine's memory ";
						break;
				}
		}

		public void SaveTagData(TagDataWriter.SaveType type, bool onlySaveChanged = true)
		{
			switch (type)
			{
				case TagDataWriter.SaveType.File:
					SaveTagDataToFile();
					break;

				case TagDataWriter.SaveType.Memory:
					SaveTagDataToMemory(onlySaveChanged);
					break;
			}
		}
		private void SaveTagDataToFile()
		{
			if (!ConfirmNewStringIds())
				return;

			using (var stream = FileManager.OpenReadWrite())
			{
				var tagDataUpdate = new TagDataWriter(stream, (uint)TagHierarchyNode.Tag.MetaLocation.AsOffset(),
					CachePageViewModel.CacheFile, CachePageViewModel.EngineDescription, TagDataWriter.SaveType.File, FieldChanges,
					CachePageViewModel.StringIdTrie);

				tagDataUpdate.WriteFields(PluginVisitor.Values);
				CachePageViewModel.CacheFile.SaveChanges(stream);
				FieldChanges.MarkAllUnchanged();

				App.Storage.HomeWindowViewModel.Status = "Successfully written changes to cache file";
			}
		}
		private void SaveTagDataToMemory(bool onlySaveChanged = true)
		{
			if (CachePageViewModel.RteProvider == null)
			{
				MetroMessageBox.Show("Unable to Save Changes", "The Real Time Provider that assembly creates to send data to the correct console/device is null. This shouldn't happen. Try re-opening the cache file.");
				return;
			}

			using (var stream = CachePageViewModel.RteProvider.GetMetaStream(CachePageViewModel.CacheFile))
			{
				if (stream == null)
				{
					ShowConnectionError();
					return;
				}

				var changes = onlySaveChanged ? _memoryChanges : null;
				var tagDataUpdate = new TagDataWriter(stream, TagHierarchyNode.Tag.MetaLocation.AsPointer(),
					CachePageViewModel.CacheFile, CachePageViewModel.EngineDescription, TagDataWriter.SaveType.Memory, changes,
					CachePageViewModel.StringIdTrie);
				tagDataUpdate.WriteFields(PluginVisitor.Values);

				switch (CachePageViewModel.RteProvider.ConnectionType)
				{
					case RteConnectionType.ConsoleX360:
						App.Storage.HomeWindowViewModel.Status = "Successfully written changes to Xbox 360 Development Console's memory";
						break;

					case RteConnectionType.LocalProcess:
						App.Storage.HomeWindowViewModel.Status = "Successfully written changes to Local Machine's memory";
						break;
				}
			}
		}

		#endregion

		public IList<TagDataField> LoadViewValueAsPlugin()
		{
			var path = string.Format("{0}\\Examples\\ThirdGenExample.xml",
				VariousFunctions.GetApplicationLocation() + @"Plugins");
			var reader = XmlReader.Create(path);

			var plugin = new ThirdGenPluginVisitor(CachePageViewModel.ClassHierarchy, CachePageViewModel.StringIdTrie, CachePageViewModel.CacheFile.MetaArea, true);
			AssemblyPluginLoader.LoadPlugin(reader, plugin);
			reader.Close();

			return plugin.Values;
		}

		private void ShowConnectionError()
		{
			switch (CachePageViewModel.RteProvider.ConnectionType)
			{
				case RteConnectionType.ConsoleX360:
					MetroMessageBox.Show("Connection Error",
						"Unable to connect to your Xbox 360 console. Make sure that XBDM is enabled, you have the Xbox 360 SDK installed, and that your console's IP has been set correctly.");
					break;

				case RteConnectionType.LocalProcess:
					MetroMessageBox.Show("Connection Error",
						"Unable to connect to the game. Make sure that it is running on your computer and that the map you are poking to is currently loaded.");
					break;
			}
		}
		private bool ConfirmNewStringIds()
		{
			var newStrings = (from stringIdField in FieldChanges.OfType<StringIDData>() where !CachePageViewModel.StringIdTrie.Contains(stringIdField.Value) select stringIdField.Value).ToList();

			if (newStrings.Count > 0)
				return MetroMessageBoxList.Show("New StringIds",
					"The following stringID(s) do not currently exist in the cache file and will be added.\r\nContinue?", newStrings);

			return true;
		}

		#region Search Helpers

		private void FilterAndHighlightTagData()
		{
			SearchResults = new ObservableCollection<TagDataSearchResult>();
			ResultIndices.Clear();
			var filterer = new TagDataFilterer(Flattener, TagDataFilterer_CollectResult, TagDataFilterer_HighlightField);
			filterer.FilterFields(PluginVisitor.Values, SearchQuery);
			SelectedSearchResult = SearchResults.FirstOrDefault();
		}
		private void TagDataFilterer_CollectResult(TagDataField foundField, TagDataField listField, TagBlockData parent)
		{
			ResultIndices[listField] = SearchResults.Count;
			SearchResults.Add(new TagDataSearchResult(foundField, listField, parent));
		}
		private static void TagDataFilterer_HighlightField(TagDataField field, bool highlight)
		{
			field.Opacity = highlight ? 1f : .15f;
		}

		private void SearchTimer(object state)
		{
			lock (_searchTimer)
			{
				if (string.IsNullOrEmpty(SearchQuery))
				{
					ResetSearch();
					return;
				}

				Application.Current.Dispatcher.Invoke(delegate
				{
					FilterAndHighlightTagData();
					SelectFirstResult();
				});
			}
		}
		private void ResetSearch()
		{
			SearchResults = null;
			ResultIndices.Clear();
			ShowAll();
			SelectField(null);
		}
		private void SelectFirstResult()
		{
			if (SearchResults.Any())
			{
				Application.Current.Dispatcher.Invoke(delegate
				{
					if (FindResultByListField(SelectedSearchResult.Field) == -1)
						SelectResult(SearchResults[0]);
					else
						TagEditor.TagDataViewer.ScrollIntoView(SelectedSearchResult);
				});
			}
		}

		private void ShowAll()
		{
			foreach (var field in PluginVisitor.Values)
				ShowField(field);
		}
		private void ShowField(TagDataField field)
		{
			field.Opacity = 1.0f;

			// If the field is a tag block, recursively set the opacity of its children
			var tagBlock = field as TagBlockData;
			if (tagBlock == null) return;

			// Show wrappers
			Flattener.EnumWrappers(tagBlock, ShowField);

			// Show template fields
			foreach (var child in tagBlock.Template)
				ShowField(child);

			// Show modified fields
			foreach (var child in tagBlock.Pages.SelectMany(page => page.Fields.Where(child => child != null)))
				ShowField(child);
		}

		public int FindResultByListField(TagDataField field)
		{
			int index;
			if (field != null && _resultIndices.TryGetValue(field, out index))
				return index;
			return -1;
		}
		public void SelectField(TagDataField field)
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				if (field == null)
					return;

				TagEditor.TagDataViewer.ScrollIntoView(field);
				TagEditor.TagDataViewer.SelectedItem = field;
			});
		}
		public void SelectResult(TagDataSearchResult result)
		{
			var tagBlock = result.TagBlock;
			if (tagBlock != null)
				Flattener.ForceVisible(tagBlock);

			SelectedSearchResult = result;
			SelectField(result.ListField);
		}

		#endregion
	}
}