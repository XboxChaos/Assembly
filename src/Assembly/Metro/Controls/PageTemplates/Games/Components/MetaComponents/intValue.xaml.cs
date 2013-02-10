using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Assembly.Metro.Dialogs;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
    /// <summary>
    /// Interaction logic for intValue.xaml
    /// </summary>
    public partial class intValue : UserControl
    {
        public intValue()
        {
            InitializeComponent();
        }

        /*private void viewValueAs_Click(object sender, RoutedEventArgs e)
        {
            MetaData.ValueField value = this.DataContext as MetaData.ValueField;
            MetroViewValueAs.Show(value.MemoryAddress, value.CacheOffset);
        }*/
    }
}
