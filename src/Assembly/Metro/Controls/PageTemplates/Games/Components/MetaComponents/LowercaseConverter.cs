using System;
using System.Globalization;
using System.Windows.Data;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	public class LowercaseConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value.ToString().ToLowerInvariant();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}