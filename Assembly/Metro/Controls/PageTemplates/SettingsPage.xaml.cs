using System;
using System.Collections.Generic;
using System.Globalization;
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
using Assembly.Helpers;
using Assembly.Metro.Dialogs;

namespace Assembly.Metro.Controls.PageTemplates
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        private bool _settingsChanged = false;

        public SettingsPage()
        {
            InitializeComponent();

            #region Misc
            cbAccentSelector.Items.Clear();
            // Populate accents from enum in settings
            foreach (Settings.Accents accent in Enum.GetValues(typeof(Settings.Accents)))
                cbAccentSelector.Items.Add(accent.ToString());
            cbAccentSelector.SelectedIndex = (int)Settings.applicationAccent;

            cbEnableEggs.IsChecked = Settings.applicationEasterEggs;
            cbEnableEggs_Altered(null, null);
            #endregion

            #region XDK
            txtXBDMNameIP.Text = Settings.XDKNameIP;
            cbXDKFreeze.IsChecked = Settings.XDKScreenshotFreeze;
            cbXDKAutoSaveScreenshots.IsChecked = Settings.XDKAutoSave;
            txtAutoSaveDirectory.Text = Settings.XDKScreenshotPath;
            cbXDKScreenshotReszing.IsChecked = Settings.XDKResizeImages;
            txtXDKScreenshotWeight.Text = Settings.XDKResizeScreenshotWidth.ToString();
            txtXDKScreenshotHeight.Text = Settings.XDKResizeScreenshotHeight.ToString();
            cbXDKScreenGammaAdjust.IsChecked = Settings.XDKScreenshotGammaCorrect;
            sliderXDKScreenGammaModifier.Value = Settings.XDKScreenshotGammaModifier;
            lblXDKScreenGammaValue.Text = string.Format("Gamma ({0}):", Settings.XDKScreenshotGammaModifier);
            #endregion

            #region Start Page
            cbStartPageRecentMap.IsChecked = Settings.startpageShowRecentsMap;
            cbStartPageRecentBLF.IsChecked = Settings.startpageShowRecentsBLF;
            cbStartPageRecentMapInfo.IsChecked = Settings.startpageShowRecentsMapInfo;
            #endregion

            #region Halo Map
            cbTagSorting.SelectedIndex = (int)Settings.halomapTagSort;
            cbMapInfoPanel.SelectedIndex = (int)Settings.halomapMapInfoDockSide;
            #endregion

            #region Plugins
            cbPluginsShowComments.IsChecked = Settings.pluginsShowComments;
            #endregion

            #region Default File Types
            cbDefaultMapEditor.IsChecked = Settings.defaultMAP;
            cbDefaultBLFEditor.IsChecked = Settings.defaultBLF;
            cbDefaultMIFEditor.IsChecked = Settings.defaultMIF;
            #endregion
        }

        #region Real Time Accent Updateing
        private void cbAccentSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string theme = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Enum.Parse(typeof(Settings.Accents), ((Settings.Accents)cbAccentSelector.SelectedIndex).ToString()).ToString());
            try
            {
                App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Themes/" + theme + ".xaml", UriKind.Relative) });
            }
            catch
            {
                App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Themes/Blue.xaml", UriKind.Relative) });
            }

            SettingsChanged();
        }
        #endregion

        #region Settings Changed
        public void SettingsChanged() { _settingsChanged = true; }

        private void txtXBDMNameIP_TextChanged(object sender, TextChangedEventArgs e) { SettingsChanged(); }
        private void cbStartPageRecentMap_Checked(object sender, RoutedEventArgs e) { SettingsChanged(); }
        private void cbStartPageRecentMap_UnChecked(object sender, RoutedEventArgs e) { SettingsChanged(); }
        private void cbStartPageRecentBLF_Checked(object sender, RoutedEventArgs e) { SettingsChanged(); }
        private void cbStartPageRecentBLF_UnChecked(object sender, RoutedEventArgs e) { SettingsChanged(); }
        private void cbStartPageRecentMapInfo_Checked(object sender, RoutedEventArgs e) { SettingsChanged(); }
        private void cbStartPageRecentMapInfo_UnChecked(object sender, RoutedEventArgs e) { SettingsChanged(); }
        private void cbTagSorting_SelectionChanged(object sender, SelectionChangedEventArgs e) { SettingsChanged(); }
        private void txtPluginDirectory_TextChanged(object sender, TextChangedEventArgs e) { SettingsChanged(); }
        #endregion

        public bool Close()
        {
            if (_settingsChanged)
                if (MetroMessageBox.Show("Are you sure bro?", "Are you sure you want to exit? Someone told me you have un-saved settings.", MetroMessageBox.MessageBoxButtons.YesNo) == MetroMessageBox.MessageBoxResult.No)
                    return false;

            Settings.ApplyAccent();
            return true;
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            #region Misc
            Settings.applicationAccent = (Settings.Accents)cbAccentSelector.SelectedIndex;
            Settings.applicationEasterEggs = (bool)cbEnableEggs.IsChecked;
            #endregion

            #region XDK
            Settings.XDKNameIP = txtXBDMNameIP.Text;
            Settings.XDKAutoSave = (bool)cbXDKAutoSaveScreenshots.IsChecked;
            Settings.XDKScreenshotPath = txtAutoSaveDirectory.Text;
            Settings.XDKResizeImages = (bool)cbXDKScreenshotReszing.IsChecked;
            Settings.XDKResizeScreenshotHeight = int.Parse(txtXDKScreenshotHeight.Text);
            Settings.XDKResizeScreenshotWidth = int.Parse(txtXDKScreenshotWeight.Text);
            Settings.XDKScreenshotGammaCorrect = (bool)cbXDKScreenGammaAdjust.IsChecked;
            Settings.XDKScreenshotGammaModifier = sliderXDKScreenGammaModifier.Value;
            Settings.XDKScreenshotFreeze = (bool)cbXDKFreeze.IsChecked;
            #endregion

            #region Start Page
            Settings.startpageShowRecentsMap = (bool)cbStartPageRecentMap.IsChecked;
            Settings.startpageShowRecentsBLF = (bool)cbStartPageRecentBLF.IsChecked;
            Settings.startpageShowRecentsMapInfo = (bool)cbStartPageRecentMapInfo.IsChecked;
            #endregion

            #region Halo Map
            Settings.halomapTagSort = (Settings.TagSort)cbTagSorting.SelectedIndex;
            Settings.halomapMapInfoDockSide = (Settings.MapInfoDockSide)cbMapInfoPanel.SelectedIndex;
            #endregion

            #region Plugins
            Settings.pluginsShowComments = (bool)cbPluginsShowComments.IsChecked;
            #endregion

            #region Default File Types
            Settings.defaultMAP = (bool)cbDefaultMapEditor.IsChecked;
            Settings.defaultBLF = (bool)cbDefaultBLFEditor.IsChecked;
            Settings.defaultMIF = (bool)cbDefaultMIFEditor.IsChecked;
            #endregion

            Settings.UpdateSettings();

            if (MetroMessageBox.Show("Saved", "Your settings have been saved successfully. Do you want to close settings now?", MetroMessageBox.MessageBoxButtons.YesNo) == MetroMessageBox.MessageBoxResult.Yes)
                Settings.homeWindow.ExternalTabClose((TabItem)this.Parent);
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Settings.ApplyAccent();
            Settings.homeWindow.ExternalTabClose((TabItem)this.Parent);
        }

        private void btnAutoSaveScreenshotDirectory_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.ShowNewFolderButton = true;
            fbd.SelectedPath = txtAutoSaveDirectory.Text;
            fbd.Description = "Select the folder you would like to auto-save screenshots in";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                txtAutoSaveDirectory.Text = fbd.SelectedPath;
        }
        private void sliderXDKScreenGammaModifier_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblXDKScreenGammaValue.Text = string.Format("Gamma ({0}):", e.NewValue);
        }

        private void cbEnableEggs_Altered(object sender, RoutedEventArgs e)
        {
            if ((bool)cbEnableEggs.IsChecked)
                cbEnableEggs.Content = "Enable Easter Eggs ;)";
            else
                cbEnableEggs.Content = "Enable Easter Eggs :)";
        }
    }
}
