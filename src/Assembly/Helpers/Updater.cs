using System;
using Assembly.Helpers.Net;
using Assembly.Metro.Dialogs;

namespace Assembly.Helpers
{
    public class Updater
    {
        public static void BeginUpdateProcess()
        {
            // Grab JSON Update package from the server
			var info = Updates.GetUpdateInfo();

            // If the request failed, tell the user to gtfo
            if (info == null || !info.Successful)
            {
                Settings.homeWindow.Dispatcher.Invoke(new Action(
	                                                      () =>
	                                                      MetroMessageBox.Show("Update Check Failed",
	                                                                           "Assembly is unable to check for updates at this time. Sorry :(")));
                return;
            }

            // WOAH. UPDATES.
            Settings.homeWindow.Dispatcher.Invoke(new Action(() => MetroUpdateDialog.Show(info)));
        }
    }
}
