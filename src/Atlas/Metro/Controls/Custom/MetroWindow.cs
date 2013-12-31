using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using Atlas.Native;

namespace Atlas.Metro.Controls.Custom
{
	public class MetroWindow : Window, INotifyPropertyChanged
	{
		public static DependencyProperty WindowStatusProperty;
		public static DependencyProperty WindowTitleProperty;
		public static DependencyProperty WindowMaskVisibilityProperty;

		public static DependencyProperty WindowBorderThicknessProperty;
		public static DependencyProperty WindowActionRestoreVisibilityProperty;
		public static DependencyProperty WindowActionMaximizeVisibilityProperty;

		public static DependencyProperty WindowResizingVisibilityProperty;

		public MetroWindow()
		{
			StateChanged += OnStateChanged;
			SourceInitialized += OnSourceInitialized;

			// Update the current state, for old times sake
// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			OnStateChanged(null);
		}

		static MetroWindow()
		{
			WindowStatusProperty = 
				DependencyProperty.Register("WindowStatus", typeof(String), typeof(MetroWindow));
			WindowTitleProperty = 
				DependencyProperty.Register("WindowTitle", typeof(String), typeof(MetroWindow));
			WindowMaskVisibilityProperty = 
				DependencyProperty.Register("WindowMaskVisibility", typeof(Boolean), typeof(MetroWindow));

			WindowBorderThicknessProperty =
				DependencyProperty.Register("WindowBorderThickness", typeof(Thickness), typeof(MetroWindow));
			WindowActionRestoreVisibilityProperty =
				DependencyProperty.Register("WindowActionRestoreVisibility", typeof(Visibility), typeof(MetroWindow));
			WindowActionMaximizeVisibilityProperty =
				DependencyProperty.Register("WindowActionMaximizeVisibility", typeof(Visibility), typeof(MetroWindow));

			WindowResizingVisibilityProperty =
				DependencyProperty.Register("WindowResizingVisibility", typeof(Visibility), typeof(MetroWindow));
		}

		#region Properties

		#region Window

		public String WindowStatus
		{
			get { return (String)GetValue(WindowStatusProperty); }
			set { SetField(WindowStatusProperty, value); }
		}

		public String WindowTitle
		{
			get { return (String)GetValue(WindowTitleProperty); }
			set { SetField(WindowTitleProperty, value); }
		}

		public Boolean WindowMaskVisibility
		{
			get { return (Boolean)GetValue(WindowMaskVisibilityProperty); }
			set { SetField(WindowMaskVisibilityProperty, value); }
		}

		#endregion

		#region Window Actions

		public Thickness WindowBorderThickness
		{
			get { return (Thickness)GetValue(WindowBorderThicknessProperty); }
			set { SetField(WindowBorderThicknessProperty, value); }
		}

		public Visibility WindowActionRestoreVisibility
		{
			get { return (Visibility)GetValue(WindowActionRestoreVisibilityProperty); }
			set { SetField(WindowActionRestoreVisibilityProperty, value); }
		}

		public Visibility WindowActionMaximizeVisibility
		{
			get { return (Visibility)GetValue(WindowActionMaximizeVisibilityProperty); }
			set { SetField(WindowActionMaximizeVisibilityProperty, value); }
		}

		#endregion

		#region Resizing

		public Visibility WindowResizingVisibility
		{
			get { return (Visibility)GetValue(WindowResizingVisibilityProperty); }
			set { SetField(WindowResizingVisibilityProperty, value); }
		}

		#endregion

		#endregion

		#region Events

		private void OnStateChanged(object sender, EventArgs eventArgs)
		{
			switch (WindowState)
			{
				case WindowState.Normal:
					WindowBorderThickness = new Thickness(1, 1, 1, 25);
					WindowActionRestoreVisibility = Visibility.Collapsed;
					WindowActionMaximizeVisibility = Visibility.Visible;
					WindowResizingVisibility = Visibility.Visible;
					break;
				case WindowState.Maximized:
					WindowBorderThickness = new Thickness(0, 0, 0, 25);
					WindowActionRestoreVisibility = Visibility.Visible;
					WindowActionMaximizeVisibility = Visibility.Collapsed;
					WindowResizingVisibility = Visibility.Collapsed;
					break;
			}
		}

		private void OnSourceInitialized(object sender, EventArgs eventArgs)
		{
			var handle = (new WindowInteropHelper(this)).Handle;
			var hwndSource = HwndSource.FromHwnd(handle);
			if (hwndSource != null)
				hwndSource.AddHook(WindowProc);
		}

		#region Maximize Workspace Workarounds

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

				/*
				mmi.ptMaxPosition.x = Math.Abs(scrn.Bounds.Left - scrn.WorkingArea.Left);
				mmi.ptMaxPosition.y = Math.Abs(scrn.Bounds.Top - scrn.WorkingArea.Top);
				mmi.ptMaxSize.x = Math.Abs(scrn.Bounds.Right - scrn.WorkingArea.Left);
				mmi.ptMaxSize.y = Math.Abs(scrn.Bounds.Bottom - scrn.WorkingArea.Top);
				*/
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
				Width = xAdjust;
			if (yAdjust > MinHeight)
				Height = yAdjust;
		}

		public void ResizeRightThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			var xAdjust = Width + e.HorizontalChange;

			if (xAdjust > MinWidth)
				Width = xAdjust;
		}

		public void ResizeBottomThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			var yAdjust = Height + e.VerticalChange;

			if (yAdjust > MinHeight)
				Height = yAdjust;
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

		#region Binding

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		protected bool SetField<T>(DependencyProperty property, T value, 
			[CallerMemberName] string propertyName = "")
		{
			SetValue(property, value);
			OnPropertyChanged(propertyName);
			return true;
		}

		#endregion
	}
}
