using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Assembly.Metro.Native
{
    public static class WindowMovement
    {
        /// <summary>
        /// Makes a custom-shaped window draggable based upon clicks on a titlebar.
        /// </summary>
        /// <param name="window">The Window to make draggable.</param>
        /// <param name="titlebar">The visual that should act as the titlebar.</param>
        public static void MakeWindowMovable(Window window, Visual titlebar)
        {
            WindowInteropHelper interop = new WindowInteropHelper(window);
            HwndSource source = HwndSource.FromHwnd(interop.Handle);

            CaptionHitTester tester = new CaptionHitTester(window, titlebar);
            tester.Register(source);
        }

        private class CaptionHitTester
        {
            private readonly Window _window;
            private readonly Visual _titleBar;

            private HitTestResult _testResult = null;

            public CaptionHitTester(Window window, Visual titleBar)
            {
                _window = window;
                _titleBar = titleBar;
            }

            public void Register(HwndSource source)
            {
                source.AddHook(HitTestHook);
            }

            private IntPtr HitTestHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                switch (msg)
                {
                    case WM_NCHITTEST:
                        IntPtr defaultTest = DefWindowProc(hwnd, msg, wParam, lParam);
                        if ((int)defaultTest != HTCLIENT)
                            break;

                        // Get the cursor position on the screen from lParam
                        int screenPos = lParam.ToInt32();
                        int x = screenPos & 0xFFFF; // Low word
                        int y = screenPos >> 16; // High word

                        // Get the position relative to the window
                        Point clientPos = _window.PointFromScreen(new Point(x, y));

                        // Check if the mouse is over the titlebar
                        // (HitTestResult stores the result to _testResult)
                        _testResult = null;
                        VisualTreeHelper.HitTest(_titleBar, HitTestFilter, HitTestResult, new PointHitTestParameters(clientPos));
                        if (_testResult != null && _testResult.VisualHit == _titleBar)
                        {
                            // Return HTCAPTION to make the WM think the titlebar was clicked
                            handled = true;
                            return (IntPtr)HTCAPTION;
                        }
                        break;
                }
                return IntPtr.Zero;
            }

            private HitTestFilterBehavior HitTestFilter(DependencyObject o)
            {
                // By default, VisualTreeHelper.HitTest ignores IsHitTestVisible
                UIElement elem = o as UIElement;
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

            private const int WM_NCHITTEST = 0x0084;

            private const int HTCLIENT = 1;
            private const int HTCAPTION = 2;

            [DllImport("user32.dll")]
            private static extern IntPtr DefWindowProc(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam);
        }
    }
}
