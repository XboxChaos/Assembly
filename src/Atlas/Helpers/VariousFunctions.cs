using System.IO;
using System.Text.RegularExpressions;

namespace Atlas.Helpers
{
	public static class VariousFunctions
	{
		public static readonly char[] DisallowedPluginChars =
		{
			' ', '>', '<', ':', '-', '_', '/', '\\', '&', ';', '!', '?',
			'|', '*', '"'
		};
		private static readonly string InvalidFileNameChars = new string(Path.GetInvalidFileNameChars());

		/// <summary>
		///     Gets the parent directory of the application's exe
		/// </summary>
		public static string GetApplicationLocation()
		{
			return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
		}

		/// <summary>
		/// Gets the path of a plugin
		/// </summary>
		/// <param name="gamePluginName">The name of the plugin folder for that game.</param>
		/// <param name="className">The name of the class for that plugin.</param>
		public static string GetPluginPath(string gamePluginName, string className)
		{
			 return string.Format("{0}\\{1}\\{2}.xml", GetApplicationLocation() + @"Plugins", gamePluginName, className);
		}

		/// <summary>
		///     Gets the location of the applications assembly (lulz, assembly.exe)
		/// </summary>
		public static string GetApplicationAssemblyLocation()
		{
			return System.Reflection.Assembly.GetExecutingAssembly().Location;
		}

		/// <summary>
		///     Replaces invalid filename characters in a tag class with an underscore (_) so that it can be used as part of a
		///     path.
		/// </summary>
		/// <param name="name">The tag class string to replace invalid characters in.</param>
		/// <returns>The "sterilized" name.</returns>
		public static string SterilizeTagClassName(string name)
		{
			// http://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name
			var regex = string.Format(@"(\.+$)|([{0}])", InvalidFileNameChars);
			return Regex.Replace(name, regex, "_");
		}
	}
}
