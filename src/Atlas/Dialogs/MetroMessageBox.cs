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
			var dialog = new MetroMessageBoxWindow(new MessageBoxViewModel(title, message))
			{
				Owner = App.Storage.HomeWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			App.Storage.HomeWindowViewModel.ShowDialog();
			dialog.ShowDialog();
			App.Storage.HomeWindowViewModel.HideDialog();
		}
	}
}
