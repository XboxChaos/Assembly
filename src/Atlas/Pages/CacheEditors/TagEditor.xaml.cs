using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Atlas.Helpers.Tags;
using Atlas.ViewModels;
using Blamite.Util;

namespace Atlas.Pages.CacheEditors
{
	/// <summary>
	/// Interaction logic for TagEditor.xaml
	/// </summary>
	public partial class TagEditor : ICacheEditor
	{
		private CachePageViewModel _cachePageViewModel;
		private TagHierarchyNode _tagHierarchyNode;

		public string EditorTitle
		{
			get { return _editorTitle; }
			private set { SetField(ref _editorTitle, value); }
		}
		private string _editorTitle;
		private const string EditorFormat = "Tag - {0}.{1}";

		public TagEditor(CachePageViewModel cachePageViewModel, TagHierarchyNode tagHierarchyNode)
		{
			DataContext = _cachePageViewModel = cachePageViewModel;
			_tagHierarchyNode = tagHierarchyNode;
			EditorTitle = string.Format(EditorFormat, _tagHierarchyNode.Name, CharConstant.ToString(_tagHierarchyNode.TagClass.Magic));

			InitializeComponent();
		}

		public bool Close()
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
