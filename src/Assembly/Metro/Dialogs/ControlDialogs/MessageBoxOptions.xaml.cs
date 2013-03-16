using System.Windows;
using System.Windows.Controls.Primitives;
using Assembly.Helpers.Native;
using Assembly.Helpers;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for MessageBoxOptions.xaml
    /// </summary>
    public partial class MessageBoxOptions
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

        private void btnOkay_Click(object sender, RoutedEventArgs e)
        {
            TempStorage.MessageBoxButtonStorage = MetroMessageBox.MessageBoxResult.OK;
            Close();
        }
        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            TempStorage.MessageBoxButtonStorage = MetroMessageBox.MessageBoxResult.Yes;
            Close();
        }
        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            TempStorage.MessageBoxButtonStorage = MetroMessageBox.MessageBoxResult.No;
            Close();
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            TempStorage.MessageBoxButtonStorage = MetroMessageBox.MessageBoxResult.Cancel;
            Close();
        }
    }
}
