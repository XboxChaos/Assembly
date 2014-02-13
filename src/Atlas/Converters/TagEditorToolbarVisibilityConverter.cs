using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Atlas.Views.Cache;

namespace Atlas.Converters
{
	[ValueConversion(typeof(ICacheEditor), typeof(Visibility))]
	public class TagEditorToolbarVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var editor = value as ICacheEditor;
			if (editor == null) return Visibility.Collapsed;

			return (editor is TagEditor) ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
