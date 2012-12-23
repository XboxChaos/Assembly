using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Assembly.Backend;
using Assembly.SyntaxHighlighting;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
    /// <summary>
    /// Interaction logic for PluginEditor.xaml
    /// </summary>
    public partial class PluginEditor : UserControl
    {
        private string _pluginPath;
        private MetaContainer _parent;
        private MetaEditor _sibling;

        public PluginEditor(BuildInformation buildInfo, TagEntry tag, MetaContainer parent, MetaEditor sibling)
        {
            InitializeComponent();

            _parent = parent;
            _sibling = sibling;

            LoadSyntaxHighlighting();
            Settings.SettingsChanged += Settings_SettingsChanged;

            string className = VariousFunctions.SterilizeTagClassName(CharConstant.ToString(tag.RawTag.Class.Magic));
            _pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins", buildInfo.PluginFolder, className);
            LoadPlugin();
        }

        void Settings_SettingsChanged(object sender, EventArgs e)
        {
            // Reload the syntax highlighting definition in case the theme changed
            LoadSyntaxHighlighting();
        }

        private void btnPluginSave_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(_pluginPath, txtPlugin.Text);
            _sibling.RefreshEditor();
            _parent.tbMetaEditors.SelectedIndex = 4;
        }

        private void btnLoadFromDisk_Click_1(object sender, RoutedEventArgs e)
        {
            LoadPlugin();
        }

        private void txtPlugin_MouseRightButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            // Move the cursor to the place where the click occurred (AvalonEdit doesn't do this by default)
            // http://community.sharpdevelop.net/forums/p/12521/34105.aspx
            var position = txtPlugin.GetPositionFromPoint(e.GetPosition(txtPlugin));
            if (position.HasValue)
                txtPlugin.TextArea.Caret.Position = position.Value;
        }

        private void LoadSyntaxHighlighting()
        {
            // Load the file depending upon which theme is being used
            string filename = "XMLBlue.xshd";
            switch (Settings.applicationAccent)
            {
                case Settings.Accents.Blue:
                    filename = "XMLBlue.xshd";
                    break;
                case Settings.Accents.Green:
                    filename = "XMLGreen.xshd";
                    break;
                case Settings.Accents.Orange:
                    filename = "XMLOrange.xshd";
                    break;
                case Settings.Accents.Purple:
                    filename = "XMLPurple.xshd";
                    break;
            }
            txtPlugin.SyntaxHighlighting = HighlightLoader.LoadEmbeddedDefinition(filename);
        }

        private void LoadPlugin()
        {
            // Load Plugin Path
            if (File.Exists(_pluginPath))
                txtPlugin.Text = File.ReadAllText(_pluginPath);
        }
    }
}
