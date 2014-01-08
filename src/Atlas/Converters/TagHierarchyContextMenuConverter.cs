using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Atlas.Helpers.Tags;

namespace Atlas.Converters
{
	[ValueConversion(typeof (TagHierarchyNode), typeof (ContextMenu))]
	public class TagHierarchyContextMenuConverter : IValueConverter
	{
		public ContextMenu TagContextMenu { get; set; }

		public ContextMenu FolderContextMenu { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var item = value as TagHierarchyNode;
			if (item == null) return null;

			return item.IsTag ? TagContextMenu : FolderContextMenu;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}