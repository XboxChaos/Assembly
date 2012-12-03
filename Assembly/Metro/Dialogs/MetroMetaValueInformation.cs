using Assembly.Backend;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Dialogs
{
    public class MetroMetaValueInformation
    {
        /// <summary>
        /// Show a Metro Meta Value Information Window
        /// </summary>
        /// <param name="value">The value data of </param>
        public static void Show(ValueField value)
        {
            Settings.homeWindow.ShowMask();
            ControlDialogs.MetaValueInformation metaValueInfo = new ControlDialogs.MetaValueInformation(value);
            metaValueInfo.Owner = Settings.homeWindow;
            metaValueInfo.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            metaValueInfo.ShowDialog();
            Settings.homeWindow.HideMask();
        }
    }
}
