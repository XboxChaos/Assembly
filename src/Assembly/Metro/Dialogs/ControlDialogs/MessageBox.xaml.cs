using System.Windows;
using Assembly.Helpers.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for MessageBox.xaml
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
	}
}