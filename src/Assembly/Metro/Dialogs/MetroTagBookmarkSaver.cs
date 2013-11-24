using System.Collections.Generic;
using System.Windows;
using Assembly.Helpers;
using Assembly.Metro.Dialogs.ControlDialogs;

namespace Assembly.Metro.Dialogs
{
	public class MetroTagBookmarkSaver
	{
		/// <summary>
		///     Show a Metro Message Box
		/// </summary>
		public static KeyValuePair<string, int> Show()
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var tagBookmarkSaver = new TagBookmarkSaver
			{
				Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			tagBookmarkSaver.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();

			KeyValuePair<string, int> tempStorage = TempStorage.TagBookmarkSaver;
			TempStorage.TagBookmarkSaver = new KeyValuePair<string, int>(null, -1);
			return tempStorage;
		}
	}
}