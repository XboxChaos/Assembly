using System.Collections.Generic;
using Assembly.Helpers;
using Blamite.Plugins;

namespace Assembly.Metro.Dialogs
{
    public static class MetroPluginRevisionViewer
    {
        /// <summary>
        /// Show the Plugin Revision Viewer Window
        /// </summary>
        public static void Show(IList<PluginRevision> revisions, string pluginClass)
        {
            Settings.homeWindow.ShowMask();
			var revisionViewer = new ControlDialogs.PluginRevisionViewer(revisions, pluginClass)
				                     {
					                     Owner = Settings.homeWindow,
					                     WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
				                     };
	        revisionViewer.ShowDialog();
            Settings.homeWindow.HideMask();
        }
    }
}
