using System;
using System.Globalization;
using System.Windows.Data;

namespace Atlas.Converters
{
	[ValueConversion(typeof (int), typeof (bool))]
	public class IntegerToBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var integer = int.Parse(value.ToString());

			return (integer >= 1);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
