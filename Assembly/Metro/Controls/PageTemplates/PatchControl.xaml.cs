using System.Windows.Controls;

namespace Assembly.Metro.Controls.PageTemplates
{
    /// <summary>
    /// Interaction logic for PatchControl.xaml
    /// </summary>
    public partial class PatchControl
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
