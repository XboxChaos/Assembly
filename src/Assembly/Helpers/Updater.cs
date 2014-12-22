using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using Assembly.Helpers.Net;
using Assembly.Metro.Dialogs;
using Newtonsoft.Json;

namespace Assembly.Helpers
{
	public class Updater
	{
		public static void BeginUpdateProcess()
		{
			// Grab JSON Update package from the server
			UpdateInfo info = Updates.GetUpdateInfo();

			// If the request failed, tell the user to gtfo
			if (info == null || !info.Successful)
			{
				App.AssemblyStorage.AssemblySettings.HomeWindow.Dispatcher.Invoke(new Action(
					() =>
						MetroMessageBox.Show("Update Check Failed",
							"Assembly is unable to check for updates at this time. Sorry :(")));
				return;
			}

			App.AssemblyStorage.AssemblySettings.HomeWindow.Dispatcher.Invoke(
				new Action(() => MetroUpdateDialog.Show(info, UpdateAvailable(info))));
		}

		public static bool UpdateAvailable(UpdateInfo info)
		{
			if (info == null || !info.Successful || VersionInfo.GetUserFriendlyVersion() == null)
				return false;

			var serverVersion = info.LatestVersion;
			var currentVersion = VersionInfo.GetInternalVersion();

			return (VersionInfo.Compare(currentVersion, serverVersion) < 0);
		}
	}
}