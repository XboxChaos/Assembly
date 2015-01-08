using System;
using System.IO;
using System.Windows;
using System.Windows.Shell;

namespace Assembly.Helpers
{
	public class JumpLists
	{
		public static void UpdateJumplists()
		{
			var iconLibraryPath = ExtractIconLibrary();

			var jump = new JumpList();
			JumpList.SetJumpList(Application.Current, jump);

			if (App.AssemblyStorage.AssemblySettings.ApplicationRecents != null)
			{
				for (int i = 0; i < 10; i++)
				{
					if (i > App.AssemblyStorage.AssemblySettings.ApplicationRecents.Count - 1)
						break;

					var task = new JumpTask();
					int iconIndex = -200;
					switch (App.AssemblyStorage.AssemblySettings.ApplicationRecents[i].FileType)
					{
						case Settings.RecentFileType.Blf:
							iconIndex = -200;
							break;
						case Settings.RecentFileType.Cache:
							iconIndex = -201;
							break;
						case Settings.RecentFileType.MapInfo:
							iconIndex = -202;
							break;
					}

					task.ApplicationPath = VariousFunctions.GetApplicationAssemblyLocation();
					task.Arguments = string.Format("assembly://open \"{0}\"",
						App.AssemblyStorage.AssemblySettings.ApplicationRecents[i].FilePath);
					task.WorkingDirectory = VariousFunctions.GetApplicationLocation();

					task.IconResourcePath = iconLibraryPath;
					task.IconResourceIndex = iconIndex;

					task.CustomCategory = "Recent";
					task.Title = App.AssemblyStorage.AssemblySettings.ApplicationRecents[i].FileName + " - " +
					             App.AssemblyStorage.AssemblySettings.ApplicationRecents[i].FileGame;
					task.Description = string.Format("Open {0} in Assembly. ({1})",
						App.AssemblyStorage.AssemblySettings.ApplicationRecents[i].FileName,
						App.AssemblyStorage.AssemblySettings.ApplicationRecents[i].FilePath);

					jump.JumpItems.Add(task);
				}
			}

			// Show Recent and Frequent categories :D
			jump.ShowFrequentCategory = false;
			jump.ShowRecentCategory = false;

			// Send to the Windows Shell
			jump.Apply();
		}

		// Name of the embedded icon library DLL
		private const string IconLibraryName = "AssemblyIconLibrary.dll";

		/// <summary>
		/// Extracts the icon library DLL if it does not exist.
		/// </summary>
		/// <returns>The path to the DLL.</returns>
		/// <exception cref="System.InvalidOperationException">Thrown if the DLL fails to load.</exception>
		private static string ExtractIconLibrary()
		{
			// If the library has already been extracted, then don't do anything
			var iconLibraryPath = Path.Combine(Path.GetTempPath(), IconLibraryName);
			if (File.Exists(iconLibraryPath))
				return iconLibraryPath;

			// Extract it
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			var resourceName = assembly.GetName().Name + "." + IconLibraryName;
			using (var stream = assembly.GetManifestResourceStream(resourceName))
			{
				if (stream == null)
					throw new InvalidOperationException("Unable to load " + IconLibraryName);
				using (var output = File.Open(iconLibraryPath, FileMode.Create, FileAccess.Write))
					stream.CopyTo(output);
			}
			return iconLibraryPath;
		}
	}
}