using System;
using Atlas.Native;
using Atlas.ViewModels;

namespace Atlas.Windows
{
	/// <summary>
	/// Interaction logic for Home.xaml
	/// </summary>
	public partial class Home
	{
		private readonly HomeViewModel _viewModel;

		public Home()
		{
			InitializeComponent();
			
			_viewModel = new HomeViewModel();
			DataContext = _viewModel;
		}

		protected override void OnStateChanged(EventArgs e)
		{
			_viewModel.OnStateChanged(WindowState, e);
			base.OnStateChanged(e);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			OnStateChanged(null);
			DwmDropShadow.DropShadowToWindow(this);

			base.OnSourceInitialized(e);
		}
	}
}
