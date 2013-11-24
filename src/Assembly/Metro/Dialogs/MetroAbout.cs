namespace Assembly.Metro.Dialogs
{
	public static class MetroAbout
	{
		/// <summary>
		/// Show the About Window
		/// </summary>
		public static void Show()
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var about = new ControlDialogs.About { Owner = App.AssemblyStorage.AssemblySettings.HomeWindow, WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner };
			about.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();
		}
	}
}
