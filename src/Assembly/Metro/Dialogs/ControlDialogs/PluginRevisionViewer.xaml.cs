using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using Assembly.Metro.Native;
using Blamite.Plugins;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for PluginRevisionViewer.xaml
    /// </summary>
    public partial class PluginRevisionViewer
    {
        private readonly IList<PluginRevision> _revisions = new List<PluginRevision>();
        private readonly string _pluginClass;
        private readonly string _pluginClassDescription;

        public IList<PluginRevision> Revisions
        {
            get { return _revisions; }
        }
        public string PluginClass
        {
            get { return _pluginClass; }
        }
        public string PluginClassDescription
        {
            get { return _pluginClassDescription; }
        }

        public PluginRevisionViewer(IList<PluginRevision> revisions, string pluginClass)
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);

            _revisions = revisions;
            _pluginClass = pluginClass;
            _pluginClassDescription = string.Format("Revision list of the Assembly Plugin {0}.", _pluginClass);

            DataContext = this;
        }

        private void btnActionClose_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}
