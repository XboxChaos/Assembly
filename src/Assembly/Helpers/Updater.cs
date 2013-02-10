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

            // If there are no updates, tell the user to gtfo
            if (!UpdateAvailable(info))
            {
                Settings.homeWindow.Dispatcher.Invoke(new Action(
	                                                      () =>
	                                                      MetroMessageBox.Show("No Update Available",
	                                                                           "There are currently no updates available for Assembly. Sorry :(")));
                return;
            }

            // WOAH. UPDATES.
            Settings.homeWindow.Dispatcher.Invoke(new Action(() => MetroUpdateDialog.Show(info)));
        }

        private static bool UpdateAvailable(UpdateInfo info)
        {
            if (!info.Successful)
                return false;

            // Just convert the version strings to ints and compare
            var serverVersion = VersionStringToInt(info.LatestVersion);
			var localVersion = VersionStringToInt(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

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
