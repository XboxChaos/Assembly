using Assembly.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
    /// <summary>
    /// Interaction logic for StringIDValue.xaml
    /// </summary>
    public partial class StringIDValue : UserControl
    {
        private ObservableCollection<string> _stringIDs = new ObservableCollection<string>();

        public ObservableCollection<string> StringIDs
        {
            get { return _stringIDs; }
        }

        public StringIDValue()
        {
            InitializeComponent();
            cbStringIDs.ItemsSource = Settings.selectedHaloMap.CacheFile.StringIDs;
        }
    }
}
