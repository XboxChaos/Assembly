using System;
using Assembly.Helpers;

namespace Assembly.Metro.Dialogs
{
    public static class MetroException
    {
        /// <summary>
        /// Because you are the only exception.
        /// </summary>
        /// <param name="ex">The exception to pass into the dialog.</param>
        public static void Show(Exception ex)
        {
            // Run it though the dictionary, see if it can be made more user-friendlyKK

            ex = ExceptionDictionary.GetFriendlyException(ex);

			if (Settings.homeWindow != null)
				Settings.homeWindow.ShowMask();

            var exceptionDialog = Settings.homeWindow != null ? new ControlDialogs.Exception(ex)
	                                                                {
		                                                                Owner = Settings.homeWindow,
																		WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
	                                                                } : new ControlDialogs.Exception(ex);
			exceptionDialog.ShowDialog();

			if (Settings.homeWindow != null)
				Settings.homeWindow.HideMask();
        }
    }
}