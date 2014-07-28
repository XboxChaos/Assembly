using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Atlas.Dialogs;
using Atlas.Helpers.Tags;
using Atlas.ViewModels;
using Atlas.ViewModels.Cache;
using Atlas.Views.Cache.TagEditorComponents.Data;

namespace Atlas.Views.Cache
{
	/// <summary>
	/// Interaction logic for TagEditor.xaml
	/// </summary>
	public partial class TagEditor : ICacheEditor
	{
		public static RoutedCommand ViewValueAsCommand = new RoutedCommand();
		public static RoutedCommand GoToPluginCommand = new RoutedCommand();

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
			TagEditorRowDefinition.DataContext =
				PluginEditorRowDefinition.DataContext =
				
				ShowCommentsToggleButton.DataContext =
				ShowEnumIndicesToggleButton.DataContext =
				ShowInvisiblesToggleButton.DataContext = 
				ShowTagBlockInfoToggleButton.DataContext = App.Storage.Settings;

			ViewModel.LoadTagData(TagDataReader.LoadType.File, this);

			// Global Event Handling
			AddHandler(Keyboard.KeyDownEvent, (KeyEventHandler)HandleKeyDownEvent);
		}

		public bool Close()
		{
			if (ViewModel.FieldChanges.Any())
				throw new InvalidOperationException();

			App.Storage.HomeWindowViewModel.AddRecentEditor(ViewModel.CachePageViewModel, this, ViewModel.TagHierarchyNode, ViewModel.TagHierarchyNode.FullPath);

			return true;
		}

		#region Events

		private void HandleKeyDownEvent(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
				StartSearching();

			if (e.Key == Key.Escape)
				DataSearchResetButton_Click(null, null);

			if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
				ViewModel.SaveTagData(TagDataWriter.SaveType.File);

			if (e.Key == Key.P && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
				ViewModel.SaveTagData(TagDataWriter.SaveType.Memory);
		}

		private void SearchQueryTextBox_OnKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter && e.Key != Key.Return) return;

			if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
				PreviousSearchTagDataButton_Click(null, null);
			else
				NextSearchTagDataButton_Click(null, null);
		}

		private void PluginOptionToggleButton_OnChanged(object sender, RoutedEventArgs e)
		{
			ViewModel.LoadTagData(TagDataReader.LoadType.File, this);
		}

		#endregion

		#region Data Search Events

		private void StartSearching()
		{
			SearchBoxBorder.Visibility = Visibility.Visible;
			SearchQueryTextBox.Focus();
			SearchQueryTextBox.SelectAll();
		}

		private void DataSearchResetButton_Click(object sender, RoutedEventArgs e)
		{
			SearchBoxBorder.Visibility = Visibility.Collapsed;
			ViewModel.SearchQuery = "";
			TagDataViewer.SelectedItem = null;
			TagDataViewer.Focus();
		}

		private void NextSearchTagDataButton_Click(object sender, RoutedEventArgs e)
		{
			if (DataSearchResultsComboBox.SelectedIndex < ViewModel.SearchResults.Count - 1)
				ViewModel.SelectResult(ViewModel.SearchResults[DataSearchResultsComboBox.SelectedIndex + 1]);
		}
		private void PreviousSearchTagDataButton_Click(object sender, RoutedEventArgs e)
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
		
		#region Tag Data Viewer Helpers

		private void GoToPlugin_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			var field = GetWrappedField(e.Source);
			e.CanExecute = (field != null && field.PluginLine > 0);
		}

		private void GoToPlugin_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var field = GetWrappedField(e.Source);
			if (field == null) return;

			var line = (int) field.PluginLine;
			var selectedLineDetails = PluginTextEditor.Document.GetLineByNumber(line);

			if (App.Storage.Settings.TagEditorPluginGridLength.Value <= 1.0)
			{
				App.Storage.Settings.TagEditorGridLength = new GridLength(0.7, GridUnitType.Star);
				App.Storage.Settings.TagEditorPluginGridLength = new GridLength(0.3, GridUnitType.Star);
			}

			PluginTextEditor.ScrollToLine(line);
			PluginTextEditor.Select(selectedLineDetails.Offset, selectedLineDetails.Length);
			PluginTextEditor.Focus();
		}

		private void ViewValueAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			var field = GetValueField(e.Source);
			e.CanExecute = (field != null);
		}

		private void ViewValueAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var field = GetValueField(e.Source);
			if (field == null) return;

			var viewValueAsFieldList = ViewModel.LoadViewValueAsPlugin();
			var offset = (uint)ViewModel.CachePageViewModel.CacheFile.MetaArea.PointerToOffset(field.FieldAddress);
			MetroViewValueAs.Show(ViewModel.CachePageViewModel, offset, viewValueAsFieldList);
		}

		private static TagDataField GetWrappedField(TagDataField field)
		{
			while (true)
			{
				var wrapper = field as WrappedTagBlockEntry;
				if (wrapper == null)
					return field;
				field = wrapper.WrappedField;
			}
		}
		private static TagDataField GetWrappedField(object elem)
		{
			// Get the FrameworkElement
			var source = elem as FrameworkElement;
			if (source == null)
				return null;

			// Get the field
			var field = source.DataContext as TagDataField;
			return field == null ? null : GetWrappedField(field);
		}

		/// <summary>
		///     Given a source element, retrieves the ValueField it represents.
		/// </summary>
		/// <param name="elem">The FrameworkElement to get the ValueField for.</param>
		/// <returns>The ValueField if elem's data context is set to one, or null otherwise.</returns>
		private static ValueField GetValueField(object elem)
		{
			var field = GetWrappedField(elem);
			var valueField = field as ValueField;
			if (valueField != null) return valueField;

			var wrapper = field as WrappedTagBlockEntry;
			if (wrapper != null)
				valueField = GetWrappedField(wrapper) as ValueField;

			return valueField;
		}

		#endregion

		#region Plugin Revision

		private void ShowPluginRevisionButton_OnClick(object sender, RoutedEventArgs e)
		{
			MetroPluginRevisionViewer.Show(ViewModel.TagHierarchyNode.Name, ViewModel.PluginVisitor.PluginRevisions);
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

		#region Split Helpers

		private bool _tagEditorExpanded;
		private void TagEditorButton_OnClick(object sender, RoutedEventArgs e)
		{
			if (_tagEditorExpanded)
			{
				_tagEditorExpanded = false;
				return;
			}

			App.Storage.Settings.TagEditorGridLength = new GridLength(0.7, GridUnitType.Star);
			App.Storage.Settings.TagEditorPluginGridLength = new GridLength(0.3, GridUnitType.Star);
		}
		private void TagEditorButton_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			_tagEditorExpanded = true;
			App.Storage.Settings.TagEditorGridLength = new GridLength(1.0, GridUnitType.Star);
			App.Storage.Settings.TagEditorPluginGridLength = new GridLength(0.0, GridUnitType.Star);
		}

		private bool _pluginEditorExpanded;
		private void PluginEditorButton_OnClick(object sender, RoutedEventArgs e)
		{
			if (_pluginEditorExpanded)
			{
				_pluginEditorExpanded = false;
				return;
			}

			App.Storage.Settings.TagEditorGridLength = new GridLength(0.3, GridUnitType.Star);
			App.Storage.Settings.TagEditorPluginGridLength = new GridLength(0.7, GridUnitType.Star);
		}
		private void PluginEditorButton_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			_pluginEditorExpanded = true;
			App.Storage.Settings.TagEditorPluginGridLength = new GridLength(1.0, GridUnitType.Star);
			App.Storage.Settings.TagEditorGridLength = new GridLength(0.0, GridUnitType.Star);
		}

		#endregion
	}
}
