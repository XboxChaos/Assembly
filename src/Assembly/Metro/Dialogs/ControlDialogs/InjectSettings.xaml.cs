using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using Assembly.Metro.Controls.PageTemplates.Games;
using Blamite.Injection;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Data;
using System.Text;
using System.Globalization;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for InjectSettings.xaml
	/// </summary>
	public partial class InjectSettings
	{
		private TagContainer Container { get; set; }

		private readonly TagHierarchy _allTags;

		public TagHierarchy Tags
		{
			get { return _allTags; }
		}

		private List<ExtractedGroup> UsedGroups = new List<ExtractedGroup>();

		public InjectSettings(TagHierarchy tags, TagContainer container)
		{
			InitializeComponent();
			
			Container = container;
			_allTags = tags;

			List<int> blah = new List<int>();

			foreach (ExtractedTag t in Container.Tags)
				blah.Add(t.Group);

			UsedGroups.Add(new ExtractedGroup(0x2D2D2D2D));

			foreach (int t in blah.Distinct())
				UsedGroups.Add(new ExtractedGroup(t));

			UsedGroups = UsedGroups.OrderBy(c => c.MagicString).ToList();

			tagGroups.ItemsSource = UsedGroups;
			tagGroups.SelectedIndex = 0;

		}

		public bool? KeepSounds
		{
			get { return keepsnd.IsChecked; }
			set { keepsnd.IsChecked = value; }
		}

		public bool? FindRaw
		{
			get { return findraw.IsChecked; }
			set { findraw.IsChecked = value; }
		}

		public bool? InjectRaw
		{
			get { return injectraw.IsChecked; }
			set { injectraw.IsChecked = value; }
		}

		public bool? UniqueShaders
		{
			get { return uniqueshaders.IsChecked; }
			set { uniqueshaders.IsChecked = value; }
		}

		public bool? AddPrediction
		{
			get { return addprediction.IsChecked; }
			set { addprediction.IsChecked = value; }
		}


		private void btnOK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void listTags_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (listTags.SelectedItem != null)
			{
				var tag = listTags.SelectedItem as ExtractedTag;
				if (tag == null)
					return;

				string newName = MetroInputBox.Show("Rename Tag",
				"Please enter a new name for the tag.",
				tag.Name, "Enter a tag name.");
				if (newName == null || newName == tag.Name)
					return;

				// Set the name
				tag.Name = newName;

				// Update UI
				listTags.Items.Refresh();
			}
		}


		private void tagGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (tagGroups.SelectedIndex > 0)
			{
				ExtractedGroup eg = tagGroups.SelectedItem as ExtractedGroup;
				listTags.ItemsSource = Container.Tags.Where(t => t.Group == eg.Magic).OrderBy(t => t.Name);
			}
			else
			{
				listTags.ItemsSource = null;
			}
		}

		private void MassRename_Click(object sender, RoutedEventArgs e)
		{
			bool GroupOnly = (bool)renameGroupOnly.IsChecked;
			string find = massRenameFind.Text;
			string replace = massRenameReplace.Text;
			int counter = 0;

			if (string.IsNullOrEmpty(find))
				return;

			ExtractedGroup CurrentGroup = (ExtractedGroup)tagGroups.SelectedItem;

			foreach (ExtractedTag t in GroupOnly ? Container.Tags.Where(tt => tt.Group == CurrentGroup.Magic) : Container.Tags)
			{
				if (t.Name.Contains(find))
				{
					counter++;
					t.Name = t.Name.Replace(find, replace);
				}
			}

			MetroMessageBox.Show("Mass Replace", "Successfully renamed " + counter + " tags.");

			listTags.Items.Refresh();

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

		private class ExtractedGroup
		{
			public string MagicString { get; }
			public int Magic { get; set; }

			public ExtractedGroup(int magic)
			{
				Magic = magic;

				var magicbytes = BitConverter.GetBytes(Magic);
				Array.Reverse(magicbytes);
				MagicString = Encoding.ASCII.GetString(magicbytes);
			}
		}

	}

	[ValueConversion(typeof(ExtractedTag), typeof(bool))]
	internal class ExistingTagConverter : DependencyObject, IValueConverter
	{
		public static DependencyProperty TagsSourceProperty = DependencyProperty.Register(
			"TagsSource",
			typeof(TagHierarchy),
			typeof(ExistingTagConverter)
			);

		public TagHierarchy TagsSource
		{
			get { return (TagHierarchy)GetValue(TagsSourceProperty); }
			set { SetValue(TagsSourceProperty, value); }
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var sourceTag = (ExtractedTag)value;

			TagGroup tg = TagsSource.Groups.Where(c => c.RawGroup.Magic == sourceTag.Group).FirstOrDefault();
			if (tg == null)
				return false;

			var existingTag = tg.Children.Find(t => t.TagFileName == sourceTag.Name);

			return existingTag != null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}