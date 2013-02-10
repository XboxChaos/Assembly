using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;

namespace Assembly.Metro.Controls.Sidebar
{
    /// <summary>
    /// Interaction logic for XBDMSidebar.xaml
    /// </summary>
    public partial class XBDMSidebar
    {
        public DispatcherTimer _hideTimer = new DispatcherTimer();
        public bool ControlhasFocus = true;

        public XBDMSidebar()
        {
            InitializeComponent();
            _hideTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _hideTimer.Tick +=_hideTimer_Tick;
        }

        void _hideTimer_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("HIDE TIMER BEEN HIT, LIKE A NIGGA IN A DRUG STORE");

            if (!ControlhasFocus)
            {
                Settings.homeWindow.XBDMSidebarTimerEvent();

                Debug.WriteLine("AND DA CONTROL ALSO GOT NO FOCUS (SO WE HID THE CONTROL, YE)");
            }
            else
                Debug.WriteLine("BUT SADLY DAT CONTROL GOT DAT FOCUS");

            _hideTimer.Stop();
        }
        private void XBDMSidebar_LostFocus(object sender, RoutedEventArgs e)
        {
            ControlhasFocus = false;

            Debug.WriteLine("YO DIS CONTROL GOT NO FOCUS");

            _hideTimer.Stop();
            _hideTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _hideTimer.Start();
        }
        private void XBDMSidebar_GotFocus(object sender, RoutedEventArgs e)
        {
            ControlhasFocus = true;

            Debug.WriteLine("YO DIS CONTROL GOT SUM SRS FOCUS");
        }
        private void XBDMSidebar_MouseEnter(object sender, MouseEventArgs e)
        {
            Focus();

            Debug.WriteLine("MOUSE ENTERED THE MEMORY POKING SIDEBAR. HOW RUDE.");
        }

        private void btnPinXBDMSidebar_Unchecked(object sender, RoutedEventArgs e) { Settings.homeWindow.SwitchXBDMSidebarLocation(Windows.Home.XBDMSidebarLocations.Sidebar); }
        private void btnPinXBDMSidebar_Checked(object sender, RoutedEventArgs e) { Settings.homeWindow.SwitchXBDMSidebarLocation(Windows.Home.XBDMSidebarLocations.Docked); }

        private void btnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            var screenshotFileName = VariousFunctions.CreateTemporaryFile(VariousFunctions.GetTemporaryImageLocation());

			if (Settings.xbdm.GetScreenshot(screenshotFileName))
				Settings.homeWindow.AddScrenTabModule(screenshotFileName);
			else
				MetroMessageBox.Show("Not Connected", "You are not connected to a Debug Xbox 360.");
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