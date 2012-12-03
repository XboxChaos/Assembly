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
using Microsoft.Win32;
using Assembly.Metro.Dialogs;

namespace Assembly.Metro.Controls.PageTemplates
{
    /// <summary>
    /// Interaction logic for PatchControl.xaml
    /// </summary>
    public partial class PatchControl : UserControl
    {
        private bool _doingWork = false;

        public PatchControl()
        {
            InitializeComponent();
        }
        
        public bool Close()
        {
            return !_doingWork;
        }
    }
}
