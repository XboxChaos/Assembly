using System.Windows;
using System.Windows.Controls;

namespace Atlas.Metro.Controls.Custom
{
	public class MetroClosableTabControl : TabControl
	{
		protected override DependencyObject GetContainerForItemOverride()
		{
			return new MetroClosableTabItem();
		}
	}
}
