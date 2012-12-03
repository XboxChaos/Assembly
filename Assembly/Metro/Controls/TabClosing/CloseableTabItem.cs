using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CloseableTabItemDemo
{
    /// <summary>
    /// ========================================
    /// .NET Framework 3.0 Custom Control
    /// ========================================
    ///
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CloseableTabItemDemo"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CloseableTabItemDemo;assembly=CloseableTabItemDemo"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file. Note that Intellisense in the
    /// XML editor does not currently work on custom controls and its child elements.
    ///
    ///     <MyNamespace:CloseableTabItem/>
    ///
    /// </summary>
    public class CloseableTabItem : TabItem
    {
        static CloseableTabItem()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CloseableTabItem),
                new FrameworkPropertyMetadata(typeof(CloseableTabItem)));
        }

        public static readonly RoutedEvent CloseTabEvent =
            EventManager.RegisterRoutedEvent("CloseTab", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(CloseableTabItem));

        public event RoutedEventHandler CloseTab
        {
            add { AddHandler(CloseTabEvent, value); }
            remove { RemoveHandler(CloseTabEvent, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Button closeButton = base.GetTemplateChild("PART_Close") as Button;
            if (closeButton != null)
                closeButton.Click += new System.Windows.RoutedEventHandler(closeButton_Click);
        }

        void closeButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(CloseTabEvent, this));
        }
    }
}
