using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Atlas.ViewModels;

namespace Atlas.Pages.CacheEditors
{
	/// <summary>
	/// Interaction logic for AdvancedMemoryEditor.xaml
	/// </summary>
	public partial class AdvancedMemoryEditor : ICacheEditor
	{
		public CachePageViewModel ViewModel { get; private set; }

		public AdvancedMemoryEditor(CachePageViewModel cachePageViewModel)
		{
			InitializeComponent();
			DataContext = ViewModel = cachePageViewModel;
			EditorTitle = "Advanced Memory Editor";
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
			// lets leave this hear until the editor is finished. It'll remind us to add this feature when it's done, if it's needed
			throw new NotImplementedException();
		}

		private void LoadDataButton_Click(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
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
