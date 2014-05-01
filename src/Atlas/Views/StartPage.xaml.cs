using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Atlas.Dialogs;
using Atlas.Models;
using Atlas.ViewModels;

namespace Atlas.Views
{
	/// <summary>
	/// Interaction logic for StartPage.xaml
	/// </summary>
	public partial class StartPage : IAssemblyPage
	{
		public StartViewModel ViewModel { get; private set; }

		public StartPage()
		{
			InitializeComponent();

			DataContext = ViewModel = new StartViewModel();

			RecentsControl.DataContext = App.Storage.Settings;
		}

		public bool Close()
		{
			return true;
		}

		private void OpenRecentFileButton_OnClick(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;

			var recentFile = button.DataContext as RecentFile;
			if (recentFile == null) return;

			if (!File.Exists(recentFile.FilePath))
			{
				if (
					MetroMessageBox.Show("This file no longer exists", "The specified file no longer exists in the specified path. Would you like to go ahead and remove it from your recents?",
						new List<MetroMessageBox.MessageBoxButton>
						{
							MetroMessageBox.MessageBoxButton.Yes,
							MetroMessageBox.MessageBoxButton.No,
							MetroMessageBox.MessageBoxButton.Cancel
						}) == MetroMessageBox.MessageBoxButton.Yes)
					App.Storage.Settings.RecentFiles.Remove(recentFile);
				return;
			}
			App.Storage.HomeWindowViewModel.OpenFile(recentFile.FilePath, recentFile.Type);
		}

		private void OpenCacheButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.OpenFile(HomeViewModel.Type.BlamCache);
		}

		private void OpenMapImageButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.OpenFile(HomeViewModel.Type.MapImage);
		}

		private void OpenMapInfoButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.OpenFile(HomeViewModel.Type.MapInfo);
		}

		private void OpenCampaignButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.OpenFile(HomeViewModel.Type.Campaign);
		}
	}
}
