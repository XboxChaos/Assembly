using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using Assembly.Helpers;

namespace Assembly.Metro.Controls.PageTemplates
{
	/// <summary>
	///     Interaction logic for SettingsPage.xaml
	/// </summary>
	public partial class SettingsPage
	{
		public SettingsPage()
		{
			InitializeComponent();

			// Set DataContext
			DataContext = App.AssemblyStorage.AssemblySettings;

			// Setup Combo Boxes
			ComboBoxAccents.ItemsSource = Enum.GetValues(typeof (Settings.Accents));
			ComboBoxMapInfoDockSide.ItemsSource = Enum.GetValues(typeof (Settings.MapInfoDockSide));
			ComboBoxMapTagSort.ItemsSource = Enum.GetValues(typeof (Settings.TagSort));

			// Load UI
			btnTabSelection_Clicked(BtnSelectGeneral, null);
		}

		#region TabSelection

		private string _currentTag = "";

		private void btnTabSelection_Clicked(object sender, RoutedEventArgs e)
		{
			//if (_isActive) return;

			var button = (ToggleButton) sender;
			if (button == null || button.IsChecked == null) return;
			if (_currentTag == button.Tag.ToString()) return;

			// Get Current Tab
			string currentTabTag = _currentTag;

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
		}

		private void SetAllToDisbaled()
		{
			BtnSelectGeneral.IsChecked = false;
			BtnSelectXboxDev.IsChecked = false;
			BtnSelectMapEdit.IsChecked = false;
			BtnSelectPlugins.IsChecked = false;
			BtnSelectStrtpge.IsChecked = false;
		}

		#endregion

		#region Inline Helpers

		private void btnAutoSaveScreenshotDirectory_Click(object sender, RoutedEventArgs e)
		{
			var fbd = new FolderBrowserDialog
			{
				ShowNewFolderButton = true,
				SelectedPath = txtAutoSaveDirectory.Text,
				Description = "Select the folder you would like to auto-save screenshots in"
			};
			if (fbd.ShowDialog() == DialogResult.OK)
				App.AssemblyStorage.AssemblySettings.XdkScreenshotPath = fbd.SelectedPath;
		}

		private void sliderXDKScreenGammaModifier_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			LblXdkScreenGammaValue.Text = string.Format("Gamma ({0}):", e.NewValue);
		}

		private void btnBrowseXsd_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog()
			{
				Filter = "Executable Files|*.exe|All Files|*.*",
				FileName = App.AssemblyStorage.AssemblySettings.XsdPath,
				Title = "Open xsd.exe"
			};
			if (dialog.ShowDialog() == DialogResult.OK)
				App.AssemblyStorage.AssemblySettings.XsdPath = dialog.FileName;
		}
		#endregion

		public bool Close()
		{
			return true;
		}
	}
}