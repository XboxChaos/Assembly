using System;
using Atlas.ViewModels;

namespace Atlas.Pages
{
	/// <summary>
	/// Interaction logic for CachePage.xaml
	/// </summary>
	public partial class CachePage : IAssemblyPage
	{
		public readonly CachePageViewModel ViewModel;

		public CachePage(string cacheLocation)
		{
			InitializeComponent();

			ViewModel = new CachePageViewModel();
			DataContext = ViewModel;

			ViewModel.LoadCache(cacheLocation);

			// Set Datacontext's
			TagTreeView.DataContext = ViewModel.ActiveHierarchy;
			CacheInformationPropertyGrid.SelectedObject = ViewModel.CacheHeaderInformation;
		}

		public bool Close()
		{
			// Ask for user permission to close
			throw new NotImplementedException();
		}
	}
}
