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
using Assembly.Metro.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for Exception.xaml
    /// </summary>
    public partial class Exception : Window
    {
        public Exception(System.Exception ex)
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);

            lblContent.Text = ex.ToString();
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void headerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Left = Left + e.HorizontalChange;
            Top = Top + e.VerticalChange;
        }
    }
}