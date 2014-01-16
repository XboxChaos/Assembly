using System.Collections.Generic;
using Atlas.Models;

namespace Atlas.ViewModels.Dialog
{
	public class MessageBoxListViewModel : Base
	{
		public MessageBoxListViewModel(string title, string message, IEnumerable<object> items)
		{
			Title = title;
			Message = message;
			Items = items;
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

		public IEnumerable<object> Items
		{
			get { return _items; }
			set { SetField(ref _items, value); }
		}
		private IEnumerable<object> _items;
	}
}
