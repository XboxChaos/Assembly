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

namespace Extryze.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
    /// <summary>
    /// Interaction logic for metaBlock.xaml
    /// </summary>
    public partial class metaBlock : UserControl, IMetaComponent
    {
        long offset;

        public metaBlock()
        {
            InitializeComponent();
        }

        public void LoadValues(long off, string name)
        {
            offset = off;
            lblTitle.Content = name;
        }
    
        public void Accept(IMetaComponentVisitor visitor)
        {
            visitor.VisitMetaBlock(this);
        }
    }
}
