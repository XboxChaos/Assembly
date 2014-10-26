using System;
using System.Globalization;
using System.Windows.Data;

namespace Assembly.Helpers.Converters
{
	[ValueConversion(typeof (Enum), typeof (string))]
	public class EnumToString : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value == null) ? "" : value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}