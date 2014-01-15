using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml;
using Atlas.Helpers;
using Atlas.Helpers.Plugins;
using Atlas.Helpers.Tags;
using Atlas.Pages.CacheEditors.TagEditorComponents.Data;
using Atlas.ViewModels;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.RTE;
using Blamite.Util;

namespace Atlas.Pages.CacheEditors
{
	/// <summary>
	/// Interaction logic for TagEditor.xaml
	/// </summary>
	public partial class TagEditor : ICacheEditor
	{
		private CachePageViewModel _cachePageViewModel;
		private TagHierarchyNode _tagHierarchyNode;
		private readonly IRTEProvider _rteProvider;
		private FieldChangeTracker _changeTracker;
		private readonly IStreamManager _fileManager;
		private FieldChangeSet _fileChanges;
		private ReflexiveFlattener _flattener;
		private FieldChangeSet _memoryChanges;
		private string _pluginPath;
		private ThirdGenPluginVisitor _pluginVisitor;

		public bool IsSingleInstance { get { return false; } }

		public string EditorTitle
		{
			get { return _editorTitle; }
			private set { SetField(ref _editorTitle, value); }
		}
		private string _editorTitle;
		private const string EditorFormat = "Tag - {0}";

		public TagEditor(CachePageViewModel cachePageViewModel, TagHierarchyNode tagHierarchyNode)
		{
			DataContext = _cachePageViewModel = cachePageViewModel;
			_tagHierarchyNode = tagHierarchyNode;
			_fileManager = _cachePageViewModel.MapStreamManager;
			EditorTitle = string.Format(EditorFormat, _tagHierarchyNode.Name);

			InitializeComponent();

			// Load Plugin Path
			var className = VariousFunctions.SterilizeTagClassName(CharConstant.ToString(_tagHierarchyNode.TagClass.Magic)).Trim();
			var pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
				_cachePageViewModel.EngineDescription.Settings.GetSetting<string>("plugins"), className);

			// Set Option boxes
			//cbShowInvisibles.IsChecked = App.Storage.Settings.PluginsShowInvisibles;
			//cbShowComments.IsChecked = App.Storage.Settings.PluginsShowComments;
			//cbShowEnumIndex.IsChecked = App.Storage.Settings.TagEditorShowEnumIndex;
			//cbShowInformation.IsChecked = App.Storage.Settings.TagEditorShowBlockInformation;

			// Load Meta
			RefreshEditor(MetaReader.LoadType.File, pluginPath);
		}

		public bool Close()
		{
			throw new NotImplementedException();
		}

		public void RefreshEditor(MetaReader.LoadType type, string pluginPath)
		{
			// Set the stream manager and base offset to use based upon the LoadType
			IStreamManager streamManager;
			uint baseOffset;
			switch (type)
			{
				case MetaReader.LoadType.File:
					streamManager = _fileManager;
					baseOffset = (uint)_tagHierarchyNode.Tag.MetaLocation.AsOffset();
					break;

				case MetaReader.LoadType.Memory:
					if (_rteProvider == null)
						goto default;

					if (_rteProvider.GetMetaStream(_cachePageViewModel.CacheFile) == null)
					{
						//ShowConnectionError();
						return;
					}

					streamManager = new RTEStreamManager(_rteProvider, _cachePageViewModel.CacheFile);
					baseOffset = _tagHierarchyNode.Tag.MetaLocation.AsPointer();
					break;

				default:
					// TODO: fix dialogs
					//MetroMessageBox.Show("Not Supported", "That feature is not supported for this game.");
					return;
			}

			// Load Plugin File
			using (var xml = XmlReader.Create(pluginPath))
			{
				_pluginVisitor = new ThirdGenPluginVisitor(_cachePageViewModel.ActiveHierarchy, _cachePageViewModel.StringIdTrie, _cachePageViewModel.CacheFile.MetaArea,
					App.Storage.Settings.TagEditorShowInvisibles);
				AssemblyPluginLoader.LoadPlugin(xml, _pluginVisitor);
			}

			_changeTracker = new FieldChangeTracker();
			_fileChanges = new FieldChangeSet();
			_memoryChanges = new FieldChangeSet();

			var metaReader = new MetaReader(streamManager, baseOffset, _cachePageViewModel.CacheFile, _cachePageViewModel.EngineDescription, type, _fileChanges);
			_flattener = new ReflexiveFlattener(metaReader, _changeTracker, _fileChanges);
			_flattener.Flatten(_pluginVisitor.Values);
			metaReader.ReadFields(_pluginVisitor.Values);
			TagDataViewer.ItemsSource = _pluginVisitor.Values;

			// Start monitoring fields for changes
			_changeTracker.RegisterChangeSet(_fileChanges);
			_changeTracker.RegisterChangeSet(_memoryChanges);
			_changeTracker.Attach(_pluginVisitor.Values);

			// Update Meta Toolbar
			//UpdateMetaButtons(true);
		}

		#region Inpc Helpers

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		public virtual bool SetField<T>(ref T field, T value,
			[CallerMemberName] string propertyName = "")
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		#endregion
	}
}
