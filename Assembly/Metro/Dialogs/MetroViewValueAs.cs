using Assembly.Backend;
using Assembly.Metro.Dialogs.ControlDialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using ExtryzeDLL.Blam.ThirdGen;
using System.IO;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;

namespace Assembly.Metro.Dialogs
{
    public class MetroViewValueAs
    {
        /// <summary>
        /// View the selected offset as every meta value type.
        /// </summary>
        /// <param name="cacheFile">The cache file which is being read from.</param>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="fields">The fields to display in the viewer.</param>
        /// <param name="cacheOffset">The initial offset to display.</param>
        public static void Show(ICacheFile cacheFile, Stream stream, IList<MetaField> fields, uint cacheOffset)
        {
            ViewValueAs valueAs = new ViewValueAs(cacheFile, stream, fields, cacheOffset);
            valueAs.Owner = Settings.homeWindow;
            valueAs.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            valueAs.Show();
        }
    }
}
