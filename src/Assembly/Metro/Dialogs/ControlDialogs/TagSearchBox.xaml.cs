using System;
using System.Collections.ObjectModel;
using System.Windows;
using Assembly.Metro.Controls.PageTemplates.Games;
using System.Text.RegularExpressions;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for MessageBoxList.xaml
	/// </summary>
	public partial class TagValueSearcher
	{
		public int SelectedTagIndex { get; private set; }
		public TagEntry SelectedTag { get; private set; }
		private Collection<TagEntry> AvailableTags { get; set; }
		private TagGroup TagGroup { get; set; }

		public TagValueSearcher(TagGroup tagGroup)
		{
			InitializeComponent();
			TagGroup = tagGroup;

			string tagGroupName = tagGroup.TagGroupMagic;
			Title = $"[{tagGroupName}] Tag Search";
			lblTitle.Text = Title;

			UpdateSearchResults();

		}

		private void btnOK_Click(object sender, RoutedEventArgs e)
		{
			ExitSuccessfully();
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void UpdateSearchResults()
		{
			listTagSearchResults.Items.Clear();
			foreach (TagEntry tag in TagGroup.Children)
				if (tag != null)
				{
					if (textSearch.Text.StartsWith("0x"))
					{
						string searchHex = textSearch.Text.Substring(2);
						if (tag.RawTag.Index.ToString().ToLowerInvariant().Substring(2).Contains(searchHex))
						{
							listTagSearchResults.Items.Add(tag);
							continue;
						}	
					}
					if (tag.TagFileName.ToLowerInvariant().Contains(textSearch.Text.ToLowerInvariant()))
						listTagSearchResults.Items.Add(tag);
				}
		}

		private void listTagSearchResults_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			UpdateSelectedTag();
		}

		private void textSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			UpdateSearchResults();
		}

		private void UpdateSelectedTag()
		{
			SelectedTag = (TagEntry)listTagSearchResults.SelectedItem;
			SelectedTagIndex = TagGroup.Children.IndexOf(SelectedTag);
		}

		private void ExitSuccessfully()
		{
			DialogResult = true;
			Close();
		}

		private void listTagSearchResults_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (listTagSearchResults.SelectedItem != null)
			{
				UpdateSelectedTag();
				ExitSuccessfully();
			}
		}
	}
}