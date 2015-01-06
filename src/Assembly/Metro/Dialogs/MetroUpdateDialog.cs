using System.Windows;
using Assembly.Helpers.Net;
using Assembly.Metro.Dialogs.ControlDialogs;
using XboxChaos.Models;

namespace Assembly.Metro.Dialogs
{
	public static class MetroUpdateDialog
	{
		public static void Show(ApplicationBranchResponse info, bool available)
		{
			// ill up date u
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var updater = new Updater(info, available)
			{
				Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			updater.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();
		}
	}
}