using System.Windows;
using Atlas.ViewModels.Dialog;

namespace Atlas.Dialogs.Controls
{
	/// <summary>
	/// Interaction logic for MetroMessageBoxWindow.xaml
	/// </summary>
	public partial class MetroMessageBoxWindow
	{
		public MetroMessageBoxWindow(MessageBoxViewModel viewModel)
		{
			DataContext = viewModel;
			Title = WindowTitle = viewModel.Title;

			InitializeComponent();
		}

		private void WindowCloseButton_OnClick(object sender, RoutedEventArgs e) { Close(); }
		private void OkayButton_OnClick(object sender, RoutedEventArgs e) { Close(); }
	}
}
