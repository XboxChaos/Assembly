using System;
using System.Windows;
using Atlas.Dialogs;
using Atlas.Native;
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

			ViewModel = new HomeViewModel();
			DataContext = ViewModel;

			App.Storage.HomeWindow = this;
			App.Storage.HomeWindowViewModel = ViewModel;

#if !DEBUG
			DebugMenuItems.Visibility = Visibility.Collapsed;
#endif
		}

		protected override void OnStateChanged(EventArgs e)
		{
			ViewModel.OnStateChanged(WindowState, e);
			base.OnStateChanged(e);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			OnStateChanged(null);
			DwmDropShadow.DropShadowToWindow(this);

			base.OnSourceInitialized(e);
		}

		private void OpenCacheMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.OpenFile(HomeViewModel.Type.BlamCache);
		}

		#region Debug Menu

		private void MessageBoxDiaogTextMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			MetroMessageBox.Show("Message Box Title",
				"Haxx0r ipsum L0phtCrack January 1, 1970 spoof epoch continue suitably small values tunnel in. Worm fail packet sniffer for ascii giga nak double flood linux boolean int gcc. Cd I'm compiling overflow fopen endif default system break James T. Kirk bar vi hexadecimal unix rsa ack. ");
		}

		#endregion
	}
}
