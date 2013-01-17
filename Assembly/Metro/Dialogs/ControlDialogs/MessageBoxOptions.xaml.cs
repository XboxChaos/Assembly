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
using Assembly.Helpers;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for MessageBoxOptions.xaml
    /// </summary>
    public partial class MessageBoxOptions : Window
    {
        public MessageBoxOptions(string title, string message, MetroMessageBox.MessageBoxButtons buttons)
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);

            lblTitle.Text = title;
            lblSubInfo.Text = message;

            switch(buttons)
            {
                case MetroMessageBox.MessageBoxButtons.OK:
                    btnOkay.Visibility = Visibility.Visible;

                    btnOkay.IsDefault = true;
                    btnOkay.IsCancel = true;
                    break;
                case MetroMessageBox.MessageBoxButtons.OKCancel:
                    btnOkay.Visibility = Visibility.Visible;
                    btnCancel.Visibility = Visibility.Visible;

                    btnOkay.IsDefault = true;
                    btnCancel.IsCancel = true;
                    break;
                case MetroMessageBox.MessageBoxButtons.YesNo:
                    btnYes.Visibility = Visibility.Visible;
                    btnNo.Visibility = Visibility.Visible;

                    btnYes.IsDefault = true;
                    break;
                case MetroMessageBox.MessageBoxButtons.YesNoCancel:
                    btnYes.Visibility = Visibility.Visible;
                    btnNo.Visibility = Visibility.Visible;
                    btnCancel.Visibility = Visibility.Visible;

                    btnYes.IsDefault = true;
                    btnCancel.IsCancel = true;
                    break;
            }
        }

        private void btnOkay_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TempStorage.msgBoxButtonStorage = MetroMessageBox.MessageBoxResult.OK;
            this.Close();
        }
        private void btnYes_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TempStorage.msgBoxButtonStorage = MetroMessageBox.MessageBoxResult.Yes;
            this.Close();
        }
        private void btnNo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TempStorage.msgBoxButtonStorage = MetroMessageBox.MessageBoxResult.No;
            this.Close();
        }
        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TempStorage.msgBoxButtonStorage = MetroMessageBox.MessageBoxResult.Cancel;
            this.Close();
        }

        private void headerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Left = Left + e.HorizontalChange;
            Top = Top + e.VerticalChange;
        }
    }
}
