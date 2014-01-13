using Atlas.Models;

namespace Atlas.ViewModels
{
	public class StartViewModel : Base
	{
		public StartViewModel()
		{
			App.Storage.HomeWindowViewModel.UpdateStatus("Welcome");
		}
	}
}
