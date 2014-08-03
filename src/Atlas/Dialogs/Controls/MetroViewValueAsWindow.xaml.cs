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
			DataContext = ViewModel = viewModel;
			WindowTitle = Title = ViewModel.Title;
			ViewModel.RefreshTagData();
		}

		private void WindowCloseButton_OnClick(object sender, RoutedEventArgs e) { Close(); }

		#region Seeking/Reading Management

		private void ResetSeekButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.CacheOffset = ViewModel.CacheOffsetOriginal;
			ViewModel.RefreshTagData();
		}

		private void RefreshDataButton_Click(object sender, RoutedEventArgs e) { ViewModel.RefreshTagData(); }

		private void SeekForwardButton_Click(object sender, RoutedEventArgs e) { ViewModel.CacheOffset++; }

		private void SeekBackButton_Click(object sender, RoutedEventArgs e) { ViewModel.CacheOffset--; }

		#endregion
	}
}
