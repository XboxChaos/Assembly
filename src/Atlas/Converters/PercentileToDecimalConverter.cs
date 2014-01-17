using System;
using System.Globalization;
using System.Windows.Data;

namespace Atlas.Converters
{
	[ValueConversion(typeof (string), typeof (float))]
	public class PercentileToDecimalConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return 1.0f;
			return float.Parse(((string)value).Replace("%", "")) / 100;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
