using System.Windows;
using Atlas.Native;
using Atlas.ViewModels.Dialog;

namespace Atlas.Dialogs.Controls
{
	/// <summary>
	/// Interaction logic for MetroViewValueAsWindow.xaml
	/// </summary>
	public partial class MetroViewValueAsWindow
	{
		public ViewValueAsViewModel ViewModel { get; private set; }

		public MetroViewValueAsWindow(ViewValueAsViewModel viewModel)
		{
			InitializeComponent();

			DwmDropShadow.DropShadowToWindow(this);
			DataContext = ViewModel = viewModel;
			Title = ViewModel.Title;

			ViewModel.RefreshTagData();
		}

		private void WindowCloseButton_OnClick(object sender, RoutedEventArgs e) { Close(); }
	}
}
