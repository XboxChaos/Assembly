using System;
using System.Windows.Data;

namespace Atlas.Metro.Controls.Converters
{
	public class ScrollbarOnFarRightConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (values == null) return false;
			if (values[0] == null || values[1] == null || values[2] == null) return false;
			if (values[0].Equals(double.NaN) || values[1].Equals(double.NaN) || values[2].Equals(double.NaN)) return false;

			double dblHorizontalOffset;
			double dblViewportWidth;
			double dblExtentWidth;

			double.TryParse(values[0].ToString(), out dblHorizontalOffset);
			double.TryParse(values[1].ToString(), out dblViewportWidth);
			double.TryParse(values[2].ToString(), out dblExtentWidth);

			var fOnFarRight = Math.Round((dblHorizontalOffset + dblViewportWidth), 2) < Math.Round(dblExtentWidth, 2);
			return fOnFarRight;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
