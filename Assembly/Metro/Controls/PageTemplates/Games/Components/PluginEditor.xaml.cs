using System;
using System.Collections.Generic;
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
using Assembly.Backend;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Util;
using ExtryzeDLL.Plugins;
using Assembly.Backend.Plugins;
using System.IO;
using ExtryzeDLL.Flexibility;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
    /// <summary>
    /// Interaction logic for PluginEditor.xaml
    /// </summary>
    public partial class PluginEditor : UserControl
    {

        private BuildInformation _buildInfo;
        private string _pluginPath;
        private MetaContainer _parent;
        private MetaEditor _sibling;

        public PluginEditor(BuildInformation buildInfo, TagEntry tag, MetaContainer parent, MetaEditor sibling)
        {
            InitializeComponent();

            _buildInfo = buildInfo;
            _parent = parent;
            _sibling = sibling;

            // Load Plugin Path
            string className = VariousFunctions.SterilizeTagClassName(CharConstant.ToString(tag.RawTag.Class.Magic));
            _pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins", _buildInfo.PluginFolder, className);

            if (File.Exists(_pluginPath))
                txtPlugin.Text = File.ReadAllText(_pluginPath);
        }

        private void btnPluginSave_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(_pluginPath, txtPlugin.Text);
            _sibling.RefreshEditor();
            _parent.tbMetaEditors.SelectedIndex = 3;
        }
    }
}
