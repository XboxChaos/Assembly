using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Atlas.Dialogs;
using Atlas.Native;
using Atlas.Views;
using Atlas.ViewModels;

namespace Atlas.Windows
{
	/// <summary>
	/// Interaction logic for Home.xaml
	/// </summary>
	public partial class Home
	{
		public HomeViewModel ViewModel { get; private set; }

		public Home()
		{
			InitializeComponent();
			App.Storage.HomeWindow = this;

			ViewModel = new HomeViewModel();
			DataContext = App.Storage.HomeWindowViewModel = ViewModel;
			ViewModel.AssemblyPage = new StartPage();

			Closing += OnClosing;

#if !DEBUG
			DebugMenuItems.Visibility = Visibility.Collapsed;
#endif
		}

		private static void OnClosing(object sender, CancelEventArgs cancelEventArgs)
		{
			cancelEventArgs.Cancel = !App.Storage.HomeWindowViewModel.AssemblyPage.Close();
		}

		protected override void OnStateChanged(EventArgs e)
		{
			ViewModel.OnStateChanged(WindowState, e);
			base.OnStateChanged(e);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			OnStateChanged(null);

			base.OnSourceInitialized(e);
		}

		private void OpenCacheMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.ValidateFile(ViewModel.FindFile(HomeViewModel.Type.BlamCache));
		}

		private void OpenMapImageMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.ValidateFile(ViewModel.FindFile(HomeViewModel.Type.MapImage));
		}

		private void OpenMapInfoMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.ValidateFile(ViewModel.FindFile(HomeViewModel.Type.MapInfo));
		}

		private void OpenCampaignMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.ValidateFile(ViewModel.FindFile(HomeViewModel.Type.Campaign));
		}

		#region Debug Menu

		private void MessageBoxDialogTextMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			MetroMessageBox.Show("Message Box Title",
				"Haxx0r ipsum L0phtCrack January 1, 1970 spoof epoch continue suitably small values tunnel in. Worm fail packet sniffer for ascii giga nak double flood linux boolean int gcc. Cd I'm compiling overflow fopen endif default system break James T. Kirk bar vi hexadecimal unix rsa ack. ");
		}
		private void MessageBoxOptionMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			var answer = MetroMessageBox.Show("Message Box Title", "wots ur answer?", 
				new List<MetroMessageBox.MessageBoxButton> { MetroMessageBox.MessageBoxButton.Aite, MetroMessageBox.MessageBoxButton.Okay, MetroMessageBox.MessageBoxButton.Cancel });

			MetroMessageBox.Show("answer", answer.ToString());
		}

		private void MessageBoxListMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			var answer = MetroMessageBoxList.Show("Message Box Title", "le lists", new List<string> { "item1", "item2", "item3", "gerrit" });
			MetroMessageBox.Show("answer", answer.ToString());
		}

		#endregion
	}
}
