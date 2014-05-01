using Atlas.Models;

namespace Atlas.ViewModels
{
	public class StartViewModel : Base
	{
		public StartViewModel()
		{
			App.Storage.HomeWindowViewModel.UpdateStatus("Welcome");
		}

		public void OpenFile(HomeViewModel.Type type)
		{
			switch (type)
			{
				case HomeViewModel.Type.BlamCache:
					App.Storage.HomeWindowViewModel.ValidateFile(App.Storage.HomeWindowViewModel.FindFile(HomeViewModel.Type.BlamCache));
					break;

				case HomeViewModel.Type.MapInfo:
					App.Storage.HomeWindowViewModel.ValidateFile(App.Storage.HomeWindowViewModel.FindFile(HomeViewModel.Type.MapInfo));
					break;

				case HomeViewModel.Type.MapImage:
					App.Storage.HomeWindowViewModel.ValidateFile(App.Storage.HomeWindowViewModel.FindFile(HomeViewModel.Type.MapImage));
					break;

				case HomeViewModel.Type.Campaign:
					App.Storage.HomeWindowViewModel.ValidateFile(App.Storage.HomeWindowViewModel.FindFile(HomeViewModel.Type.Campaign));
					break;

				case HomeViewModel.Type.Patch:
					App.Storage.HomeWindowViewModel.ValidateFile(App.Storage.HomeWindowViewModel.FindFile(HomeViewModel.Type.Patch));
					break;

				case HomeViewModel.Type.Other:
					App.Storage.HomeWindowViewModel.ValidateFile(App.Storage.HomeWindowViewModel.FindFile(HomeViewModel.Type.Other));
					break;
			}
		}
	}
}
