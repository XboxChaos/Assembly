using System.Windows;
using Atlas.ViewModels.Cache.Dialog;

namespace Atlas.Views.Cache.Dialogs.Controls
{
	/// <summary>
	/// Interaction logic for TagExtractorWindow.xaml
	/// </summary>
	public partial class TagExtractorWindow
	{
		public TagExtractorViewModel ViewModel { get; private set; }

		public TagExtractorWindow(TagExtractorViewModel viewModel)
		{
			InitializeComponent();

			DataContext = ViewModel = viewModel;
			Title = WindowTitle = "Tag Extractor";
		}

		private void ExtractButton_OnClick(object sender, RoutedEventArgs e) { Close(); }
		private void WindowCloseButton_OnClick(object sender, RoutedEventArgs e) { Close(); }
	}
}
