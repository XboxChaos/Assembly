using System.IO;
using System.Windows;
using System.Windows.Controls;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;

namespace Assembly.Metro.Controls.PageTemplates
{
	/// <summary>
	/// Interaction logic for StartPage.xaml
	/// </summary>
	public partial class StartPage
	{
		public StartPage()
		{
			InitializeComponent();

			cbClosePageOnLoad.IsChecked = App.AssemblyStorage.AssemblySettings.StartpageHideOnLaunch;
			cbShowOnStartUp.IsChecked = App.AssemblyStorage.AssemblySettings.StartpageShowOnLoad;

			// Load RSS feeds
			tutHalo3.Content = new Games.Components.RssPage(HoldingVault.XboxChaosH3Tuts);
			tutHaloReach.Content = new Games.Components.RssPage(HoldingVault.XboxChaosHReachTuts);
		}

		public bool Close() { return true; }

		#region Open Types of Cache Files

		private void btnOpenCacheFile_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.OpenContentFile(Windows.Home.ContentTypes.Map);
		}

		private void btnOpenCacheInfo_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.OpenContentFile(Windows.Home.ContentTypes.MapInfo);
		}

		private void btnOpenCacheImag_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.OpenContentFile(Windows.Home.ContentTypes.MapImage);
		}

		#endregion

		public void LoadRecentItem(object sender, RoutedEventArgs e)
		{
			var senderEntry = (Settings.RecentFileEntry)((Button)sender).Tag;

			if (senderEntry == null) return;

			if (!File.Exists(senderEntry.FilePath))
			{
				if (
					MetroMessageBox.Show("File Not Found",
						"That file can't be found on your computer. Would you like it to be removed from the recents list?",
						MetroMessageBox.MessageBoxButtons.YesNo) != MetroMessageBox.MessageBoxResult.Yes) return;
				RecentFiles.RemoveEntry(senderEntry);
				UpdateRecents();
			}
			else
				switch (senderEntry.FileType)
				{
					case Settings.RecentFileType.Cache:
						App.AssemblyStorage.AssemblySettings.HomeWindow.AddCacheTabModule(senderEntry.FilePath);
						break;
					case Settings.RecentFileType.Blf:
						App.AssemblyStorage.AssemblySettings.HomeWindow.AddImageTabModule(senderEntry.FilePath);
						break;
					case Settings.RecentFileType.MapInfo:
						App.AssemblyStorage.AssemblySettings.HomeWindow.AddInfooTabModule(senderEntry.FilePath);
						break;
					default:
						MetroMessageBox.Show("wut.", "This content type doesn't even exist, how the fuck did you manage that?");
						break;
				}
		}
		public void UpdateRecents()
		{
			panelRecents.Children.Clear();

			if (App.AssemblyStorage.AssemblySettings.ApplicationRecents != null)
			{
				var recentsCount = 0;
				foreach (var entry in App.AssemblyStorage.AssemblySettings.ApplicationRecents)
				{
					if (recentsCount > 9)
						break;

					var btnRecent = new Button
										{
											Tag = entry,
											Style = (Style)FindResource("TabActiveButtons"),
											HorizontalAlignment = HorizontalAlignment.Stretch,
											ToolTip = entry.FilePath
										};
					btnRecent.Click += LoadRecentItem;

					if (entry.FileType == Settings.RecentFileType.Cache && App.AssemblyStorage.AssemblySettings.StartpageShowRecentsMap)
					{
						btnRecent.Content = string.Format("{0} - {1}", entry.FileGame, entry.FileName.Replace("_", "__"));
						panelRecents.Children.Add(btnRecent);
					}
					else if (entry.FileType == Settings.RecentFileType.Blf && App.AssemblyStorage.AssemblySettings.StartpageShowRecentsBlf)
					{
						btnRecent.Content = string.Format("Map Image - {0}", entry.FileName.Replace("_", "__"));
						panelRecents.Children.Add(btnRecent);
					}
					else if (entry.FileType == Settings.RecentFileType.MapInfo && App.AssemblyStorage.AssemblySettings.StartpageShowRecentsMapInfo)
					{
						btnRecent.Content = string.Format("Map Info - {0}", entry.FileName.Replace("_", "__"));
						panelRecents.Children.Add(btnRecent);
					}

					recentsCount++;
				}
			}
			else if (App.AssemblyStorage.AssemblySettings.ApplicationRecents == null || App.AssemblyStorage.AssemblySettings.ApplicationRecents.Count == 0)
				panelRecents.Children.Add(new TextBlock
				{
					Text = "It's lonely in here, get modding ;)",
					Style = (Style)FindResource("GenericTextblock"),
					Margin = new Thickness(20, 0, 0, 0)
				});
		}

		#region Settings Modification

		private void cbClosePageOnLoad_Update(object sender, RoutedEventArgs e)
		{
			if (cbClosePageOnLoad.IsChecked != null) App.AssemblyStorage.AssemblySettings.StartpageHideOnLaunch = (bool)cbClosePageOnLoad.IsChecked;
		}

		private void cbShowOnStartUp_Update(object sender, RoutedEventArgs e)
		{
			if (cbShowOnStartUp.IsChecked != null) App.AssemblyStorage.AssemblySettings.StartpageShowOnLoad = (bool)cbShowOnStartUp.IsChecked;
		}

		#endregion
	}
}
