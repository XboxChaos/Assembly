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
using Assembly.Backend;

namespace Assembly.Metro.Controls.Sidebar
{
    /// <summary>
    /// Interaction logic for LOGSidebarButton.xaml
    /// </summary>
    public partial class LOGSidebarButton : UserControl
    {
        public BrushConverter bc = new BrushConverter();
        public LOGSidebarButton()
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
                SelectionIndicator.Fill = (Brush)bc.ConvertFrom("#3f3f46");
                SidebarName.Foreground = (Brush)bc.ConvertFrom("White");
            }
        }
    }
}
