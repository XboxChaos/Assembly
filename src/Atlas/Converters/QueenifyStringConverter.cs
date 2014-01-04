using System;
using System.Globalization;
using System.Windows.Data;

namespace Atlas.Converters
{
	[ValueConversion(typeof(String), typeof(String))]
	public class QueenifyStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var str = value.ToString();
			return char.ToUpper(str[0]) + str.Substring(1);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
