using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Atlas.Metro.Controls.Custom
{
	public class MetroClosableTabItem : TabItem
	{
		public static readonly RoutedEvent CloseTabEvent =
			EventManager.RegisterRoutedEvent("CloseTab", RoutingStrategy.Bubble,
				typeof (RoutedEventHandler), typeof (MetroClosableTabItem));

		static MetroClosableTabItem()
		{
			//This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
			//This style is defined in themes\generic.xaml
			DefaultStyleKeyProperty.OverrideMetadata(typeof (MetroClosableTabItem),
				new FrameworkPropertyMetadata(typeof (MetroClosableTabItem)));
		}

		public event RoutedEventHandler CloseTab
		{
			add { AddHandler(CloseTabEvent, value); }
			remove { RemoveHandler(CloseTabEvent, value); }
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var closeButton = GetTemplateChild("PART_Close") as Button;
			if (closeButton != null)
				closeButton.Click += (sender, args) => RaiseEvent(new RoutedEventArgs(CloseTabEvent, this));

			var element = GetTemplateChild("ContainerGrid") as Grid;
			if (element != null)
				element.MouseDown += (sender, args) =>
				{
					if (args.MiddleButton == MouseButtonState.Pressed)
						RaiseEvent(new RoutedEventArgs(CloseTabEvent, this));
				};
		}
	}
}