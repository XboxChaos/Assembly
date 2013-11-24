using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Assembly.Helpers;
using System.Windows.Controls.Primitives;

namespace Assembly.Metro.Controls.PageTemplates
{
	/// <summary>
	/// Interaction logic for SettingsPage.xaml
	/// </summary>
	public partial class SettingsPage
	{
		public SettingsPage()
		{
			InitializeComponent();

			// Set DataContext
			DataContext = App.AssemblyStorage.AssemblySettings;

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
		}
		#endregion

		#region TabSelection
		private void btnTabSelection_Clicked(object sender, RoutedEventArgs e)
		{
			//if (_isActive) return;

			var button = (ToggleButton)sender;
			if (button == null || button.IsChecked == null) return;
			if (_currentTag == button.Tag.ToString()) return;

			// Get Current Tab
			var currentTabTag = _currentTag;

			// Disable all old buttons
			SetAllToDisbaled();

			// Update UI
			button.IsChecked = true;
			_currentTag = button.Tag.ToString();

			// Apply Storyboard
			var storyboardHide = (Storyboard)TryFindResource(string.Format("Hide{0}Tab", currentTabTag));
			var storyboardShow = (Storyboard)TryFindResource(string.Format("Show{0}Tab", button.Tag));
			if (storyboardHide != null) storyboardHide.Begin();
			if (storyboardShow != null) storyboardShow.Begin();
		}

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
			return true;
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
		}
	}
}