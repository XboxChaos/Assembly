using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
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

		#region Events

		#region Maximize Workspace Workarounds

		public void OnSourceInitialized(object sender, EventArgs eventArgs)
		{
			var handle = (new WindowInteropHelper(this)).Handle;
			var hwndSource = HwndSource.FromHwnd(handle);
			if (hwndSource != null)
				hwndSource.AddHook(WindowProc);
		}

		private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case 0x0024:
					WmGetMinMaxInfo(hwnd, lParam);
					handled = true;
					break;
			}
			return IntPtr.Zero;
		}

		private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
		{
			var mmi = (Monitor_Workarea.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(Monitor_Workarea.MINMAXINFO));

			// Adjust the maximized size and position to fit the work area of the correct monitor
			const int monitorDefaulttonearest = 0x00000002;
			var monitor = Monitor_Workarea.MonitorFromWindow(hwnd, monitorDefaulttonearest);

			if (monitor != IntPtr.Zero)
			{
				var monitorInfo = new Monitor_Workarea.MONITORINFO();
				Monitor_Workarea.GetMonitorInfo(monitor, monitorInfo);
				var rcWorkArea = monitorInfo.rcWork;
				var rcMonitorArea = monitorInfo.rcMonitor;
				mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
				mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
				mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
				mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
			}

			Marshal.StructureToPtr(mmi, lParam, true);
		}

		#endregion

		#region Resizing

		public void ResizeCornerThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			var yAdjust = Height + e.VerticalChange;
			var xAdjust = Width + e.HorizontalChange;

			if (xAdjust > MinWidth)
			{
				Width = xAdjust;
			}
			else
			{
				Width = MinWidth;
			}

			if (yAdjust > MinHeight)
			{
				Height = yAdjust;
			}
			else
			{
				Height = MinHeight;
			}
		}

		public void ResizeRightThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			var xAdjust = Width + e.HorizontalChange;

			if (xAdjust > MinWidth)
			{
				Width = xAdjust;
			}
			else
			{
				Width = MinWidth;
			}
		}

		public void ResizeBottomThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			var yAdjust = Height + e.VerticalChange;

			if (yAdjust > MinHeight)
			{
				Height = yAdjust;
			}
			else
			{
				Height = MinHeight;
			}
		}

		public void ResizeLeftThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			var xAdjust = Width - e.HorizontalChange;

			if (xAdjust > MinWidth)
			{
				Width -= e.HorizontalChange;
				Left += e.HorizontalChange;
			}
			else
			{
				var diff = Width - MinWidth;
				if (diff > 0)
				{
					Width -= diff;  // Width = MinWidth
					Left += diff;   // mirror that^ change
				}
				else
				{
					Width += diff;  // Width = MinWidth
					Left -= diff;   // mirror that^ change
				}
			}
		}

		public void ResizeTopThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			var yAdjust = Height - e.VerticalChange;

			if (yAdjust > MinHeight)
			{
				Height -= e.VerticalChange;
				Top += e.VerticalChange;
			}
			else
			{
				var diff = Height - MinHeight;
				if (diff > 0)
				{
					Height -= diff; // Height = MinHeight
					Top += diff;	// mirror that^ change
				}
				else
				{
					Height += diff; // Height = MinHeight
					Top -= diff;	// mirror that^ change
				}
			}
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

		#endregion
	}
}
