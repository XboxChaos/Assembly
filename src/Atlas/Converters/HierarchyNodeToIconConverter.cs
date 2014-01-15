using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using Atlas.Helpers.Tags;

namespace Atlas.Converters
{
	[ValueConversion(typeof(String), typeof(TemplateKey))]
	public class HierarchyNodeToIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var tagHierarchyNode = (TagHierarchyNode) value;
			if (tagHierarchyNode.IsFolder)
				return Application.Current.FindResource("HierarchyClosedFolder");

			var ext = (Path.GetExtension(tagHierarchyNode.Name) ?? "").ToLower();
			switch (ext)
			{
				case ".snd!":
					return Application.Current.FindResource("HierarchySnd!Tag");

				case ".bitm":
					return Application.Current.FindResource("HierarchyBitmTag");

				case ".mode":
					return Application.Current.FindResource("HierarchyModeTag");

				default:
					return Application.Current.FindResource("HierarchyGenericTag");
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
