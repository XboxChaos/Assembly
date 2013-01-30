using System.Windows;
using System.Windows.Controls.Primitives;
using Assembly.Metro.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for MessageBox.xaml
    /// </summary>
    public partial class MessageBox
    {
        public MessageBox(string title, string message)
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);

            lblTitle.Text = title;
            lblSubInfo.Text = message;
        }

        private void btnOkay_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void headerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Left = Left + e.HorizontalChange;
            Top = Top + e.VerticalChange;
        }
    }
}
