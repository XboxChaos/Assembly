namespace Assembly.Metro.Dialogs
{
	public static class MetroImgurUpload
	{
		public static void Show(string imageId)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var upload = new ControlDialogs.ImgurUpload(imageId)
							 {
								 Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
								 WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
							 };
			upload.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();
		}
	}
}
