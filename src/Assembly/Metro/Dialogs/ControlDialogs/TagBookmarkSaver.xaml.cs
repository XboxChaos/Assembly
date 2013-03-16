using System.Collections.Generic;
using System.Windows;
using Assembly.Helpers.Native;
using System.Windows.Controls;
using Assembly.Helpers;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	/// Interaction logic for TagBookmarkSaver.xaml
	/// </summary>
	public partial class TagBookmarkSaver
	{
		public TagBookmarkSaver()
		{
			InitializeComponent();
			DwmDropShadow.DropShadowToWindow(this);
		}

		private void btnOkay_Click(object sender, RoutedEventArgs e)
		{
			var keypair = new KeyValuePair<string, int>(null, -1);
			var sfd = new System.Windows.Forms.SaveFileDialog
				          {
					          Title = "Assembly - Select where to save your Tag Bookmarks",
					          Filter = "Assembly Tag Bookmark File (*.astb)|*.astb"
				          };
			if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				keypair = new KeyValuePair<string, int>(sfd.FileName, cbItems.SelectedIndex);

			// Save keypair to TempStorage
			TempStorage.TagBookmarkSaver = keypair;

			Close();
		}

		private void cbItems_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (cbItems.SelectedItem == null) return;

			var item = cbItems.SelectedItem as ComboBoxItem;
			if (item != null && txtSelectedItemSummary != null) 
				txtSelectedItemSummary.Text = item.Tag.ToString();
		}
	}
}
