using System.IO;
using System.Linq;
using System.Xml;
using Atlas.Dialogs;
using Atlas.Helpers;
using Atlas.Helpers.Plugins;
using Atlas.Helpers.Tags;
using Atlas.Models;
using Atlas.Pages.CacheEditors;
using Atlas.Pages.CacheEditors.TagEditorComponents.Data;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.RTE;
using Blamite.Util;

namespace Atlas.ViewModels.CacheEditors
{
	public class TagEditorViewModel : Base
	{
		public TagEditorViewModel(CachePageViewModel cachePageViewModel, TagHierarchyNode tagHierarchyNode)
		{
			CachePageViewModel = cachePageViewModel;
			TagHierarchyNode = tagHierarchyNode;
			FileManager = _cachePageViewModel.MapStreamManager;

			// Load Plugin Path
			var className = VariousFunctions.SterilizeTagClassName(CharConstant.ToString(TagHierarchyNode.TagClass.Magic)).Trim();
			PluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
				CachePageViewModel.EngineDescription.Settings.GetSetting<string>("plugins"), className);

			RefreshUserInterface();
		}

		#region Properties

		public CachePageViewModel CachePageViewModel
		{
			get { return _cachePageViewModel; }
			set { SetField(ref _cachePageViewModel, value); }
		}
		private CachePageViewModel _cachePageViewModel;

		public TagHierarchyNode TagHierarchyNode
		{
			get { return _tagHierarchyNode; }
			set { SetField(ref _tagHierarchyNode, value); }
		}
		private TagHierarchyNode _tagHierarchyNode;

		public IRTEProvider RteProvider
		{
			get { return _rteProvider; }
			set { SetField(ref _rteProvider, value); }
		}
		private IRTEProvider _rteProvider;

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

		#endregion

		public void LoadTagData(TagDataReader.LoadType type, TagEditor editor)
		{
			if (!File.Exists(PluginPath))
			{
				PluginExists = false;
				RefreshUserInterface();
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
					if (_rteProvider == null)
						goto default;

					if (_rteProvider.GetMetaStream(_cachePageViewModel.CacheFile) == null)
					{
						ShowConnectionError();
						return;
					}

					streamManager = new RTEStreamManager(_rteProvider, _cachePageViewModel.CacheFile);
					baseOffset = _tagHierarchyNode.Tag.MetaLocation.AsPointer();
					break;

				default:
					MetroMessageBox.Show("Not Supported", "That feature is not supported for this game.");
					return;
			}

			// Load Plugin File
			using (var xml = XmlReader.Create(_pluginPath))
			{
				PluginVisitor = new ThirdGenPluginVisitor(CachePageViewModel.ActiveHierarchy, CachePageViewModel.StringIdTrie,
					CachePageViewModel.CacheFile.MetaArea, App.Storage.Settings.TagEditorShowInvisibles);
				AssemblyPluginLoader.LoadPlugin(xml, PluginVisitor);
			}

			ChangeTracker = new FieldChangeTracker();
			FieldChanges = new FieldChangeSet();
			MemoryChanges = new FieldChangeSet();

			var tagDataReader = new TagDataReader(streamManager, baseOffset, CachePageViewModel.CacheFile, CachePageViewModel.EngineDescription, type, FieldChanges);
			Flattener = new TagBlockFlattener(tagDataReader, ChangeTracker, FieldChanges);
			Flattener.Flatten(PluginVisitor.Values);
			tagDataReader.ReadFields(PluginVisitor.Values);
			editor.TagDataViewer.ItemsSource = PluginVisitor.Values;

			// Start monitoring fields for changes
			ChangeTracker.RegisterChangeSet(FieldChanges);
			ChangeTracker.RegisterChangeSet(MemoryChanges);
			ChangeTracker.Attach(PluginVisitor.Values);

			RefreshUserInterface();
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
				var tagDataUpdate = new TagDataWriter(stream, (uint) TagHierarchyNode.Tag.MetaLocation.AsOffset(),
					CachePageViewModel.CacheFile, CachePageViewModel.EngineDescription, TagDataWriter.SaveType.File, FieldChanges,
					CachePageViewModel.StringIdTrie);

				tagDataUpdate.WriteFields(PluginVisitor.Values);
				CachePageViewModel.CacheFile.SaveChanges(stream);
				FieldChanges.MarkAllUnchanged();

				// TODO: update window status...
			}
		}

		private void SaveTagDataToMemory(bool onlySaveChanged = true)
		{
			if (RteProvider == null)
			{
				MetroMessageBox.Show("Unable to Save Changes", "The Real Time Provider that assembly creates to send data to the correct console/device is null. This shouldn't happen. Try re-opening the cache file.");
				return;
			}

			using (var stream = RteProvider.GetMetaStream(CachePageViewModel.CacheFile))
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

				// TODO: update window status...
			}
		}

		public void RefreshUserInterface()
		{
			if (PluginExists)
			{

			}
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

		private bool ConfirmNewStringIds()
		{
			var newStrings = (from stringIdField in FieldChanges.OfType<StringIDData>() where !CachePageViewModel.StringIdTrie.Contains(stringIdField.Value) select stringIdField.Value).ToList();

			if (newStrings.Count > 0)
				return MetroMessageBoxList.Show("New StringIds",
					"The following stringID(s) do not currently exist in the cache file and will be added.\r\nContinue?", newStrings);

			return true;
		}
	}
}
