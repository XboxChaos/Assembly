using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Assembly.Metro.Native;
using ExtryzeDLL.Plugins;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for PluginRevisionViewer.xaml
    /// </summary>
    public partial class PluginRevisionViewer : Window
    {
        private IList<PluginRevision> _revisions = new List<PluginRevision>();
        private string _pluginClass;

        public IList<PluginRevision> Revisions
        {
            get { return _revisions; }
        }

        public PluginRevisionViewer(IList<PluginRevision> revisions, string pluginClass)
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);

            _revisions = revisions;
            _pluginClass = pluginClass;

            lblDesc.Text = lblDesc.Text.Replace("%1%", pluginClass);

            
        }

        private void headerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Left = Left + e.HorizontalChange;
            Top = Top + e.VerticalChange;
        }
        private void btnActionClose_Click(object sender, RoutedEventArgs e) { this.Close(); }

        private void Button_Click_1(object sender, RoutedEventArgs e) { this.Close(); }
    }
}
