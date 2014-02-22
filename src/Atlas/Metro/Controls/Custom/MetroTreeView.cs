using System.Windows;
using System.Windows.Controls;

namespace Atlas.Metro.Controls.Custom
{
	public class MetroTreeView : TreeView
	{
		public MetroTreeView()
		{
			SelectedItemChanged += ___ICH;
		}

		void ___ICH(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (SelectedItem != null)
			{
				SetValue(SelectedItemProperty, SelectedItem);
			}
		}

		public object InpcSelectedItem
		{
			get { return GetValue(SelectedItemProperty); }
			set { SetValue(SelectedItemProperty, value); }
		}
		public new static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("InpcSelectedItem", typeof(object), typeof(MetroTreeView), new UIPropertyMetadata(null));
	}
}
