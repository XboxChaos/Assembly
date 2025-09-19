using System.Windows;
using System.Windows.Controls;
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components.Editors;
using Blamite.Blam;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.RTE;
using Blamite.Util;
using System.Windows.Input;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	/// <summary>
	///     Interaction logic for MetaContainer.xaml
	/// </summary>
	public partial class MetaContainer
	{
		private readonly EngineDescription _buildInfo;
		private readonly ICacheFile _cache;
		private readonly MetaEditor _metaEditor;
		private PluginEditor _pluginEditor;
		private StringEditor _stringEditor;
		private RTEProvider _rteProvider;
		private IStreamManager _streamManager;
		private string _cacheLocation;
		private Trie _stringIDTrie;
		private TagEntry _tag;
		private TagHierarchy _tags;

		#region Public Access

		public TagEntry TagEntry
		{
			get { return _tag; }
			set { _tag = value; }
		}

		#endregion

		public MetaContainer(EngineDescription buildInfo, string cacheLocation, TagEntry tag, TagHierarchy tags, ICacheFile cache,
			IStreamManager streamManager, RTEProvider rteProvider, Trie stringIDTrie)
		{
			InitializeComponent();

			_cacheLocation = cacheLocation;
			_tag = tag;
			_tags = tags;
			_buildInfo = buildInfo;
			_cache = cache;
			_streamManager = streamManager;
			_rteProvider = rteProvider;
			_stringIDTrie = stringIDTrie;

			tbMetaEditors.SelectedIndex = (int) App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor;

			// Create Meta Information Tab
			//_metaInformation = new MetaInformation(_buildInfo, _tag, _cache);
			//tabTagInfo.Content = _metaInformation;
			if (App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor ==
				Settings.LastMetaEditorType.Info)
				tbMetaEditors.SelectedIndex =
					(int)Settings.LastMetaEditorType.MetaEditor;


			// Create Meta Editor Tab
			_metaEditor = new MetaEditor(_buildInfo, _tag, this, _tags, _cache, _streamManager, _rteProvider, _stringIDTrie);
			tabMetaEditor.Content = _metaEditor;

			// Create Plugin Editor Tab
			_pluginEditor = new PluginEditor(_buildInfo, _tag, this, _metaEditor);
			tabPluginEditor.Content = _pluginEditor;

			// Create Raw Tabs

			#region Models

			//if (_cache.ResourceMetaLoader.SupportsRenderModels && _tag.RawTag.Group.Magic == CharConstant.FromString("mode"))
			//{
			//	tabSound.Visibility = Visibility.Visible;
			//	tabSound.Content = new SoundEditor(_tag, _cache, _streamManager);
			//}
			//else
			//{
			//	tabSound.Visibility = Visibility.Collapsed;
			//	if (App.AssemblyStorage.AssemblySettings.halomapLastSelectedMetaEditor == App.AssemblyStorage.AssemblySettings.LastMetaEditorType.Model)
			//		tbMetaEditors.SelectedIndex = (int)App.AssemblyStorage.AssemblySettings.LastMetaEditorType.MetaEditor;
			//}

			#endregion

			#region Sound

			//if (_cache.ResourceMetaLoader.SupportsSounds && _tag.RawTag.Group.Magic == CharConstant.FromString("snd!"))
			//{
			//	tabSoundEditor.Visibility = Visibility.Visible;
			//	tabSoundEditor.Content = new SoundEditor(_buildInfo, _cacheLocation, _tag, _cache, _streamManager);
			//}
			//else
			{
				tabSoundEditor.Visibility = Visibility.Collapsed;
				if (App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor == 
					Settings.LastMetaEditorType.Sound)
					tbMetaEditors.SelectedIndex = 
						(int)Settings.LastMetaEditorType.MetaEditor;
			}

			#endregion

			#region Strings
			if ((_tag.GroupName == "hmt " && _buildInfo.Layouts.HasLayout("hmt")) ||
				((_tag.GroupName == "str#" || _tag.GroupName == "ustr") && _buildInfo.Layouts.HasLayout("str")) ||
				(_tag.GroupName == "unic" && _cache.LocaleArea == null &&
				(_buildInfo.Layouts.HasLayout("unic split") || _buildInfo.Layouts.HasLayout("unic table"))))
			{
				tabStringEditor.Visibility = Visibility.Visible;
				tabStringEditor.Content = new StringEditor(_buildInfo, _cache, _tag, _streamManager);
			}
			else
			{
				tabStringEditor.Visibility = Visibility.Collapsed;
				if (App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor ==
					Settings.LastMetaEditorType.String)
					tbMetaEditors.SelectedIndex =
						(int)Settings.LastMetaEditorType.MetaEditor;
			}
			#endregion
		}

		public void GoToRawPluginLine(int pluginLine)
		{
			tbMetaEditors.SelectedItem = tabPluginEditor;
			_pluginEditor.GoToLine(pluginLine);
		}

		public void ShowMetaEditor()
		{
			tbMetaEditors.SelectedItem = tabMetaEditor;
		}

        public void RefreshMetaEditor()
        {
            _metaEditor.RefreshEditor(MetaData.MetaReader.LoadType.File);
        }

		public void LoadNewTagEntry(TagEntry tag)
		{
			TagEntry = tag;

			// Create Meta Information Tab
			//_metaInformation = new MetaInformation(_buildInfo, _tag, _cache);
			//tabTagInfo.Content = _metaInformation;

			// Create Meta Editor Tab
			_metaEditor.LoadNewTagEntry(tag);

			// Create Plugin Editor Tab
			_pluginEditor = new PluginEditor(_buildInfo, _tag, this, _metaEditor);
			tabPluginEditor.Content = _pluginEditor;

		}

		private void tbMetaEditors_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if ((int)App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor != tbMetaEditors.SelectedIndex)
				App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor =
				(Settings.LastMetaEditorType)tbMetaEditors.SelectedIndex;
		}

		public void Dispose()
		{
			PluginEditor pe = (PluginEditor)tabPluginEditor.Content;
			pe.Dispose();
		}

		public void ExternalSave()
		{
			if (_metaEditor != null)
				_metaEditor.ExternalSave();
		}

		public void ExternalDump(string basePath)
		{
			if (_metaEditor != null)
				_metaEditor.ExternalDump(basePath);
		}
	}
}