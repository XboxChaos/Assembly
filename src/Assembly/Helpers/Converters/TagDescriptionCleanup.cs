using System;
using System.Globalization;
using System.Windows.Data;
namespace Assembly.Helpers.Converters
{
	public class TagDescriptionCleanup : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return String.IsNullOrEmpty((string)value) ? "unknown_description" : value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}
