using Assembly.Helpers.Net;

namespace Assembly.Metro.Dialogs
{
	public static class MetroUpdateDialog
	{
		public static void Show(UpdateInfo info, bool available)
		{
			// ill up date u
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var updater = new ControlDialogs.Updater(info, available)
							  {
								  Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
								  WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
							  };
			updater.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();
		}
	}
}
