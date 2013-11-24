using System.IO;
using System.Windows;
using Assembly.Metro.Dialogs;
using XBDMCommunicator;

namespace Assembly.Metro.Controls.Sidebar
{
	/// <summary>
	///     Interaction logic for XBDMSidebar.xaml
	/// </summary>
	public partial class XbdmSidebar
	{
		public XbdmSidebar()
		{
			InitializeComponent();
		}

		private void btnScreenshot_Click(object sender, RoutedEventArgs e)
		{
			string screenshotFileName = Path.GetTempFileName();

			if (App.AssemblyStorage.AssemblySettings.Xbdm.GetScreenshot(screenshotFileName))
				App.AssemblyStorage.AssemblySettings.HomeWindow.AddScrenTabModule(screenshotFileName);
			else
				MetroMessageBox.Show("Not Connected", "You are not connected to a debug Xbox 360.");
		}

		private void btnFreeze_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.Xbdm.Freeze();
		}

		private void btnUnfreeze_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.Xbdm.Unfreeze();
		}

		private void btnRebootTitle_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.Xbdm.Reboot(Xbdm.RebootType.Title);
		}

		private void btnRebootCold_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.Xbdm.Reboot(Xbdm.RebootType.Cold);
		}
	}
}