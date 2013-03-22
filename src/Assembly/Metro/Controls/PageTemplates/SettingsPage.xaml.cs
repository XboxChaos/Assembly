using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;
using System.Windows.Controls.Primitives;
using Assembly.Windows;
using AvalonDock.Layout;

namespace Assembly.Metro.Controls.PageTemplates
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage
    {
	    private bool _settingsChanged;

        public SettingsPage()
        {
            InitializeComponent();

            #region Misc
            cbAccentSelector.Items.Clear();
            // Populate accents from enum in settings
            foreach (Settings.Accents accent in Enum.GetValues(typeof(Settings.Accents)))
                cbAccentSelector.Items.Add(accent.ToString());
            cbAccentSelector.SelectedIndex = (int)Settings.applicationAccent;
	        cbCheckUpdatesOnStartup.IsChecked = Settings.applicationUpdateOnStartup;
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
			cbDefaultAMPEditor.IsChecked = Settings.defaultAMP;
            #endregion

			// Load UI
			btnTabSelection_Clicked(btnSelectGeneral, null);
        }

        #region Real Time Accent Updateing
        private void cbAccentSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var theme = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Enum.Parse(typeof(Settings.Accents), ((Settings.Accents)cbAccentSelector.SelectedIndex).ToString()).ToString());
            try
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Themes/" + theme + ".xaml", UriKind.Relative) });
            }
            catch
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Themes/Blue.xaml", UriKind.Relative) });
            }

	        _settingsChanged = (Settings.applicationAccent != (Settings.Accents)cbAccentSelector.SelectedIndex);
        }
        #endregion

        #region Settings Changed
		private void cbCheckUpdatesOnStartup_Modified(object sender, RoutedEventArgs e) { _settingsChanged = (Settings.applicationUpdateOnStartup != cbCheckUpdatesOnStartup.IsChecked); }

        private void txtXBDMNameIP_TextChanged(object sender, TextChangedEventArgs e) { _settingsChanged = (Settings.XDKNameIP != txtXBDMNameIP.Text); }
		private void cbXDKFreeze_Modified(object sender, RoutedEventArgs e) { _settingsChanged = (Settings.XDKScreenshotFreeze != (bool) cbXDKFreeze.IsChecked); }
		private void cbXDKAutoSaveScreenshots_Modified(object sender, RoutedEventArgs e) { _settingsChanged = (Settings.XDKAutoSave != (bool)cbXDKAutoSaveScreenshots.IsChecked); }
		private void txtAutoSaveDirectory_TextChanged(object sender, TextChangedEventArgs e) { _settingsChanged = (Settings.XDKScreenshotPath != txtAutoSaveDirectory.Text); }
		private void cbXDKScreenshotReszing_Modified(object sender, RoutedEventArgs e) { _settingsChanged = (Settings.XDKResizeImages != (bool)cbXDKScreenshotReszing.IsChecked); }
		private void txtXDKScreenshotWeight_TextChanged(object sender, TextChangedEventArgs e)
		{
			var width = 0;
			int.TryParse(txtXDKScreenshotWeight.Text, out width);
			_settingsChanged = (Settings.XDKResizeScreenshotWidth != width);
		}
		private void txtXDKScreenshotHeight_TextChanged(object sender, TextChangedEventArgs e)
		{
			var height = 0;
			int.TryParse(txtXDKScreenshotHeight.Text, out height);
			_settingsChanged = (Settings.XDKResizeScreenshotHeight != height);
		}
		private void cbXDKScreenGammaAdjust_Modified(object sender, RoutedEventArgs e)	{ _settingsChanged = (Settings.XDKScreenshotGammaCorrect != (bool)cbXDKScreenGammaAdjust.IsChecked); }

		private void cbMapInfoPanel_SelectionChanged(object sender, SelectionChangedEventArgs e)	{ _settingsChanged = (Settings.halomapMapInfoDockSide != (Settings.MapInfoDockSide)cbMapInfoPanel.SelectedIndex); }

		private void cbPluginsShowComments_Modified(object sender, RoutedEventArgs e)		{ _settingsChanged = (Settings.pluginsShowComments != (bool)cbPluginsShowComments.IsChecked); }

		private void cbStartPageRecentMap_Modified(object sender, RoutedEventArgs e)		{ _settingsChanged = (Settings.startpageShowRecentsMap != (bool)cbStartPageRecentMap.IsChecked); }
		private void cbStartPageRecentBLF_Modified(object sender, RoutedEventArgs e)		{ _settingsChanged = (Settings.startpageShowRecentsBLF != (bool)cbStartPageRecentBLF.IsChecked); }
		private void cbStartPageRecentMapInfo_Modified(object sender, RoutedEventArgs e)	{ _settingsChanged = (Settings.startpageShowRecentsMapInfo != (bool)cbStartPageRecentMapInfo.IsChecked); }

		private void cbDefaultMapEditor_Modified(object sender, RoutedEventArgs e)	{ _settingsChanged = (Settings.defaultMAP != (bool)cbDefaultMapEditor.IsChecked); }
		private void cbDefaultBLFEditor_Modified(object sender, RoutedEventArgs e)	{ _settingsChanged = (Settings.defaultBLF != (bool)cbDefaultBLFEditor.IsChecked); }
		private void cbDefaultMIFEditor_Modified(object sender, RoutedEventArgs e)	{ _settingsChanged = (Settings.defaultMIF != (bool)cbDefaultMIFEditor.IsChecked); }
		private void cbDefaultAMPEditor_Modified(object sender, RoutedEventArgs e)	{ _settingsChanged = (Settings.defaultAMP != (bool) cbDefaultAMPEditor.IsChecked); }

		private void cbTagSorting_SelectionChanged(object sender, SelectionChangedEventArgs e)	{ _settingsChanged = (Settings.halomapTagSort != (Settings.TagSort)cbTagSorting.SelectedIndex); }
        #endregion

		#region TabSelection
		private void btnTabSelection_Clicked(object sender, RoutedEventArgs e)
		{
			//if (_isActive) return;

			var button = (ToggleButton) sender;
			if (button == null || button.IsChecked == null)
			{
				_isActive = false;
				return;
			}
			if (_currentTag != button.Tag.ToString())
			{
				_isActive = true;

				// Get Current Tab
				var currentTabTag = _currentTag;

				// Disable all old buttons
				SetAllToDisbaled();

				// Update UI
				button.IsChecked = true;
				_currentTag = button.Tag.ToString();

				// Apply Storyboard
				var storyboardHide = (Storyboard) TryFindResource(string.Format("Hide{0}Tab", currentTabTag));
				var storyboardShow = (Storyboard) TryFindResource(string.Format("Show{0}Tab", button.Tag));

				if (storyboardHide != null) storyboardHide.Begin();
				if (storyboardShow != null) storyboardShow.Begin();
				if (storyboardShow != null) storyboardShow.Completed += (sender1, eventArgs) => _isActive = false;
			}
			else
				_isActive = false;
		}

	    private bool _isActive;
	    private string _currentTag = "";
		private void SetAllToDisbaled()
		{
			btnSelectGeneral.IsChecked = false;
			btnSelectXboxDev.IsChecked = false;
			btnSelectMapEdit.IsChecked = false;
			btnSelectPlugins.IsChecked = false;
			btnSelectStrtpge.IsChecked = false;
		}
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
	        Settings.applicationUpdateOnStartup = (bool) cbCheckUpdatesOnStartup.IsChecked;
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
	        Settings.defaultAMP = (bool)cbDefaultAMPEditor.IsChecked;
            #endregion

            Settings.UpdateSettings();

            if (MetroMessageBox.Show("Saved", "Your settings have been saved successfully. Do you want to close settings now?", MetroMessageBox.MessageBoxButtons.YesNo) == MetroMessageBox.MessageBoxResult.Yes)
				Settings.homeWindow.ExternalTabClose(Home.TabGenre.Settings);
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Settings.ApplyAccent();
	        Settings.homeWindow.ExternalTabClose(Home.TabGenre.Settings);
        }

        private void btnAutoSaveScreenshotDirectory_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog
	                      {
		                      ShowNewFolderButton = true,
		                      SelectedPath = txtAutoSaveDirectory.Text,
		                      Description = "Select the folder you would like to auto-save screenshots in"
	                      };
	        if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                txtAutoSaveDirectory.Text = fbd.SelectedPath;
        }
        private void sliderXDKScreenGammaModifier_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblXDKScreenGammaValue.Text = string.Format("Gamma ({0}):", e.NewValue);

	        _settingsChanged = (!Settings.XDKScreenshotGammaModifier.Equals(e.NewValue));
        }

		private void lblHeader_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			_0xabad1dea.GayRats.CheckIfGayRatsExist();
		}
    }
}