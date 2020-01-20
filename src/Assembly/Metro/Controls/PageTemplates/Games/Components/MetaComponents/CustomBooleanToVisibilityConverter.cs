using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	public class CustomBooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((bool)value) ? Visibility.Visible : parameter ?? Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}