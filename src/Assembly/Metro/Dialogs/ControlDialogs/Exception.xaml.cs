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

		private void btnReportToGithub_Click(object sender, RoutedEventArgs e)
		{
			string title = HttpUtility.HtmlEncode(_exception.Message);
			string body = HttpUtility.HtmlEncode(_exception.ToString());
			const string labels = "bug";

			string githubIssueCreate =
				string.Format("https://github.com/XboxChaos/Assembly/issues/new?title={0}&body={1}&labels={2}",
					title, body, labels);

			btnReportToGithub.IsEnabled = false;
			Process.Start(githubIssueCreate);
		}
	}
}