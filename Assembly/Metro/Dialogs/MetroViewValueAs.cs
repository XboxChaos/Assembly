using Assembly.Backend;
using Assembly.Metro.Dialogs.ControlDialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Assembly.Metro.Dialogs
{
    public class MetroViewValueAs
    {
        /// <summary>
        /// View the selected offset as every meta value type.
        /// </summary>
        /// <param name="memoryAddress">The base offset of the value, from memory of the Xbox Console.</param>
        /// <param name="cacheOffset">The base offset of the value, from the cache file.</param>
        public static void Show(uint memoryAddress, uint cacheOffset)
        {
            ViewValueAs valueAs = new ViewValueAs(memoryAddress, cacheOffset);
            valueAs.Owner = Settings.homeWindow;
            valueAs.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            valueAs.Show();
        }
    }
}
