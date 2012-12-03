using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assembly.Metro.Dialogs;
using System.Windows.Threading;

namespace Assembly.Backend
{
    public class Updater
    {
        public class UpdateFormat
        {
            public int AssemblyVersion { get; set; }
            public int ServerVersion { get; set; }
            public string AssemblyVersionSpecial { get; set; }
            public string ServerVersionSpecial { get; set; }
            public bool CanUpdate { get; set; }

            public string EXELocation { get; set; }
            public string ComponentsLocation { get; set; }

            public string ChangeLog { get; set; }
            public string Hash { get; set; }
        }
        private static UpdateFormat _updateFormat = new UpdateFormat();

        public static void BeginUpdateProcess()
        {
            // Grab JSON Update package from the server
            _updateFormat = ServerConnector.GetServerUpdateInfo();

            // If there are no updates, tell the user then gtfo
            if (!CheckForUpdates())
            {
                Settings.homeWindow.Dispatcher.Invoke(new Action(delegate
                    {
                        MetroMessageBox.Show("No Update Avaiable!", "There are currently no updates avaiable for Assembly. Sorry :(.");
                    }));
                return;
            }

            // WOAH. UPDATES.
            Settings.homeWindow.Dispatcher.Invoke(new Action(delegate
                {
                    MetroUpdateDialog.Show(_updateFormat);
                }));
        }

        private static bool CheckForUpdates()
        {
            try { return _updateFormat.CanUpdate; }
            catch { return false; }
        }
    }
}
