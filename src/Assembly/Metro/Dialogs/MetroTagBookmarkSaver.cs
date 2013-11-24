using System.Collections.Generic;
using Assembly.Helpers;

namespace Assembly.Metro.Dialogs
{
	public class MetroTagBookmarkSaver
	{
		/// <summary>
		/// Show a Metro Message Box
		/// </summary>
		public static KeyValuePair<string, int> Show()
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var tagBookmarkSaver = new ControlDialogs.TagBookmarkSaver()
			{
				Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
				WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
			};
			tagBookmarkSaver.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();

			var tempStorage = TempStorage.TagBookmarkSaver;
			TempStorage.TagBookmarkSaver = new KeyValuePair<string, int>(null, -1);
			return tempStorage;
		}
	}
}