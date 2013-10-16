using Assembly.Helpers;
using Assembly.Metro.Dialogs.ControlDialogs;

namespace Assembly.Metro.Dialogs
{
	public static class MetroInputBox
	{
		/// <summary>
		/// Show a Metro Input Message Box
		/// </summary>
		/// <param name="title">The title of the Message Box.</param>
		/// <param name="message">The message to display inside the Message Box.</param>
		/// <returns>The string the user entered.</returns>
		public static string Show(string title, string message)
		{
			var dialog = new MessageBoxInput(title, message, "", null, "");
			dialog.ShowDialog();

			return TempStorage.MessageBoxInputStorage;
		}

		/// <summary>
		/// Show a Metro Input Message Box
		/// </summary>
		/// <param name="title">The title of the Message Box.</param>
		/// <param name="message">The message to display inside the Message Box.</param>
		/// <param name="defaultText">The default text to show in the input box.</param>
		/// <returns>The string the user entered.</returns>
		public static string Show(string title, string message, string defaultText)
		{
			var dialog = new MessageBoxInput(title, message, "", null, defaultText);
			dialog.ShowDialog();

			return TempStorage.MessageBoxInputStorage;
		}

		/// <summary>
		/// Show a Metro Input Message Box
		/// </summary>
		/// <param name="title">The title of the Message Box.</param>
		/// <param name="message">The message to display inside the Message Box.</param>
		/// <param name="defaultText">The default text to show in the input box.</param>
		/// <param name="placeholder">The placeholder text to put in the Input Box.</param>
		/// <returns>The string the user entered.</returns>
		public static string Show(string title, string message, string defaultText, string placeholder)
		{
			var dialog = new MessageBoxInput(title, message, placeholder, null, defaultText);
			dialog.ShowDialog();

			return TempStorage.MessageBoxInputStorage;
		}

		/// <summary>
		/// Show a Metro Input Message Box
		/// </summary>
		/// <param name="title">The title of the Message Box.</param>
		/// <param name="message">The message to display inside the Message Box.</param>
		/// <param name="defaultText">The default text to show in the input box.</param>
		/// <param name="placeholder">The placeholder text to put in the Input Box.</param>
		/// <param name="regexMatch">The regex string to use to validate user input.</param>
		/// <returns>The string the user entered.</returns>
		public static string Show(string title, string message, string defaultText, string placeholder, string regexMatch)
		{
			var dialog = new MessageBoxInput(title, message, placeholder, regexMatch, defaultText);
			dialog.ShowDialog();

			return TempStorage.MessageBoxInputStorage;
		}

	}
}
