using Atlas.Models;

namespace Atlas.ViewModels.Dialog
{
	public class BusyAlertBoxViewModel : Base
	{
		public BusyAlertBoxViewModel(string title, string message)
		{
			Title = title;
			Message = message;
		}

		public string Title
		{
			get { return _title; }
			set { SetField(ref _title, value); }
		}
		private string _title;

		public string Message
		{
			get { return _message; }
			set { SetField(ref _message, value); }
		}
		private string _message;

		public bool CanClose
		{
			get { return _canClose; }
			set { SetField(ref _canClose, value); }
		}
		private bool _canClose;
	}
}
