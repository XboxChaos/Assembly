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

namespace Assembly.Metro.Dialogs.ControlDialogs.Controls
{
    /// <summary>
    /// Interaction logic for RevisionBlock.xaml
    /// </summary>
    public partial class RevisionBlock : UserControl
    {
        public RevisionBlock(string researcher, int version, string description)
        {
            InitializeComponent();

            pluginExpander.Header = researcher;
            txtPluginRevisionResearcher.Text = researcher;
            txtPluginRevisionNumber.Text = version.ToString();
            txtPluginRevisionDescrption.Text = "                               " + description;
        }
    }
}
