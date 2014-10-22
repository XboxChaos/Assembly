using System;
using System.Globalization;
using System.Windows.Data;

namespace Assembly.Helpers.Converters
{
	[ValueConversion(typeof (int), typeof (string))]
	public class IntegerTo32BitHex : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Format("0x{0:X8}", value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}
