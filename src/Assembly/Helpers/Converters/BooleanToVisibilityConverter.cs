using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Assembly.Helpers.Converters
{
	public class BooleanToVisibilityConverter : IValueConverter
	{
		public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			if (targetType != typeof (Visibility))
				throw new InvalidOperationException("Converter can only convert to value of type Visibility.");

			var visible = System.Convert.ToBoolean(value, culture);
			if (InvertVisibility) visible = !visible;
			return visible ? Visibility.Visible : Visibility.Collapsed;
		}

		public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			return value;
		}

		public Boolean InvertVisibility { get; set; }
	}
}
