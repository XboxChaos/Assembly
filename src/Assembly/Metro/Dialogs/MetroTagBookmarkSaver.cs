using System.Collections.Generic;
using Assembly.Helpers;

namespace Assembly.Metro.Dialogs
{
	public class MetroTagBookmarkSaver
	{
		/// <summary>
		/// Show a Metro Message Box
		/// </summary>
		/// <param name="title">The title of the Message Box</param>
		/// <param name="message">The message to be displayed in the Message Box</param>
		public static KeyValuePair<string, int> Show()
		{
			Settings.homeWindow.ShowMask();
			var tagBookmarkSaver = new ControlDialogs.TagBookmarkSaver()
			{
				Owner = Settings.homeWindow,
				WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
			};
			tagBookmarkSaver.ShowDialog();
			Settings.homeWindow.HideMask();

			var tempStorage = TempStorage.TagBookmarkSaver;
			TempStorage.TagBookmarkSaver = new KeyValuePair<string, int>(null, -1);
			return tempStorage;
		}
	}
}