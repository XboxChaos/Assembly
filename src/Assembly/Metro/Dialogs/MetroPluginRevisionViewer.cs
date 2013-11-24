using System.Collections.Generic;
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
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var revisionViewer = new ControlDialogs.PluginRevisionViewer(revisions, pluginClass)
									 {
										 Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
										 WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
									 };
			revisionViewer.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();
		}
	}
}
