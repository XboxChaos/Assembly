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
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;

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

        #region Public Access
        public TagEntry TagEntry
        {
            get { return _tag; }
            set { _tag = value; }
        }
        #endregion

        public MetaContainer(BuildInformation buildInfo, TagEntry tag, TagHierarchy tags, ICacheFile cache, IStreamManager streamManager)
        {
            InitializeComponent();

            _tag = tag;
            _buildInfo = buildInfo;
            _cache = cache;

            tbMetaEditors.SelectedIndex = (int)Settings.halomapLastSelectedMetaEditor;

            // Create Meta Information Tab
            MetaInformation metaInformation = new MetaInformation(_buildInfo, _tag, _cache);
            tabTagInfo.Content = metaInformation;

            // Create Meta Editor Tab
            MetaEditor metaEditor = new MetaEditor(_buildInfo, _tag, tags, _cache, streamManager);
            tabMetaEditor.Content = metaEditor;

            // Create Plugin Editor Tab
            PluginEditor pluginEditor = new PluginEditor(_buildInfo, _tag, this, metaEditor);
            tabPluginEditor.Content = pluginEditor;
        }

        private void tbMetaEditors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.halomapLastSelectedMetaEditor = (Settings.LastMetaEditorType)tbMetaEditors.SelectedIndex;
        }
    }
}
