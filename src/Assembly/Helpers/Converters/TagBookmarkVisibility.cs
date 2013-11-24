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
				outputBoolean = (bool)value;

			if (!App.AssemblyStorage.AssemblySettings.HalomapOnlyShowBookmarkedTags)
				return Visibility.Visible;

			return (App.AssemblyStorage.AssemblySettings.HalomapOnlyShowBookmarkedTags && outputBoolean)
						? Visibility.Visible
						: Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}
