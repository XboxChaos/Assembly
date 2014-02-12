using System;
using Atlas.ViewModels;
using Atlas.ViewModels.CacheEditors;

namespace Atlas.Views.CacheEditors
{
	/// <summary>
	/// Interaction logic for NetworkSessionEditor.xaml
	/// </summary>
	public partial class NetworkSessionEditor : ICacheEditor
	{
		public NetworkSessionEditorViewModel ViewModel { get; private set; }

		public string EditorTitle { get { return "Network Session"; } }
		public bool IsSingleInstance { get { return true; } }

		public NetworkSessionEditor(CachePageViewModel cachePageViewModel)
		{
			InitializeComponent();

			DataContext = ViewModel = new NetworkSessionEditorViewModel(cachePageViewModel);
		}
		
		public bool Close()
		{
			throw new NotImplementedException();
		}
	}
}
