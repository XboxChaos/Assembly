using Assembly.Helpers;

namespace Assembly.Metro.Dialogs
{
    public static class MetroImgurUpload
    {
        public static void Show(string imageID)
        {
            Settings.homeWindow.ShowMask();
			var upload = new ControlDialogs.ImgurUpload(imageID)
				             {
					             Owner = Settings.homeWindow,
								 WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
				             };
	        upload.ShowDialog();
            Settings.homeWindow.HideMask();
        }
    }
}
