using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Assembly.Helpers.Converters
{
	[ValueConversion(typeof (bool), typeof (Brush))]
	public class TagBookmarkForeground : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool outputBoolean = false;
			if (value != null)
				outputBoolean = (bool) value;

			return outputBoolean
				? Application.Current.FindResource("ExtryzeAccentBrush")
				: Brushes.White;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}