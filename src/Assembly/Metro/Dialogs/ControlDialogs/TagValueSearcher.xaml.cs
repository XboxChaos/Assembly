using System;
using System.Collections.ObjectModel;
using System.Windows;
using Assembly.Helpers.Tags;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for MessageBoxList.xaml
	/// </summary>
	public partial class TagValueSearcher
	{
        public TagHierarchyNode SelectedTag { get; private set; }
        public int SelectedTagIndex { get; private set; }
        private Collection<TagHierarchyNode> AvailableNodes { get; set; }

		public TagValueSearcher(TagHierarchyNode currentTag, Collection<TagHierarchyNode> availableNodes)
		{
			InitializeComponent();

            string tagClassName = currentTag.Parent.Name;
            Title = $"[{tagClassName}] Tag Search";
            lblTitle.Text = Title;

            SelectedTag = currentTag;
            AvailableNodes = availableNodes;

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
            foreach (TagHierarchyNode node in AvailableNodes)
                if (node.Name.ToString().ToLower().Contains(textSearch.Text.ToLower()))
                    listTagSearchResults.Items.Add(node);
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
            SelectedTag = (TagHierarchyNode)listTagSearchResults.SelectedItem;
            SelectedTagIndex = AvailableNodes.IndexOf(SelectedTag);
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