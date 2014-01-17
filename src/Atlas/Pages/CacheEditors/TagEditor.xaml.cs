using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
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
			InitializeComponent();

			DataContext = ViewModel = new TagEditorViewModel(cachePageViewModel, tagHierarchyNode, this);
			EditorTitle = string.Format(EditorFormat, ViewModel.TagHierarchyNode.Name);
			
			// Set Option boxes
			ShowCommentsToggleButton.DataContext =
				ShowEnumIndicesToggleButton.DataContext =
				ShowInvisiblesToggleButton.DataContext = 
				ShowTagBlockInfoToggleButton.DataContext = App.Storage.Settings;

			ViewModel.LoadTagData(TagDataReader.LoadType.File, this);
		}

		public bool Close()
		{
			if (ViewModel.FieldChanges.Any())
				throw new InvalidOperationException();

			return true;
		}

		#region Data Search Events

		private void NextSearchTagDataButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (DataSearchResultsComboBox.SelectedIndex > ViewModel.SearchResults.Count - 1)
				ViewModel.SelectResult(ViewModel.SearchResults[DataSearchResultsComboBox.SelectedIndex + 1]);
		}
		private void PreviousSearchTagDataButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (DataSearchResultsComboBox.SelectedIndex > 0)
				ViewModel.SelectResult(ViewModel.SearchResults[DataSearchResultsComboBox.SelectedIndex - 1]);
		}
		private void TagDataViewer_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ViewModel.SearchResults.Any())
			{
				var field = TagDataViewer.SelectedItem as TagDataField;
				if (field == null || e.RemovedItems.Count <= 0 || ViewModel.FindResultByListField(field) != -1) return;

				// Disallow selecting filtered items and reflexive wrappers
				// as long as this wouldn't cause an infinite loop of selection changes
				var oldField = e.RemovedItems[0] as TagDataField;
				if (oldField != null && ViewModel.FindResultByListField(oldField) != -1)
				{
					TagDataViewer.SelectedItem = oldField;
				}
				else
					TagDataViewer.SelectedItem = null;
			}
			else
				TagDataViewer.SelectedItem = null;
		}
		private void DataSearchResultsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var selectedResult = ViewModel.SelectedSearchResult;
			if (selectedResult != null)
				ViewModel.SelectResult(selectedResult);
		}

		#endregion

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
