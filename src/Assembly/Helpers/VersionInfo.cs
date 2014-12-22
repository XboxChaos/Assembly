using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

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
		/// Gets the user-friendly version string for the program if one has been loaded.
		/// This will be in the format yyyy.mm.dd.hh.mm.ss and represents the time the program was built.
		/// Hours are stored as 24-hour time.
		/// </summary>
		/// <returns>The user-friendly version string if available, or <c>null</c> otherwise.</returns>
		public static string GetUserFriendlyVersion()
		{
			return (_currentVersion != null) ? _currentVersion.DisplayVersion : null;
		}

		/// <summary>
		/// Gets the program's internal version string.
		/// </summary>
		/// <returns>The internal version string for the program.</returns>
		public static string GetInternalVersion()
		{
			return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		}

		/// <summary>
		/// Compares two version numbers which contain components separated by periods.
		/// </summary>
		/// <param name="version1">The left-hand version.</param>
		/// <param name="version2">The right-hand version.</param>
		/// <returns>
		/// A negative number if <paramref name="version1"/> is less than <paramref name="version2"/>,
		/// zero if <paramref name="version1"/> equals <paramref name="version2"/>,
		/// and a positive number if <paramref name="version1"/> is greater than <paramref name="version2"/>.
		/// </returns>
		/// <exception cref="System.ArgumentException">Thrown if the versions do not have the same number of components.</exception>
		/// <exception cref="System.FormatException">Thrown if either version number is in an invalid format.</exception>
		public static int Compare(string version1, string version2)
		{
			var parts1 = version1.Split('.');
			var parts2 = version2.Split('.');
			if (parts1.Length != parts2.Length)
				throw new ArgumentException("The versions do not have the same number of components");
			for (var i = 0; i < parts1.Length; i++)
			{
				int part1, part2;
				if (!int.TryParse(parts1[i], out part1))
					throw new FormatException("Unable to parse component " + i + " in the left-hand version");
				if (!int.TryParse(parts2[i], out part2))
					throw new FormatException("Unable to parse component " + i + " in the right-hand version");
				if (part1 < part2)
					return -1;
				if (part1 > part2)
					return 1;
			}
			return 0;
		}

		[JsonObject]
		private class VersionData
		{
			[JsonProperty(PropertyName = "display_version")]
			public string DisplayVersion { get; set; }
		}
	}
}
