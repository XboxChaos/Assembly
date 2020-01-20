using System.Collections.Generic;
using System.Windows;
using Assembly.Metro.Dialogs.ControlDialogs;
using Blamite.Plugins;

namespace Assembly.Metro.Dialogs
{
	public static class MetroPluginRevisionViewer
	{
		/// <summary>
		///     Show the Plugin Revision Viewer Window
		/// </summary>
		public static void Show(IList<PluginRevision> revisions, string pluginGroup)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var revisionViewer = new PluginRevisionViewer(revisions, pluginGroup)
			{
				Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			revisionViewer.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();
		}
	}
}