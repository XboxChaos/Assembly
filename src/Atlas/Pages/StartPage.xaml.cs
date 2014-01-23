using Atlas.ViewModels;

namespace Atlas.Pages
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
	}
}
