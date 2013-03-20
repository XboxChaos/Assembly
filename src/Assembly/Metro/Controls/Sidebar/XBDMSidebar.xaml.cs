using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;
using Assembly.Windows;

namespace Assembly.Metro.Controls.Sidebar
{
    /// <summary>
    /// Interaction logic for XBDMSidebar.xaml
    /// </summary>
    public partial class XBDMSidebar
    {
        public XBDMSidebar()
        {
            InitializeComponent();
        }

        private void btnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            var screenshotFileName = Path.GetTempFileName();

			if (Settings.xbdm.GetScreenshot(screenshotFileName))
				Settings.homeWindow.AddScrenTabModule(screenshotFileName);
			else
				MetroMessageBox.Show("Not Connected", "You are not connected to a debug Xbox 360.");
        }
        private void btnFreeze_Click(object sender, RoutedEventArgs e)
        {
            Settings.xbdm.Freeze();
        }
        private void btnUnfreeze_Click(object sender, RoutedEventArgs e)
        {
            Settings.xbdm.Unfreeze();
        }
        private void btnRebootTitle_Click(object sender, RoutedEventArgs e)
        {
            Settings.xbdm.Reboot(XBDMCommunicator.Xbdm.RebootType.Title);
        }
        private void btnRebootCold_Click(object sender, RoutedEventArgs e)
        {
            Settings.xbdm.Reboot(XBDMCommunicator.Xbdm.RebootType.Cold);
        }
    }
}