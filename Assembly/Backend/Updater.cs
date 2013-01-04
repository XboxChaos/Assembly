using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assembly.Metro.Dialogs;
using System.Windows.Threading;
using Assembly.Backend.Net;

namespace Assembly.Backend
{
    public class Updater
    {
        public static void BeginUpdateProcess()
        {
            // Grab JSON Update package from the server
            UpdateInfo info = Updates.GetUpdateInfo();

            // If there are no updates, tell the user to gtfo
            if (!UpdateAvailable(info))
            {
                Settings.homeWindow.Dispatcher.Invoke(new Action(delegate
                    {
                        MetroMessageBox.Show("No Update Available", "There are currently no updates available for Assembly. Sorry :(");
                    }));
                return;
            }

            // WOAH. UPDATES.
            Settings.homeWindow.Dispatcher.Invoke(new Action(delegate
                {
                    MetroUpdateDialog.Show(info);
                }));
        }

        private static bool UpdateAvailable(UpdateInfo info)
        {
            if (!info.Successful)
                return false;

            // Just convert the version strings to ints and compare
            int serverVersion = VersionStringToInt(info.LatestVersion);
            int localVersion = VersionStringToInt(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

            return (serverVersion > localVersion);
        }

        private static int VersionStringToInt(string version)
        {
            version = version.Replace(".", "");
            int versionInt;
            if (int.TryParse(version, out versionInt))
                return versionInt;
            return 0;
        }
    }
}
