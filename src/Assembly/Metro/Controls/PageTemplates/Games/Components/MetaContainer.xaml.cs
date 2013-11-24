using System.Windows;
using System.Windows.Controls;
using Assembly.Helpers;
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
		private readonly MetaEditor _metaEditor;
		private MetaInformation _metaInformation;
		private PluginEditor _pluginEditor;
		private IRTEProvider _rteProvider;
		private IStreamManager _streamManager;
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

		public MetaContainer(EngineDescription buildInfo, TagEntry tag, TagHierarchy tags, ICacheFile cache,
			IStreamManager streamManager, IRTEProvider rteProvider, Trie stringIDTrie)
		{
			InitializeComponent();

			_tag = tag;
			_tags = tags;
			_buildInfo = buildInfo;
			_cache = cache;
			_streamManager = streamManager;
			_rteProvider = rteProvider;
			_stringIDTrie = stringIDTrie;

			tbMetaEditors.SelectedIndex = (int) App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor;

			// Create Meta Information Tab
			_metaInformation = new MetaInformation(_buildInfo, _tag, _cache);
			tabTagInfo.Content = _metaInformation;

			// Create Meta Editor Tab
			_metaEditor = new MetaEditor(_buildInfo, _tag, this, _tags, _cache, _streamManager, _rteProvider, _stringIDTrie)
			{
				Padding = new Thickness(0)
			};
			tabMetaEditor.Content = _metaEditor;

			// Create Plugin Editor Tab
			_pluginEditor = new PluginEditor(_buildInfo, _tag, this, _metaEditor);
			tabPluginEditor.Content = _pluginEditor;

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

			#region BSP

			/*if (true && _tag.RawTag.Class.Magic == CharConstant.FromString("sbsp")) // add some manual check here akarias, since you code isn't in blamite
			{
				tabBspEditor.Visibility = Visibility.Visible;
				tabBspEditor.Content = new BspEditor(_tag, _cache, _streamManager);
			}
			else*/
			{
				tabBspEditor.Visibility = Visibility.Collapsed;
				if (App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor == Settings.LastMetaEditorType.Bsp)
					tbMetaEditors.SelectedIndex = (int) Settings.LastMetaEditorType.MetaEditor;
			}

			#endregion

			#region Sound

			//if (_cache.ResourceMetaLoader.SupportsSounds && _tag.RawTag.Class.Magic == CharConstant.FromString("snd!"))
			//{
			//	tabSound.Visibility = Visibility.Visible;
			//	tabSound.Content = new SoundEditor(_tag, _cache, _streamManager);
			//}
			//else
			//{
			//	tabSound.Visibility = Visibility.Collapsed;
			//	if (App.AssemblyStorage.AssemblySettings.halomapLastSelectedMetaEditor == App.AssemblyStorage.AssemblySettings.LastMetaEditorType.Sound)
			//		tbMetaEditors.SelectedIndex = (int)App.AssemblyStorage.AssemblySettings.LastMetaEditorType.MetaEditor;
			//}

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

		public void LoadNewTagEntry(TagEntry tag)
		{
			TagEntry = tag;

			// Create Meta Information Tab
			_metaInformation = new MetaInformation(_buildInfo, _tag, _cache);
			tabTagInfo.Content = _metaInformation;

			// Create Meta Editor Tab
			_metaEditor.LoadNewTagEntry(tag);

			// Create Plugin Editor Tab
			_pluginEditor = new PluginEditor(_buildInfo, _tag, this, _metaEditor);
			tabPluginEditor.Content = _pluginEditor;
		}

		private void tbMetaEditors_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.HalomapLastSelectedMetaEditor =
				(Settings.LastMetaEditorType) tbMetaEditors.SelectedIndex;
		}
	}
}