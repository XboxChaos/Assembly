using System.Diagnostics;
using System.Web;
using System.Windows;
using Assembly.Helpers.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for Exception.xaml
	/// </summary>
	public partial class Exception
	{
		private readonly System.Exception _exception;

		public Exception(System.Exception ex)
		{
			InitializeComponent();
			DwmDropShadow.DropShadowToWindow(this);
			_exception = ex;

			lblMessage.Text = ex.Message;
			lblContent.Text = ex.ToString();

			Activate();
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