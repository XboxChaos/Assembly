using System;
using System.Globalization;
using System.Windows.Data;

namespace Assembly.Helpers.Converters
{
	[ValueConversion(typeof (string), typeof (string))]
	public class ResourceFilePath : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value ?? "None (Local)";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}
