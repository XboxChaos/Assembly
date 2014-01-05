using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Atlas.Models;

namespace Atlas.Converters
{
	[ValueConversion(typeof(String), typeof(TemplateKey))]
	public class CacheEditorNodeToIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var cacheEditor = (CacheEditorNode)value;

			return Application.Current.FindResource("HierarchyGenericTag");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
