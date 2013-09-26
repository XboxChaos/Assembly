using Blamite.Blam;
using Blamite.Flexibility;
using Blamite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
    /// <summary>
    /// Interaction logic for MetaInformation.xaml
    /// </summary>
    public partial class MetaInformation : UserControl
    {
        public MetaInformation(EngineDescription buildInfo, TagEntry tag, ICacheFile cache)
        {
            InitializeComponent();

            lblTagName.Text = tag.TagFileName != null ?
                tag.TagFileName :
                "0x" + tag.RawTag.Index.Value.ToString("X");

            lblDatum.Text = string.Format("Datum Index: {0}", tag.RawTag.Index);
            lblAddress.Text = string.Format("Memory Address: 0x{0:X8}", tag.RawTag.MetaLocation.AsPointer());
            lblOffset.Text = string.Format("File Offset: 0x{0:X}", tag.RawTag.MetaLocation.AsOffset());
        }
    }
}
