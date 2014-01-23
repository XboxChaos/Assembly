using Atlas.ViewModels.Dialog;

namespace Atlas.Dialogs.Controls
{
	/// <summary>
	/// Interaction logic for MetroBusyAlertBoxWindow.xaml
	/// </summary>
	public partial class MetroBusyAlertBoxWindow
	{
		public BusyAlertBoxViewModel ViewModel { get; set; }

		public MetroBusyAlertBoxWindow(BusyAlertBoxViewModel viewModel)
		{
			DataContext = ViewModel = viewModel;
			Title = WindowTitle = viewModel.Title;

			InitializeComponent();

			Closing += (sender, args) => { args.Cancel = !ViewModel.CanClose; };
		}
	}
}