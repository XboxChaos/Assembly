using System.Collections.Generic;
using System.Windows;
using Atlas.Dialogs.Controls;
using Atlas.ViewModels.Dialog;

namespace Atlas.Dialogs
{
	public static class MetroMessageBox
	{
		public static void Show(string message)
		{
			Show("Message Box", message);
		}

		public static void Show(string title, string message)
		{
			Show(title, message, new List<MessageBoxButton> { MessageBoxButton.Okay });
		}

		public static MessageBoxButton Show(string title, string message, List<MessageBoxButton> buttons)
		{
			var dialog = new MetroMessageBoxWindow(new MessageBoxViewModel(title, message, buttons))
			{
				Owner = App.Storage.HomeWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			App.Storage.HomeWindowViewModel.ShowDialog();
			dialog.ShowDialog();
			App.Storage.HomeWindowViewModel.HideDialog();

			return dialog.ExitButtonType;
		}

		public enum MessageBoxButton
		{
			Okay,
			Cancel,
			Yes,
			No,
			Aite
		}
	}
}
