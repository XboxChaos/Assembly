using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Atlas.ViewModels;

namespace Atlas.Pages.CacheEditors
{
	/// <summary>
	/// Interaction logic for AdvancedMemoryEditor.xaml
	/// </summary>
	public partial class AdvancedMemoryEditor : ICacheEditor
	{
		private CachePageViewModel _cachePageViewModel;

		public AdvancedMemoryEditor(CachePageViewModel cachePageViewModel)
		{
			_cachePageViewModel = cachePageViewModel;

			InitializeComponent();
			EditorTitle = "Advanced Memory Editor";

			MemoryDataGrid.DataContext = _cachePageViewModel.SelectedEngineMemoryVersion;
		}

		public bool IsSingleInstance { get { return true; } }

		public string EditorTitle
		{
			get { return _editorTitle; }
			private set { SetField(ref _editorTitle, value); }
		}
		private string _editorTitle;

		public bool Close()
		{
			throw new System.NotImplementedException();
		}


		#region Inpc Helpers

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		public virtual bool SetField<T>(ref T field, T value,
			[CallerMemberName] string propertyName = "")
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		#endregion
	}
}
