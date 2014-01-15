using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Atlas.Models;

namespace Atlas.Selectors
{
	public class XbdmQuickOptionsButtonTypeSelector : DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var element = container as FrameworkElement;
			if (element == null || item == null || !(item is EngineMemory.EngineVersion.QuickOption)) 
				return null;

			var quickOption = item as EngineMemory.EngineVersion.QuickOption;

			if (quickOption.IsToggle)
				return element.FindResource("Toggle") as DataTemplate;

			return element.FindResource("NotToggle") as DataTemplate;
		}
	}
}
