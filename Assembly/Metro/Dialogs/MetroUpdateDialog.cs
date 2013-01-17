using Assembly.Helpers;
using Assembly.Helpers.Net;

namespace Assembly.Metro.Dialogs
{
    public static class MetroUpdateDialog
    {
        public static void Show(UpdateInfo info)
        {
            // ill up date u
            Settings.homeWindow.ShowMask();
			var updater = new ControlDialogs.Updater(info)
				              {
					              Owner = Settings.homeWindow,
					              WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
				              };
	        updater.ShowDialog();
            Settings.homeWindow.HideMask();
        }
    }
}
