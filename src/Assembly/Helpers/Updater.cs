using System;
using System.Collections.Generic;
using System.IO;
using Assembly.Helpers.Net;
using Assembly.Metro.Dialogs;
using Newtonsoft.Json;

namespace Assembly.Helpers
{
	public class Updater
	{
		public const string PostUpdatePath = "update.json";

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
			if (info == null || !info.Successful)
				return false;

			var serverVersion = info.LatestVersion;
			var currentVersion = VariousFunctions.GetApplicationVersion();

			return (serverVersion.CompareTo(currentVersion) > 0);
		}

		public static PostUpdateInfo LoadPostUpdateInfo(string path)
		{
			var info = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<PostUpdateInfo>(info);
		}

		public static void CleanUpAfterUpdate(PostUpdateInfo info)
		{
			DeleteFiles(info.FilesToDelete);
		}

		private static void DeleteFiles(IEnumerable<string> deletedFiles)
		{
			var basePath = VariousFunctions.GetApplicationLocation();
			foreach (var path in deletedFiles)
			{
				// Ensure the path points to a file inside Assembly's directory
				if (Path.IsPathRooted(path))
					continue;
				var fullPath = Path.GetFullPath(Path.Combine(basePath, path));
				if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
					continue;
				try
				{
					File.Delete(fullPath);
				}
				catch
				{
				}
			}
		}
	}

	[JsonObject]
	public class PostUpdateInfo
	{
		[JsonProperty(PropertyName = "version")]
		public string Version { get; set; }

		[JsonProperty(PropertyName = "delete_files")]
		public string[] FilesToDelete { get; set; }
	}
}