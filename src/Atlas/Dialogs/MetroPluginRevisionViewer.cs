using System.Collections.Generic;
using System.Windows;
using Atlas.Dialogs.Controls;
using Atlas.ViewModels.Dialog;
using Blamite.Plugins;

namespace Atlas.Dialogs
{
	public static class MetroPluginRevisionViewer
	{
		public static void Show(string title, IList<PluginRevision> revisions)
		{
			var dialog = new MetroPluginRevisionViewerWindow(new PluginRevisionViewerViewModel(title, revisions))
			{
				Owner = App.Storage.HomeWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			App.Storage.HomeWindowViewModel.ShowDialog();
			dialog.ShowDialog();
			App.Storage.HomeWindowViewModel.HideDialog();
		}
	}
}
