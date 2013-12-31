using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Atlas.Converters
{
	[ValueConversion(typeof (bool), typeof (Visibility))]
	public class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var boolValue = (Boolean) value;
			if (IsInverted) boolValue = !boolValue;

			return boolValue ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public bool IsInverted { get; set; }
	}
}
