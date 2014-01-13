using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Atlas.Native
{
	public static class WindowMovement
	{
		#region IsDraggable attached property

		public static readonly DependencyProperty IsDraggableProperty = DependencyProperty.RegisterAttached(
			"IsDraggable",
			typeof (bool),
			typeof (WindowMovement),
			new PropertyMetadata(false, IsDraggableChanged));

		/// <summary>
		///     Sets the value of the WindowMovement.IsDraggable attached property on a Window.
		/// </summary>
		/// <param name="window">The Window to set the property on.</param>
		/// <param name="value">The value to set the property to.</param>
		public static void SetIsDraggable(Window window, bool value)
		{
			window.SetValue(IsDraggableProperty, value);
		}

		/// <summary>
		///     Gets the value of the WindowMovement.IsDraggable attached property on a Window.
		/// </summary>
		/// <param name="window">The Window to set the property on.</param>
		/// <returns>The value of the property on the Window.</returns>
		public static bool GetIsDraggable(Window window)
		{
			return (bool) window.GetValue(IsDraggableProperty);
		}

		private static void IsDraggableChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var window = sender as Window;
			if (window == null)
				return;
			if (e.NewValue.Equals(e.OldValue))
				return;

			var newValue = (bool) e.NewValue;
			if (newValue)
			{
				// Make the window movable
				if (!MakeWindowMovable(window))
					window.SourceInitialized += WindowSourceInitialized;
				// The window isn't initialized, so wait for it to be before making it movable
			}
			else
			{
				// Make the window unmovable
				MakeWindowUnmovable(window);
				window.SourceInitialized -= WindowSourceInitialized;
			}
		}

		private static void WindowSourceInitialized(object sender, EventArgs e)
		{
			MakeWindowMovable((Window)sender);
		}

		#endregion

		#region DragsWindow attached property

		public static readonly DependencyProperty DragsWindow = DependencyProperty.RegisterAttached(
			"DragsWindow",
			typeof (bool),
			typeof (WindowMovement),
			new PropertyMetadata(false));

		/// <summary>
		///     Sets the value of the WindowMovement.DragsWindow attached property on a Visual.
		/// </summary>
		/// <param name="visual">The Visual to set the property on.</param>
		/// <param name="value">The value to set the property to.</param>
		public static void SetDragsWindow(Visual visual, bool value)
		{
			visual.SetValue(DragsWindow, value);
		}

		/// <summary>
		///     Gets the value of the WindowMovement.DragsWindow attached property on a Visual.
		/// </summary>
		/// <param name="visual">The Visual to set the property on.</param>
		/// <returns>The value of the property on the Visual.</returns>
		public static bool GetDragsWindow(Visual visual)
		{
			return (bool) visual.GetValue(DragsWindow);
		}

		/// <summary>
		///     Determines whether or not a DependencyObject has the WindowMovement.DragsWindow property set to true.
		/// </summary>
		/// <param name="visual">The DependencyObject to test for the property on.</param>
		/// <returns>true if the object has the attached property and it is set to true.</returns>
		public static bool HasDragsWindowEnabled(DependencyObject visual)
		{
			object dragsWindow = visual.ReadLocalValue(DragsWindow);
			return (dragsWindow != DependencyProperty.UnsetValue && (bool) dragsWindow);
		}

		#endregion

		private static readonly Dictionary<Window, CaptionHitTester> _registeredWindows =
			new Dictionary<Window, CaptionHitTester>();

		/// <summary>
		///     Gets a Window's HwndSource.
		/// </summary>
		/// <param name="window">The Window to get the HwndSource for.</param>
		/// <returns>The Window's HwndSource, or null if it is not initialized.</returns>
		private static HwndSource GetWindowSource(Window window)
		{
			var interop = new WindowInteropHelper(window);
			if (interop.Handle == IntPtr.Zero)
				return null;

			return HwndSource.FromHwnd(interop.Handle);
		}

		/// <summary>
		///     Makes a custom-shaped window draggable based upon clicks on objects with the WindowMovement.DragsWindow attribute.
		/// </summary>
		/// <param name="window">The Window to make draggable.</param>
		/// <returns>true if the operation was successful.</returns>
		private static bool MakeWindowMovable(Window window)
		{
			HwndSource source = GetWindowSource(window);
			if (source == null)
				return false;

			CaptionHitTester tester;
			if (!_registeredWindows.TryGetValue(window, out tester))
			{
				// Register a CaptionHitTester for the window
				tester = new CaptionHitTester(window);
				_registeredWindows[window] = tester;
				window.Closed += window_Closed; // Unregisters the window when it's closed
			}

			// Register the CaptionHitTester on the window (this is where the magic happens)
			tester.Hook(source);
			return true;
		}

		private static void window_Closed(object sender, EventArgs e)
		{
			// Unregister the window so the dictionary doesn't eat up memory
			_registeredWindows.Remove((Window)sender);
		}

		/// <summary>
		///     Makes a window affected by <see cref="MakeWindowMovable" /> no longer movable.
		/// </summary>
		/// <param name="window">The Window to make unmovable.</param>
		private static void MakeWindowUnmovable(Window window)
		{
			HwndSource source = GetWindowSource(window);
			if (source == null)
				return;

			CaptionHitTester tester;
			if (_registeredWindows.TryGetValue(window, out tester))
				tester.Unhook(source);
		}

		/// <summary>
		///     Hooks onto WPF windows and makes WM_NCHITTEST return HTCLIENT if the mouse is over
		///     a control with WindowMovement.DragsWindow set to True.
		/// </summary>
		private class CaptionHitTester
		{
			private readonly Window _window;
			private HitTestResult _testResult;

			public CaptionHitTester(Window window)
			{
				_window = window;
			}

			/// <summary>
			///     Registers the CaptionHitTester with an HwndSource, hooking into its window procedure.
			/// </summary>
			/// <param name="source">The HwndSource to hook into.</param>
			public void Hook(HwndSource source)
			{
				source.AddHook(HitTestHook);
			}

			/// <summary>
			///     Unregisters the CaptionHitTester with an HwndSource, removing its window procedure hook.
			/// </summary>
			/// <param name="source">The HwndSource to remove the hook from.</param>
			public void Unhook(HwndSource source)
			{
				source.RemoveHook(HitTestHook);
			}

			private IntPtr HitTestHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
			{
				if (msg == WM_NCHITTEST)
				{
					// Set handled to true because we need to do the default test anyway
					// and can just return defaultTest (below)
					handled = true;

					// Don't check if the mouse isn't over the client area
					IntPtr defaultTest = DefWindowProc(hwnd, msg, wParam, lParam);
					if ((int) defaultTest != HTCLIENT)
						return defaultTest;

					// Get the cursor position on the screen from lParam
					int screenPos = lParam.ToInt32();
					int x = (short) (screenPos & 0xFFFF); // Low word
					int y = (short) (screenPos >> 16); // High word

					// Get the position relative to the window
					Point clientPos = _window.PointFromScreen(new Point(x, y));

					// Check if the mouse is over the titlebar
					// (HitTestResult stores the result to _testResult)
					_testResult = null;
					VisualTreeHelper.HitTest(_window, HitTestFilter, HitTestResult, new PointHitTestParameters(clientPos));

					if (_testResult != null && _testResult.VisualHit != null)
					{
						// Only accept objects which have the WindowMovement.DragsWindow attached property set to true
						if (HasDragsWindowEnabled(_testResult.VisualHit))
							return (IntPtr) HTCAPTION; // Return HTCAPTION to make the WM think the titlebar was clicked
					}

					return defaultTest;
				}
				return IntPtr.Zero;
			}

			private HitTestFilterBehavior HitTestFilter(DependencyObject o)
			{
				// By default, VisualTreeHelper.HitTest ignores IsHitTestVisible
				var elem = o as UIElement;
				if (elem != null && (!elem.IsHitTestVisible || !elem.IsVisible))
					return HitTestFilterBehavior.ContinueSkipSelfAndChildren;

				return HitTestFilterBehavior.Continue;
			}

			private HitTestResultBehavior HitTestResult(HitTestResult result)
			{
				// Store the result and stop
				_testResult = result;
				return HitTestResultBehavior.Stop;
			}

			#region Native definitions

			private const int WM_NCHITTEST = 0x0084;

			private const int HTCLIENT = 1;
			private const int HTCAPTION = 2;

			[DllImport("user32.dll")]
			private static extern IntPtr DefWindowProc(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam);

			#endregion Native definitions
		}
	}
}