namespace Assembly.Metro.Dialogs
{
	public static class MetroPostGeneratorViewer
	{
		/// <summary>
		/// Show a Metro Post Generator Viewer
		/// </summary>
		/// <param name="bbcode">The generated BBCode of the post.</param>
		/// <param name="modAuthor">The author of the mod.</param>
		public static void Show(string bbcode, string modAuthor)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var msgBox = new ControlDialogs.PostGeneratorViewer(bbcode, modAuthor)
							 {
								 Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
								 WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
							 };
			msgBox.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();
		}
	}
}