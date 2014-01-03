using System.Windows;
using System.Windows.Controls;
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components.Editors;
using Assembly.Helpers.Tags;
using Blamite.Blam;
using Blamite.Flexibility;
using Blamite.IO;
using Blamite.RTE;
using Blamite.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	/// <summary>
	///     Interaction logic for MetaContainer.xaml
	/// </summary>
	public partial class MetaContainer
	{
		private readonly EngineDescription _buildInfo;
		private readonly ICacheFile _cache;
		private MetaEditor _metaEditor;
		private MetaInformation _metaInformation;
		private PluginEditor _pluginEditor;
		private IRTEProvider _rteProvider;
		private IStreamManager _streamManager;
		private string _cacheLocation;
		private Trie _stringIDTrie;
		private TagHierarchy _tags;

		public MetaContainer(EngineDescription buildInfo, string cacheLocation, ITag tag, TagHierarchy tags, ICacheFile cache,
			IStreamManager streamManager, IRTEProvider rteProvider, Trie stringIDTrie)
		{
			InitializeComponent();

			_cacheLocation = cacheLocation;
			_tags = tags;
			_buildInfo = buildInfo;
			_cache = cache;
			_streamManager = streamManager;
			_rteProvider = rteProvider;
			_stringIDTrie = stringIDTrie;

			tbMetaEditors.SelectedIndex = (int) App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor;

			LoadTag(tag);

			// Create Raw Tabs

			#region Models

			//if (_cache.ResourceMetaLoader.SupportsRenderModels && _tag.RawTag.Class.Magic == CharConstant.FromString("mode"))
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

			if (_cache.ResourceMetaLoader.SupportsSounds && tag.Class.Magic == CharConstant.FromString("snd!"))
			{
				tabSoundEditor.Visibility = Visibility.Visible;
				tabSoundEditor.Content = new SoundEditor(_buildInfo, _cacheLocation, tag, _cache, _streamManager);
			}
			else
			{
				tabSoundEditor.Visibility = Visibility.Collapsed;
				if (App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor == 
					Settings.LastMetaEditorType.Sound)
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

		public void LoadTag(ITag tag)
		{
			// Create Meta Information Tab
			_metaInformation = new MetaInformation(_buildInfo, tag, _cache);
			tabTagInfo.Content = _metaInformation;

			// Create Meta Editor Tab
			_metaEditor = new MetaEditor(_buildInfo, tag, this, _tags, _cache, _streamManager, _rteProvider, _stringIDTrie);
			tabMetaEditor.Content = _metaEditor;

			// Create Plugin Editor Tab
			_pluginEditor = new PluginEditor(_buildInfo, tag, this, _metaEditor);
			tabPluginEditor.Content = _pluginEditor;
		}

		private void tbMetaEditors_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor =
				(Settings.LastMetaEditorType) tbMetaEditors.SelectedIndex;
		}
	}
}