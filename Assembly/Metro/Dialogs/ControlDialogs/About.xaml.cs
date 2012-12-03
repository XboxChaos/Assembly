using System;
using System.Collections.Generic;
using System.Linq;
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
using Assembly.Backend;
using Assembly.Metro.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);
        }

        private void headerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Left = Left + e.HorizontalChange;
            Top = Top + e.VerticalChange;
        }
        private void btnActionClose_Click(object sender, RoutedEventArgs e) { this.Close(); }

        private void Button_Click_1(object sender, RoutedEventArgs e) { this.Close(); }

        private void lblZedd_MouseDown(object sender, MouseButtonEventArgs e) { imageOfGodOfAllThingsHolyAndModdy.Visibility = _0xabad1dea.GodOfAllThingsHolyAndModdy.ShowGod(); lblZedd.Text = _0xabad1dea.GodOfAllThingsHolyAndModdy.ShowGodsRealName(); }
    }
}
