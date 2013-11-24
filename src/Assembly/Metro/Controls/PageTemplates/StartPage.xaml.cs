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

            cbClosePageOnLoad.IsChecked = Settings.startpageHideOnLaunch;
            cbShowOnStartUp.IsChecked = Settings.startpageShowOnLoad;

            // Load RSS feeds
            tutHalo3.Content = new Games.Components.RssPage(HoldingVault.XboxChaosH3Tuts);
            tutHaloReach.Content = new Games.Components.RssPage(HoldingVault.XboxChaosHReachTuts);
        }

        public bool Close() { return true; }

        #region Open Types of Cache Files
        private void btnOpenCacheFile_Click(object sender, RoutedEventArgs e)
        {
            Settings.homeWindow.OpenContentFile(Windows.Home.ContentTypes.Map);
        }

        private void btnOpenCacheInfo_Click(object sender, RoutedEventArgs e)
        {
            Settings.homeWindow.OpenContentFile(Windows.Home.ContentTypes.MapInfo);
        }

        private void btnOpenCacheImag_Click(object sender, RoutedEventArgs e)
        {
            Settings.homeWindow.OpenContentFile(Windows.Home.ContentTypes.MapImage);
        }
        #endregion

        public void LoadRecentItem(object sender, RoutedEventArgs e)
        {
            var senderEntry = (Settings.RecentFileEntry)((Button)sender).Tag;

            if (senderEntry != null)
                if (!File.Exists(senderEntry.FilePath))
                {
                    if (MetroMessageBox.Show("File Not Found", "That file can't be found on your computer. Would you like it to be removed from the recents list?", MetroMessageBox.MessageBoxButtons.YesNo) == MetroMessageBox.MessageBoxResult.Yes)
                    {
                        RecentFiles.RemoveEntry(senderEntry);
                        UpdateRecents();
                    }
                }
                else
                    switch (senderEntry.FileType)
                    {
	                    case Settings.RecentFileType.Cache:
		                    Settings.homeWindow.AddCacheTabModule(senderEntry.FilePath);
		                    break;
	                    case Settings.RecentFileType.Blf:
		                    Settings.homeWindow.AddImageTabModule(senderEntry.FilePath);
		                    break;
	                    case Settings.RecentFileType.MapInfo:
		                    Settings.homeWindow.AddInfooTabModule(senderEntry.FilePath);
		                    break;
	                    default:
		                    MetroMessageBox.Show("wut.", "This content type doesn't even exist, how the fuck did you manage that?");
		                    break;
                    }
        }
        public void UpdateRecents()
        {
            panelRecents.Children.Clear();

            if (Settings.applicationRecents != null)
            {
                var recentsCount = 0;
                foreach (var entry in Settings.applicationRecents)
                {
                    if (recentsCount > 9)
                        break;

                    var btnRecent = new Button
	                                    {
		                                    Tag = entry,
		                                    Style = (Style) FindResource("TabActiveButtons"),
		                                    HorizontalAlignment = HorizontalAlignment.Stretch,
		                                    ToolTip = entry.FilePath
	                                    };
	                btnRecent.Click += LoadRecentItem;

                    if (entry.FileType == Settings.RecentFileType.Cache && Settings.startpageShowRecentsMap)
                    {
                        btnRecent.Content = string.Format("{0} - {1}", entry.FileGame, entry.FileName.Replace("_", "__"));
                        panelRecents.Children.Add(btnRecent);
                    }
                    else if (entry.FileType == Settings.RecentFileType.Blf && Settings.startpageShowRecentsBLF)
                    {
                        btnRecent.Content = string.Format("Map Image - {0}", entry.FileName.Replace("_", "__"));
                        panelRecents.Children.Add(btnRecent);
                    }
                    else if (entry.FileType == Settings.RecentFileType.MapInfo && Settings.startpageShowRecentsMapInfo)
                    {
                        btnRecent.Content = string.Format("Map Info - {0}", entry.FileName.Replace("_", "__"));
                        panelRecents.Children.Add(btnRecent);
                    }

                    recentsCount++;
                }
            }
            else if (Settings.applicationRecents == null || Settings.applicationRecents.Count == 0)
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
	        if (cbClosePageOnLoad.IsChecked != null) Settings.startpageHideOnLaunch = (bool)cbClosePageOnLoad.IsChecked;
	        Settings.UpdateSettings();
        }

	    private void cbShowOnStartUp_Update(object sender, RoutedEventArgs e)
	    {
		    if (cbShowOnStartUp.IsChecked != null) Settings.startpageShowOnLoad = (bool)cbShowOnStartUp.IsChecked;
		    Settings.UpdateSettings();
	    }

	    #endregion
    }
}
