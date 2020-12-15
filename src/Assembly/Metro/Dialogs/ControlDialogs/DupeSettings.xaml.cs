using Blamite.Blam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	/// Interaction logic for DupeSettings.xaml
	/// </summary>
	public partial class DupeSettings : Window
	{
		ICacheFile _cacheFile;
		ITagGroup _tagGroup;
		string _origName;

		public DupeSettings(ICacheFile cache, ITagGroup tagGroup, string origName)
		{
			InitializeComponent();

			_cacheFile = cache;
			_tagGroup = tagGroup;
			_origName = origName;
			NewName = origName;

			tagName.Text = origName;

			tagName.Focus();
			tagName.SelectAll();
		}

		public string NewName { get; private set; }

		public bool? DupeAsset
		{
			get { return dupeRaw.IsChecked; }
			set { dupeRaw.IsChecked = value; }
		}

		public bool? DupePred
		{
			get { return dupePrediction.IsChecked; }
			set { dupePrediction.IsChecked = value; }
		}

		public bool? DupeSoundGestalt
		{
			get { return dupeSound.IsChecked; }
			set { dupeSound.IsChecked = value; }
		}

		private void tagName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
				Finish();
		}

		private void btnOK_Click(object sender, RoutedEventArgs e)
		{
			Finish();
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void ResizeDrop_DragDelta(object sender, DragDeltaEventArgs e)
		{
			double yadjust = Height + e.VerticalChange;
			double xadjust = Width + e.HorizontalChange;

			if (xadjust > MinWidth)
				Width = xadjust;
			if (yadjust > MinHeight)
				Height = yadjust;
		}

		private void Finish()
		{
			if (string.IsNullOrEmpty(NewName) || string.IsNullOrWhiteSpace(NewName))
			{
				DialogResult = false;
				Close();
			}

			if (tagName.Text == _origName || _cacheFile.Tags.FindTagByName(tagName.Text, _tagGroup, _cacheFile.FileNames) != null)
			{
				MetroMessageBox.Show("Duplicate Tag", "Please enter a name that is different from the original and that is not in use.");
				return;
			}

			NewName = tagName.Text;
			DialogResult = true;
			Close();
		}
	}
}
