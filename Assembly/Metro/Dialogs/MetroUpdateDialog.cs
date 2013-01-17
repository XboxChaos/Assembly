using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assembly.Helpers;
using Assembly.Helpers.Net;

namespace Assembly.Metro.Dialogs
{
    public class MetroUpdateDialog
    {
        public static void Show(UpdateInfo info)
        {
            // SHOW DIZ UPDATERRRR
            Settings.homeWindow.ShowMask();
            ControlDialogs.Updater updater = new ControlDialogs.Updater(info);
            updater.Owner = Settings.homeWindow;
            updater.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            updater.ShowDialog();
            Settings.homeWindow.HideMask();
        }
    }
}
