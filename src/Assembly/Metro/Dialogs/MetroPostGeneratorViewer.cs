using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assembly.Helpers;

namespace Assembly.Metro.Dialogs
{
	public static class MetroPostGeneratorViewer
    {
        /// <summary>
        /// Show a Metro Post Generator Viewer
        /// </summary>
		/// <param name="bbcode">The generated BBCode of the post.</param>
		/// <param name="modAuthor">The author of the mod.</param>
        public static void Show(string bbcode, string modAuthor)
        {
            Settings.homeWindow.ShowMask();
			var msgBox = new ControlDialogs.PostGeneratorViewer(bbcode, modAuthor)
				             {
					             Owner = Settings.homeWindow, 
								 WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
				             };
	        msgBox.ShowDialog();
            Settings.homeWindow.HideMask();
        }
    }
}