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
    /// Interaction logic for MapHeaderEntry.xaml
    /// </summary>
    public partial class MapHeaderEntry : UserControl
    {
        public MapHeaderEntry(string entryType, string entryData)
        {
            InitializeComponent();

            lblEntryType.Text = entryType;
            lblEntryData.Text = entryData;
        }
    }
}
