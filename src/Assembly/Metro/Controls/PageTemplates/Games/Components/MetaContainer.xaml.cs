using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using System.Xml;
using Assembly.Helpers;
using Assembly.Helpers.Plugins;
using Blamite.Blam;
using Blamite.Flexibility;
using Blamite.IO;
using Blamite.RTE;
using Blamite.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
    /// <summary>
    /// Interaction logic for MetaContainer.xaml
    /// </summary>
    public partial class MetaContainer : UserControl
    {
        private TagEntry _tag;
        private BuildInformation _buildInfo;
        private ICacheFile _cache;
        private Trie _stringIDTrie;

		private MetaInformation _metaInformation;
		private MetaEditor _metaEditor;
		private PluginEditor _pluginEditor;

        #region Public Access
        public TagEntry TagEntry
        {
            get { return _tag; }
            set { _tag = value; }
        }
        #endregion

        public MetaContainer(BuildInformation buildInfo, TagEntry tag, TagHierarchy tags, ICacheFile cache, IStreamManager streamManager, IRTEProvider rteProvider, Trie stringIDTrie)
        {
            InitializeComponent();

            _tag = tag;
            _buildInfo = buildInfo;
            _cache = cache;
            _stringIDTrie = stringIDTrie;

            tbMetaEditors.SelectedIndex = (int)Settings.halomapLastSelectedMetaEditor;

            // Create Meta Information Tab
            _metaInformation = new MetaInformation(_buildInfo, _tag, _cache);
			tabTagInfo.Content = _metaInformation;

            // Create Meta Editor Tab
			_metaEditor = new MetaEditor(_buildInfo, _tag, this, tags, _cache, streamManager, rteProvider, _stringIDTrie);
			_metaEditor.Padding = new Thickness(0);
            tabMetaEditor.Content = _metaEditor;

            // Create Plugin Editor Tab
            _pluginEditor = new PluginEditor(_buildInfo, _tag, this, _metaEditor);
            tabPluginEditor.Content = _pluginEditor;
        }

		public void GoToRawPluginLine(int pluginLine)
		{
			tbMetaEditors.SelectedIndex = (int)Settings.LastMetaEditorType.PluginEditor;
			_pluginEditor.GoToLine(pluginLine);

		}

        private void tbMetaEditors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.halomapLastSelectedMetaEditor = (Settings.LastMetaEditorType)tbMetaEditors.SelectedIndex;
        }
    }
}
