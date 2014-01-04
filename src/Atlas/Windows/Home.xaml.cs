using System;
using Atlas.Native;
using Atlas.Pages;
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

		private void OpenCacheMenuItem_OnClick(object sender, System.Windows.RoutedEventArgs e)
		{
			ViewModel.OpenFile(HomeViewModel.Type.BlamCache);
		}
	}
}
