using System;
using System.Windows;
using Assembly.Helpers;

namespace Assembly.Metro.Dialogs
{
	public static class MetroException
	{
		/// <summary>
		///     Because you are the only exception.
		/// </summary>
		/// <param name="ex">The exception to pass into the dialog.</param>
		public static void Show(Exception ex)
		{
			// Run it though the dictionary, see if it can be made more user-friendly

			ex = ExceptionDictionary.GetFriendlyException(ex);

			if (App.AssemblyStorage?.AssemblySettings.HomeWindow != null)
				App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();

			ControlDialogs.Exception exceptionDialog = App.AssemblyStorage?.AssemblySettings.HomeWindow != null
				? new ControlDialogs.Exception(ex)
				{
					Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
					WindowStartupLocation = WindowStartupLocation.CenterOwner
				}
				: new ControlDialogs.Exception(ex);
			exceptionDialog.ShowDialog();

			if (App.AssemblyStorage?.AssemblySettings.HomeWindow != null)
				App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();
		}
	}
}