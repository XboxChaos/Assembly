using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using XboxChaos.Models;

namespace Assembly.Helpers
{
	/// <summary>
	/// Contains methods for getting program version info.
	/// </summary>
	public static class VersionInfo
	{
		private static VersionData _currentVersion;

		/// <summary>
		/// Loads version information from a JSON file.
		/// </summary>
		/// <param name="filename">The JSON file to load version information from.</param>
		/// <returns><c>true</c> if successful.</returns>
		public static bool Load(string filename)
		{
			if (!File.Exists(filename))
				return false;
			var contents = File.ReadAllText(filename);
			_currentVersion = JsonConvert.DeserializeObject<VersionData>(contents);
			return true;
		}

		/// <summary>
		/// Gets the user-friendly version number for the program if one has been loaded.
		/// </summary>
		/// <returns>The user-friendly version number if available, or <c>null</c> otherwise.</returns>
		public static BranchTimeVersion GetUserFriendlyVersion()
		{
			return (_currentVersion != null) ? _currentVersion.DisplayVersion : null;
		}

		/// <summary>
		/// Gets the name of the branch this application was built from.
		/// </summary>
		/// <returns>The branch name if available, or <c>null</c> otherwise.</returns>
		public static string GetCurrentBranchName()
		{
			var friendly = GetUserFriendlyVersion();
			return (friendly != null) ? friendly.BranchName : null;
		}

		/// <summary>
		/// Gets the program's internal version number.
		/// </summary>
		/// <returns>The internal version number for the program.</returns>
		public static Version GetInternalVersion()
		{
			return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
		}

		[JsonObject]
		private class VersionData
		{
			[JsonProperty(PropertyName = "display_version")]
			public BranchTimeVersion DisplayVersion { get; set; }
		}
	}
}
