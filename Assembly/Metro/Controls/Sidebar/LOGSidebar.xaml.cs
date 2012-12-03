using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Assembly.Backend;
using Assembly.Metro.Dialogs;
using XBDMCommunicator;

namespace Assembly.Metro.Controls.Sidebar
{
    /// <summary>
    /// Interaction logic for LOGSidebar.xaml
    /// </summary>
    public partial class LOGSidebar : UserControl
    {
        public DispatcherTimer _hideTimer = new DispatcherTimer();
        public bool ControlhasFocus = true;

        public LOGSidebar()
        {
            InitializeComponent();
            _hideTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _hideTimer.Tick += _hideTimer_Tick;
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
        private void LOGSidebar_LostFocus(object sender, RoutedEventArgs e)
        {
            ControlhasFocus = false;

            Debug.WriteLine("YO DIS CONTROL GOT NO FOCUS");

            _hideTimer.Stop();
            _hideTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _hideTimer.Start();
        }
        private void LOGSidebar_GotFocus(object sender, RoutedEventArgs e)
        {
            ControlhasFocus = true;

            Debug.WriteLine("YO DIS CONTROL GOT SUM SRS FOCUS");
        }
        private void LOGSidebar_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Focus();

            Debug.WriteLine("MOUSE ENTERED THE MEMORY POKING SIDEBAR. HOW RUDE.");
        }

        private void btnPinLOGSidebar_Unchecked(object sender, RoutedEventArgs e) { Settings.homeWindow.SwitchXBDMSidebarLocation(Assembly.Windows.Home.XBDMSidebarLocations.Sidebar); }
        private void btnPinLOGSidebar_Checked(object sender, RoutedEventArgs e) { Settings.homeWindow.SwitchXBDMSidebarLocation(Assembly.Windows.Home.XBDMSidebarLocations.Docked); }

    }
}
