using System.Collections.Generic;
using System.Windows;
using Atlas.Dialogs.Controls;
using Atlas.ViewModels.Dialog;

namespace Atlas.Dialogs
{
	public static class MetroMessageBoxList
	{
		/// <summary>
		///     Shows a metro message box containing a list of items in it.
		/// </summary>
		/// <param name="title">The title of the Message Box</param>
		/// <param name="message">The message to be displayed in the Message Box</param>
		/// <param name="items">The items to be displayed in the message box.</param>
		public static bool Show(string title, string message, IEnumerable<object> items)
		{
			var msgBox = new MetroMessageBoxListWindow(new MessageBoxListViewModel(title, message, items))
			{
				Owner = App.Storage.HomeWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			msgBox.ShowDialog();
			return msgBox.DialogResult ?? false;
		}

		/// <summary>
		///     Show a Metro Message Box
		/// </summary>
		/// <param name="message">The message to be displayed in the Message Box</param>
		/// <param name="items">The items to be displayed in the message box.</param>
		public static bool Show(string message, IEnumerable<object> items)
		{
			return Show("Message Box - Assembly", message, items);
		}
	}
}