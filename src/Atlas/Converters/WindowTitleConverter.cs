using System;
using System.Globalization;
using System.Windows.Data;

namespace Atlas.Converters
{
	[ValueConversion(typeof(String), typeof(String))]
	public class WindowTitleConverter : IValueConverter
	{
		public const string Ending = " - Assembly";

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return String.Format("{0}{1}", value, Ending);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var strValue = (String) value;
			return strValue.Remove(strValue.Length - Ending.Length);
		}
	}
}
