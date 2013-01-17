using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assembly.Helpers;
using ExtryzeDLL.Plugins;

namespace Assembly.Metro.Dialogs
{
    public class MetroPluginRevisionViewer
    {
        /// <summary>
        /// Show the Plugin Revision Viewer Window
        /// </summary>
        public static void Show(IList<PluginRevision> revisions, string pluginClass)
        {
            Settings.homeWindow.ShowMask();
            ControlDialogs.PluginRevisionViewer revisionViewer = new ControlDialogs.PluginRevisionViewer(revisions, pluginClass);
            revisionViewer.Owner = Settings.homeWindow;
            revisionViewer.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            revisionViewer.ShowDialog();
            Settings.homeWindow.HideMask();
        }
    }
}
