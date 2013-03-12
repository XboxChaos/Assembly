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
        public DispatcherTimer _hideTimer = new DispatcherTimer();
        public bool ControlhasFocus = true;
	    private bool onStartup = true;

        public XBDMSidebar()
        {
            InitializeComponent();
            _hideTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _hideTimer.Tick +=_hideTimer_Tick;

	        btnPinXBDMSidebar.IsChecked = Settings.applicationXBDMSidebarLocation == Home.XBDMSidebarLocations.Docked;
	        onStartup = false;
        }

        void _hideTimer_Tick(object sender, EventArgs e)
        {
            if (!ControlhasFocus)
            {
                Settings.homeWindow.XBDMSidebarTimerEvent();
            }

            _hideTimer.Stop();
        }
        private void XBDMSidebar_LostFocus(object sender, RoutedEventArgs e)
        {
            ControlhasFocus = false;

            _hideTimer.Stop();
            _hideTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _hideTimer.Start();
        }
        private void XBDMSidebar_GotFocus(object sender, RoutedEventArgs e)
        {
            ControlhasFocus = true;
        }
        private void XBDMSidebar_MouseEnter(object sender, MouseEventArgs e)
        {
            Focus();
        }

        private void btnPinXBDMSidebar_Unchecked(object sender, RoutedEventArgs e) { if (!onStartup) Settings.homeWindow.SwitchXBDMSidebarLocation(Windows.Home.XBDMSidebarLocations.Sidebar); }
		private void btnPinXBDMSidebar_Checked(object sender, RoutedEventArgs e) { if (!onStartup) Settings.homeWindow.SwitchXBDMSidebarLocation(Windows.Home.XBDMSidebarLocations.Docked); }

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