using System.Windows;
using System.Windows.Controls;

namespace Assembly.Metro.Controls.WPFTreeView
{
	public class MetroTreeViewItem : TreeViewItem
	{
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			IsSelected = true;
			if (e != null) RaiseEvent(e);
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new MetroTreeViewItem();
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return (item is MetroTreeViewItem);
		}
	}
}