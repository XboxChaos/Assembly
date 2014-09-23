using System.Diagnostics;
using System.Web;
using System.Windows;
using Assembly.Helpers.Native;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for TagBlockEditor.xaml
	/// </summary>
	public partial class TagBlockEditor
	{
		private ReflexiveData _tagBlock;
		private MetaReader _reader;
		private ObservableCollection<MetaField> _previewFields = new ObservableCollection<MetaField>();

		public TagBlockEditor(ReflexiveData tagBlock, MetaReader reader)
		{
			_tagBlock = tagBlock;
			_reader = reader;

			InitializeComponent();
			LoadInfo();
			LoadPreviewFields();
			LoadEntryList();
		}

		private void LoadInfo()
		{
			lblTitle.Text += " - " + _tagBlock.Name;
		}

		private void LoadPreviewFields()
		{
			// Duplicate the tag block's template to use for preview fields
			foreach (var field in _tagBlock.Template)
				_previewFields.Add(field.CloneValue());

			panelMetaComponents.ItemsSource = _previewFields;
		}

		private void LoadEntryList()
		{
			foreach (var entry in _tagBlock.Pages)
				entryList.Items.Add(new ListBoxItem() { Content = string.Format("{0} - {1} ({2})", entry.Index, _tagBlock.LastChunkIndex, _tagBlock.Length) });
		}
	}
}