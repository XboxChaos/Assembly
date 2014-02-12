using System.Collections.Generic;
using System.Windows;
using Atlas.Dialogs;
using Atlas.ViewModels.BLF;

namespace Atlas.Views.BLF
{
	/// <summary>
	/// Interaction logic for MapImagePage.xaml
	/// </summary>
	public partial class MapImagePage : IAssemblyPage
	{
		public readonly MapImagePageViewModel ViewModel;

		public MapImagePage(string mapInfoLocation)
		{
			InitializeComponent();

			ViewModel = new MapImagePageViewModel(this);
			DataContext = ViewModel;
			ViewModel.LoadMapImage(mapInfoLocation);
		}

		public bool Close()
		{
			var result = MetroMessageBox.Show("Are you sure?",
				"Are you sure you want to close this campaign? Unsaved changes will be lost.",
				new List<MetroMessageBox.MessageBoxButton>
				{
					MetroMessageBox.MessageBoxButton.Yes,
					MetroMessageBox.MessageBoxButton.No,
					MetroMessageBox.MessageBoxButton.Cancel
				});

			return (result == MetroMessageBox.MessageBoxButton.No);
		}

		private void ReplaceImageButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ReplaceImage();
		}

		private void ExtractImageButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ExtractImage();
		}
	}
}
