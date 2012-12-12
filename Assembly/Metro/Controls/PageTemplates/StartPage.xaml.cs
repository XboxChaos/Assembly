using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Assembly.Backend;
using Assembly.Metro.Dialogs;
using Assembly.Windows;

namespace Assembly.Metro.Controls.PageTemplates
{
    /// <summary>
    /// Interaction logic for StartPage.xaml
    /// </summary>
    public partial class StartPage : UserControl
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
            Settings.RecentFileEntry senderEntry = (Settings.RecentFileEntry)((Button)sender).Tag;

            if (senderEntry != null)
                if (!File.Exists(senderEntry.FilePath))
                {
                    if (MetroMessageBox.Show("File can't be found", "That file can't be found on your Computer. Would you like it to be removed from the recents list?", MetroMessageBox.MessageBoxButtons.YesNo) == MetroMessageBox.MessageBoxResult.Yes)
                    {
                        RecentFiles.RemoveEntry(senderEntry);
                        UpdateRecents();
                    }
                    return;
                }
                else
                    if (senderEntry.FileType == Settings.RecentFileType.Cache)
                        Settings.homeWindow.AddCacheTabModule(senderEntry.FilePath);
                    else if (senderEntry.FileType == Settings.RecentFileType.BLF)
                        Settings.homeWindow.AddImageTabModule(senderEntry.FilePath);
                    else if (senderEntry.FileType == Settings.RecentFileType.MapInfo)
                        Settings.homeWindow.AddInfooTabModule(senderEntry.FilePath);
                    else
                        MetroMessageBox.Show("wut.", "This content type doesn't even exist, how the fuck did you manage that?");
        }
        public void UpdateRecents()
        {
            panelRecents.Children.Clear();

            if (Settings.applicationRecents != null)
            {
                int recentsCount = 0;
                foreach (Settings.RecentFileEntry entry in Settings.applicationRecents)
                {
                    if (recentsCount > 9)
                        break;

                    Button btnRecent = new Button();
                    btnRecent.Tag = entry;
                    btnRecent.Style = (Style)FindResource("TabActiveButtons");
                    btnRecent.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                    btnRecent.ToolTip = entry.FilePath;
                    btnRecent.Click += LoadRecentItem;

                    if (entry.FileType == Settings.RecentFileType.Cache && Settings.startpageShowRecentsMap)
                    {
                        btnRecent.Content = string.Format("{0} - {1}", entry.FileGame, entry.FileName.Replace("_", "__"));
                        panelRecents.Children.Add(btnRecent);
                    }
                    else if (entry.FileType == Settings.RecentFileType.BLF && Settings.startpageShowRecentsBLF)
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
                panelRecents.Children.Add(new TextBlock()
                {
                    Text = "It's lonely in here, get modding ;)",
                    Style = (Style)FindResource("GenericTextblock"),
                    Margin = new Thickness(20, 0, 0, 0)
                });
        }

        #region Settings Modification
        private void cbClosePageOnLoad_Update(object sender, RoutedEventArgs e)
        {
            Settings.startpageHideOnLaunch = (bool)cbClosePageOnLoad.IsChecked;
            Settings.UpdateSettings();
        }
        private void cbShowOnStartUp_Update(object sender, RoutedEventArgs e)
        {
            Settings.startpageShowOnLoad = (bool)cbShowOnStartUp.IsChecked;
            Settings.UpdateSettings();
        }
        #endregion
    }
}
