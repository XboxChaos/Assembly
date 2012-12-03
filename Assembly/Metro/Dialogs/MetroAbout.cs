using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assembly.Backend;

namespace Assembly.Metro.Dialogs
{
    public class MetroAbout
    {
        /// <summary>
        /// Show the About Window
        /// </summary>
        public static void Show()
        {
            Settings.homeWindow.ShowMask();
            ControlDialogs.About about = new ControlDialogs.About();
            about.Owner = Settings.homeWindow;
            about.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            about.ShowDialog();
            Settings.homeWindow.HideMask();
        }
    }
}
