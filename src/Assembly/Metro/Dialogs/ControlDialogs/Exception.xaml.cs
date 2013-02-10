using System.Windows;
using System.Windows.Controls.Primitives;
using Assembly.Metro.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for Exception.xaml
    /// </summary>
    public partial class Exception
    {
        public Exception(System.Exception ex)
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);

            lblContent.Text = ex.ToString();
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}