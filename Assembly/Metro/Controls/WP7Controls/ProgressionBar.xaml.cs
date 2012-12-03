using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Assembly.Metro.Controls.WP7Controls
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var percentage = Double.Parse(parameter.ToString(), new CultureInfo("en-GB"));
            return ((double)value) * percentage;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Interaction logic for ProgressionBar.xaml
    /// </summary>
    public partial class ProgressionBar
    {
        public ProgressionBar()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public static readonly DependencyProperty ProgressColourProperty = DependencyProperty.RegisterAttached("ProgressColour", typeof(Brush), typeof(ProgressionBar), new UIPropertyMetadata(null));

        public Brush ProgressColour
        {
            get { return (Brush)GetValue(ProgressColourProperty); }
            set { SetValue(ProgressColourProperty, value); }
        }

        public void Stop()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                var s = this.Resources["animate"] as Storyboard;
                s.Stop();
                this.Visibility = Visibility.Collapsed;
            })
                );
        }
    }
}
