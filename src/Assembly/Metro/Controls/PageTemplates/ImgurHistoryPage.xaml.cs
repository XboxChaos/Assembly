using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;

namespace Assembly.Metro.Controls.PageTemplates
{
	/// <summary>
	///     Interaction logic for ImgurHistoryPage.xaml
	/// </summary
	public partial class ImgurHistoryPage
	{
		public ImgurHistoryPage()
		{
			InitializeComponent();

			UpdateHistory();
		}

		public bool Close()
		{
			return true;
		}

		public void UpdateHistory()
		{
			dataPastUploads.Items.Clear();

			foreach (var entry in App.AssemblyStorage.AssemblySettings.ImgurUploadHistory)
			{
				dataPastUploads.Items.Add(entry);
			}
		}

		private void HistoryDelete_Click(object sender, RoutedEventArgs e)
		{
			var senderEntry = (Settings.ImgurHistoryEntry)((Button)sender).DataContext;

			if (senderEntry == null) return;

			ImgurHistory.RemoveEntry(senderEntry);

			UpdateHistory();
		}

		private void URL_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
				Clipboard.SetText(((TextBlock)e.OriginalSource).Text);
		}

		private void ClearAll_Click(object sender, RoutedEventArgs e)
		{
			if (MetroMessageBox.Show("Clear All History",
		"Are you sure you want to clear ALL archived uploads? This action cannot be undone.",
		MetroMessageBox.MessageBoxButtons.YesNo) != MetroMessageBox.MessageBoxResult.Yes) return;
			ImgurHistory.ClearEntries();

			UpdateHistory();
		}
	}
}