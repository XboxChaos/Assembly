using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Assembly.Helpers;

namespace Assembly.Metro.Controls.Sidebar
{
    /// <summary>
    /// Interaction logic for XBDMSidebarButton.xaml
    /// </summary>
    public partial class XBDMSidebarButton : UserControl
    {
        public XBDMSidebarButton()
        {
            InitializeComponent();
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Settings.homeWindow.XBDMSidebarTimerEvent();
        }

        private void Rectangle_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                SelectionIndicator.Fill = (SolidColorBrush)FindResource("ExtryzeAccentBrushSecondary");
                SidebarName.Foreground = (SolidColorBrush)FindResource("ExtryzeAccentBrushSecondary");
            }
            else
            {
                SelectionIndicator.Fill = (SolidColorBrush)FindResource("SidebarBlockBrush");
                SidebarName.Foreground = (SolidColorBrush)FindResource("TextBrushPrimary");
            }
        }
    }
}
