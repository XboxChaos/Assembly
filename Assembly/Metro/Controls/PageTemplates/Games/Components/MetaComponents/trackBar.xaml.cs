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
    /// Interaction logic for TrackBar.xaml
    /// </summary>
    public partial class TrackBar : UserControl, IMetaComponent
    {
        double minimum, maximum, small_change, large_change;

        public TrackBar()
        {
            InitializeComponent();
        }

        public void LoadValues(string title, string type, double min, double max, double smallStep, double largeStep)
        {
            lblName.ToolTip = lblName.Text = title;
            minimum = min;
            maximum = max;
            small_change = smallStep;
            large_change = largeStep;
        }

        public void Accept(IMetaComponentVisitor visitor)
        {
            visitor.VisitTrackBar(this);
        }
    }
}
