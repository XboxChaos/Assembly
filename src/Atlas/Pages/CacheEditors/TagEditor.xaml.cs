using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Atlas.Helpers.Tags;
using Atlas.Pages.CacheEditors.TagEditorComponents.Data;
using Atlas.ViewModels;
using Atlas.ViewModels.CacheEditors;

namespace Atlas.Pages.CacheEditors
{
	/// <summary>
	/// Interaction logic for TagEditor.xaml
	/// </summary>
	public partial class TagEditor : ICacheEditor
	{
		public TagEditorViewModel ViewModel { get; private set; }

		public bool IsSingleInstance { get { return false; } }

		public string EditorTitle
		{
			get { return _editorTitle; }
			private set { SetField(ref _editorTitle, value); }
		}
		private string _editorTitle;
		private const string EditorFormat = "Tag - {0}";

		public TagEditor(CachePageViewModel cachePageViewModel, TagHierarchyNode tagHierarchyNode)
		{
			DataContext = ViewModel = new TagEditorViewModel(cachePageViewModel, tagHierarchyNode);
			EditorTitle = string.Format(EditorFormat, ViewModel.TagHierarchyNode.Name);

			InitializeComponent();

			// Set Option boxes
			ShowEnumIndicesMenuItem.DataContext =
				ShowBlockInformationMenuItem.DataContext =
				ShowCommentsMenuItem.DataContext = 
				ShowInvisiblesMenuItem.DataContext = App.Storage.Settings;

			ViewModel.LoadTagData(TagDataReader.LoadType.File, this);
		}

		public bool Close()
		{
			if (ViewModel.FieldChanges.Any())
				throw new InvalidOperationException();

			return true;
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
