using System;
using Assembly.Helpers;

namespace Assembly.Metro.Dialogs
{
    public static class MetroException
    {
        /// <summary>
        /// Show the exception error dialog window.
        /// </summary>
        /// <param name="ex">The exception to pass into the dialog.</param>
        public static void Show(Exception ex)
        {
            // Run it though the dictionary, see if it can be made more user-friendlyKK

            ex = ExceptionDictionary.GetFriendlyException(ex);

            Settings.homeWindow.ShowMask();
			var exceptionDialog = new ControlDialogs.Exception(ex)
				                      {
					                      Owner = Settings.homeWindow,
					                      WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
				                      };
	        exceptionDialog.ShowDialog();
            Settings.homeWindow.HideMask();
        }
    }
}