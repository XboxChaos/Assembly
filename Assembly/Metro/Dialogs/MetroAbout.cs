using Assembly.Helpers;

namespace Assembly.Metro.Dialogs
{
    public static class MetroAbout
    {
        /// <summary>
        /// Show the About Window
        /// </summary>
        public static void Show()
        {
            Settings.homeWindow.ShowMask();
			var about = new ControlDialogs.About
				            {Owner = Settings.homeWindow, WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner};
	        about.ShowDialog();
            Settings.homeWindow.HideMask();
        }
    }
}
