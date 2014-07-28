using System;
using System.Globalization;
using System.Windows.Data;

namespace Atlas.Metro.Controls.Converters
{
	public class ScrollbarOnFarLeftConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return false;
			return ((double)value > 0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
