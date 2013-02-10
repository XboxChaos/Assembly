using System;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Assembly.Metro.Native
{
    public static class DwmDropShadow
    {

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMarInset);

        /// <summary>
        /// Drops a standard shadow to a WPF Window, even if the window isborderless. Only works with DWM (Vista and Seven).
        /// This method is much more efficient than setting AllowsTransparency to true and using the DropShadow effect,
        /// as AllowsTransparency involves a huge permormance issue (hardware acceleration is turned off for all the window).
        /// </summary>
        /// <param name="window">Window to which the shadow will be applied</param>
        public static void DropShadowToWindow(Window window)
        {
            if (!DropShadow(window))
            {
                window.SourceInitialized += window_SourceInitialized;
            }
        }

        private static void window_SourceInitialized(object sender, EventArgs e)
        {
            var window = (Window)sender;

            DropShadow(window);

            window.SourceInitialized -= window_SourceInitialized;
        }

        /// <summary>
        /// The actual method that makes API calls to drop the shadow to the window
        /// </summary>
        /// <param name="window">Window to which the shadow will be applied</param>
        /// <returns>True if the method succeeded, false if not</returns>
        private static bool DropShadow(Window window)
        {
            try
            {
				var helper = new WindowInteropHelper(window);
				var val = 2;
				var ret1 = DwmSetWindowAttribute(helper.Handle, 2, ref val, 4);

				if (ret1 != 0)
					return false;

	            var m = new Margins {Bottom = 0, Left = 0, Right = 0, Top = 0};
	            var ret2 = DwmExtendFrameIntoClientArea(helper.Handle, ref m);
	            return ret2 == 0;
            }
            catch
            {
                // Probably dwmapi.dll not found (incompatible OS)
                return false;
            }
        }
    }
}
