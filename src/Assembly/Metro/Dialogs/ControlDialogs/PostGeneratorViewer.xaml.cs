using System.Windows;
using Assembly.Helpers.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	/// Interaction logic for PostGeneratorViewer.xaml
	/// </summary>
	public partial class PostGeneratorViewer
	{
		public PostGeneratorViewer(string bbcode, string author)
		{
			InitializeComponent();
			DwmDropShadow.DropShadowToWindow(this);

			txtGeneratedBBCode.Text = bbcode;
			lblTitle.Text = string.Format("Hey {0}, your post has been Generated.", author);
		}

		private void btnOkay_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}