using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Assembly.Helpers.Converters
{
	[ValueConversion(typeof(bool), typeof(Visibility))]
	public class TagBookmarkVisibility : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var outputBoolean = false;
			if (value != null)
				outputBoolean = (bool) value;

			if (!Settings.halomapOnlyShowBookmarkedTags)
				return Visibility.Visible;

			return (Settings.halomapOnlyShowBookmarkedTags && outputBoolean)
				       ? Visibility.Visible
				       : Visibility.Collapsed;


			//return (Settings.halomapOnlyShowBookmarkedTags && outputBoolean)
			//		   ? (Brush)Application.Current.FindResource("ExtryzeAccentBrushSecondary")
			//		   : (Brush)Brushes.White;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}
