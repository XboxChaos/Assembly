using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for tagValue.xaml
	/// </summary>
	public partial class tagValue : UserControl
	{
		public static RoutedCommand JumpToCommand = new RoutedCommand();

		public tagValue()
		{
			InitializeComponent();
		}

		private void cbTagClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			bool enable = (cbTagClass.SelectedIndex > 0);
			cbTagEntry.IsEnabled = enable;
			btnJumpToTag.IsEnabled = enable;
		}

		private void cbTagEntry_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cbTagEntry.SelectedIndex < 0 && cbTagClass.SelectedIndex > 0)
				cbTagEntry.SelectedIndex = 0;
		}

		private void CanExecuteJumpToCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			// hack so that the button is enabled by default
			e.CanExecute = true;
		}

		private void btnNullTag_Click(object sender, RoutedEventArgs e)
		{
			cbTagClass.SelectedIndex = 0;
		}
	}

	/// <summary>
	///     Converts a TagClass to an index in the class list.
	/// </summary>
	[ValueConversion(typeof (TagClass), typeof (int))]
	internal class TagClassConverter : DependencyObject, IValueConverter
	{
		public static DependencyProperty TagsSourceProperty = DependencyProperty.Register(
			"TagsSource",
			typeof (TagHierarchy),
			typeof (TagClassConverter)
			);

		public TagHierarchy TagsSource
		{
			get { return (TagHierarchy) GetValue(TagsSourceProperty); }
			set { SetValue(TagsSourceProperty, value); }
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var sourceClass = (TagClass) value;
			int index = TagsSource.Classes.IndexOf(sourceClass);
			return index + 1;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int adjustedIndex = (int) value - 1;
			if (adjustedIndex >= 0 && adjustedIndex < TagsSource.Classes.Count)
				return TagsSource.Classes[adjustedIndex];
			return null;
		}
	}

	[ValueConversion(typeof (TagClass), typeof (List<TagEntry>))]
	internal class TagEntryListRetriever : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var sourceClass = value as TagClass;
			if (sourceClass == null)
				return null;
			return sourceClass.Children;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}