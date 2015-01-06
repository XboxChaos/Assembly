using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Assembly.Helpers.Net;
using Assembly.Metro.Dialogs;
using Newtonsoft.Json;
using XboxChaos;
using XboxChaos.Models;

namespace Assembly.Helpers
{
	public class Updater
	{
		private const string ApplicationName = "Assembly"; // Application name to send to the server
		private const string StableBranchName = "master"; // Branch name for stable releases
		private const string ExperimentalBranchName = "dev"; // Branch name for experimental releases

		public static async Task<ApplicationBranchResponse> GetBranchInfo()
		{
			// Grab JSON Update package from the server
			var info = await XboxChaosApi.GetApplicationInfoAsync(ApplicationName);
			if (info == null || info.Result == null || info.Error != null)
				return null;
			return GetBranch(info.Result);
		}

		public static async Task BeginUpdateProcess()
		{
			var info = await GetBranchInfo();

			// If the request failed, tell the user to gtfo
			if (info == null)
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

		/// <summary>
		/// Gets the branch to check for updates.
		/// </summary>
		/// <param name="info">The application information.</param>
		/// <returns>The branch to check for updates, or <c>null</c> otherwise.</returns>
		private static ApplicationBranchResponse GetBranch(ApplicationResponse info)
		{
			var branchName = VersionInfo.GetCurrentBranchName();
			if (branchName == null)
				return null;
			if (IsStandardBranch(branchName))
			{
				// Current branch is either master or dev - take the user's setting instead
				branchName = GetBranchName(App.AssemblyStorage.AssemblySettings.UpdateChannel);
			}
			return info.ApplicationBranches.FirstOrDefault(b => b.Name == branchName);
		}

		/// <summary>
		/// Determines whether or not a branch name corresponds to a "standard" branch name which can be chosen from the settings.
		/// </summary>
		/// <param name="branchName">Name of the branch.</param>
		/// <returns><c>true</c> if the branch is standard.</returns>
		public static bool IsStandardBranch(string branchName)
		{
			return (branchName == StableBranchName || branchName == ExperimentalBranchName);
		}

		/// <summary>
		/// Gets the name of the branch corresponding to an update source.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <returns>The branch name.</returns>
		public static string GetBranchName(Settings.UpdateSource source)
		{
			switch (source)
			{
				case Settings.UpdateSource.Stable:
					return StableBranchName;
				case Settings.UpdateSource.Experimental:
					return ExperimentalBranchName;
				default:
					throw new ArgumentException("Unrecognized update source: " + source);
			}
		}

		public static bool UpdateAvailable(ApplicationBranchResponse info)
		{
			if (info.Version == null || info.BuildDownload == null || info.UpdaterDownload == null)
				return false;

			// If the branch name is different, force an update
			if (info.Name != VersionInfo.GetCurrentBranchName())
				return true;

			// Check for a newer internal version number
			var serverVersion = info.Version.Internal;
			var currentVersion = VersionInfo.GetInternalVersion();
			return (serverVersion.CompareTo(currentVersion) > 0);
		}
	}
}