using Assembly.Helpers;

namespace Assembly.Metro.Dialogs
{
	public static class MetroMessageBox
	{
		/// <summary>
		/// Show a Metro Message Box
		/// </summary>
		/// <param name="title">The title of the Message Box</param>
		/// <param name="message">The message to be displayed in the Message Box</param>
		public static void Show(string title, string message)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var msgBox = new ControlDialogs.MessageBox(title, message)
							 {
								 Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
								 WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
							 };
			msgBox.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();
		}
		/// <summary>
		/// Show a Metro Message Box
		/// </summary>
		/// <param name="message">The message to be displayed in the Message Box</param>
		public static void Show(string message)
		{
			Show("Assembly - Message Box", message);
		}

		/// <summary>
		/// The Results a Metro MessageBox can return
		/// </summary>
		public enum MessageBoxResult
		{
			OK,
			Yes,
			No,
			Cancel
		}
		/// <summary>
		/// The diffrent Button Combinations that Metro Message Box's support
		/// </summary>
		public enum MessageBoxButtons
		{
			Ok,
			OkCancel,
			YesNo,
			YesNoCancel
		}
		/// <summary>
		/// Show a Metro Message Box
		/// </summary>
		/// <param name="title">The title of the Message Box</param>
		/// <param name="message">The message to be displayed in the Message Box</param>
		/// <param name="buttons">The buttons to show in the Message Box</param>
		/// <returns>The button the user clicked</returns>
		public static MessageBoxResult Show(string title, string message, MessageBoxButtons buttons)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var msgBox = new ControlDialogs.MessageBoxOptions(title, message, buttons)
							 {
								 Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
								 WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
							 };
			msgBox.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();

			return TempStorage.MessageBoxButtonStorage;
		}
	}
}