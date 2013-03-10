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
            Settings.homeWindow.Dispatcher.Invoke(new Action(() => MetroUpdateDialog.Show(info, UpdateAvailable(info))));
        }

        public static bool UpdateAvailable(UpdateInfo info)
        {
            if (info == null || !info.Successful)
                return false;

            // Just convert the version strings to ints and compare
            var serverVersion = VersionStringToInt(info.LatestVersion);
            var localVersion = VersionStringToInt(VariousFunctions.GetApplicationVersion());

            return (serverVersion > localVersion);
        }

        private static int VersionStringToInt(string version)
        {
            version = version.Replace(".", "");
            int versionInt;
            return int.TryParse(version, out versionInt) ? versionInt : 0;
        }
    }
}
