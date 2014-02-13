using Atlas.Models;

namespace Atlas.ViewModels.Cache
{
	public class NetworkSessionEditorViewModel : Base
	{
		public NetworkSessionEditorViewModel(CachePageViewModel cachePageViewModel)
		{
			CachePageViewModel = cachePageViewModel;
		}

		public CachePageViewModel CachePageViewModel
		{
			get { return _cachePageViewModel; }
			set { SetField(ref _cachePageViewModel, value); }
		}
		private CachePageViewModel _cachePageViewModel;
	}
}
