using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assembly.Helpers;

namespace Assembly.Metro.Dialogs
{
    public class MetroImgurUpload
    {
        public static void Show(string imageID)
        {
            Settings.homeWindow.ShowMask();
            ControlDialogs.ImgurUpload upload = new ControlDialogs.ImgurUpload(imageID);
            upload.Owner = Settings.homeWindow;
            upload.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            upload.ShowDialog();
            Settings.homeWindow.HideMask();
        }
    }
}
