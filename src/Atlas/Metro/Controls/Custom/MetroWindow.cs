using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Atlas.Native;

namespace Atlas.Metro.Controls.Custom
{
    public class MetroWindow : Window
    {
        public MetroWindow()
        {
            SourceInitialized += OnSourceInitialized;
        }

        #region Maximize Workspace Workarounds

        public void OnSourceInitialized(object sender, EventArgs eventArgs)
        {
            var handle = (new WindowInteropHelper(this)).Handle;
            var hwndSource = HwndSource.FromHwnd(handle);
            if (hwndSource != null)
                hwndSource.AddHook(WindowProc);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024: // WM_GETMINMAXINFO
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            var mmi = (MonitorWorkarea.MinMaxInfo) Marshal.PtrToStructure(lParam, typeof (MonitorWorkarea.MinMaxInfo));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            const int monitorDefaulttonearest = 0x00000002;
            var monitor = MonitorWorkarea.MonitorFromWindow(hwnd, monitorDefaulttonearest);

            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MonitorWorkarea.MonitorInfo();
                MonitorWorkarea.GetMonitorInfo(monitor, monitorInfo);
                var rcWorkArea = monitorInfo.rcWork;
                var rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }

            mmi.ptMinTrackSize.x = DpiConversion.PointsToPixels(MinWidth, DpiConversion.Direction.Horizontal);
            mmi.ptMinTrackSize.y = DpiConversion.PointsToPixels(MinHeight, DpiConversion.Direction.Vertical);

            Marshal.StructureToPtr(mmi, lParam, true);
        }

		#endregion

		#region Window Actions

		public void WindowMinimizeButton_OnClick(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		public void WindowRestoreButton_OnClick(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Normal;
		}

		public void WindowMaximizeButton_OnClick(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Maximized;
		}

		public void WindowCloseButton_OnClick(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown(0);
		}

		#endregion
	}
}
