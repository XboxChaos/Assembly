using System.Collections.Generic;
using System.Windows;
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Assembly.Metro.Dialogs.ControlDialogs;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.IO;

namespace Assembly.Metro.Dialogs
{
    public class MetroViewValueAs
    {
        /// <summary>
        /// View the selected offset as every meta value type.
        /// </summary>
        /// <param name="cacheFile">The cache file which is being read from.</param>
        /// <param name="streamManager">The stream manager to open the file with.</param>
        /// <param name="fields">The fields to display in the viewer.</param>
        /// <param name="cacheOffset">The initial offset to display.</param>
        public static void Show(ICacheFile cacheFile, IStreamManager streamManager, IList<MetaField> fields, uint cacheOffset)
        {
            ViewValueAs valueAs = new ViewValueAs(cacheFile, streamManager, fields, cacheOffset);
            valueAs.Owner = Settings.homeWindow;
            valueAs.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            valueAs.Show();
        }
    }
}
