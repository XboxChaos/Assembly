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
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
    /// <summary>
    /// Interaction logic for tagValue.xaml
    /// </summary>
    public partial class tagValue : UserControl
    {
        public tagValue()
        {
            InitializeComponent();
        }

        private void cbTagClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool enable = (cbTagClass.SelectedIndex > 0);
            cbTagEntry.IsEnabled = enable;
            btnJumpToTag.IsEnabled = enable;
        }

        private void btnJumpToTag_Click(object sender, RoutedEventArgs e)
        {
            // Jump to tag
            if (Settings.selectedHaloMap != null)
            {
                if (cbTagClass.SelectedIndex > 0)
                    Settings.selectedHaloMap.OpenTag((TagEntry)cbTagEntry.SelectionBoxItem);
            }
        }

        private void cbTagEntry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbTagEntry.SelectedIndex < 0 && cbTagClass.SelectedIndex > 0)
                cbTagEntry.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Converts a TagClass to an index in the class list.
    /// </summary>
    [ValueConversion(typeof(TagClass), typeof(int))]
    class TagClassConverter : DependencyObject, IValueConverter
    {
        public static DependencyProperty TagsSourceProperty = DependencyProperty.Register(
            "TagsSource",
            typeof(TagHierarchy),
            typeof(TagClassConverter)
        );

        public TagHierarchy TagsSource
        {
            get { return (TagHierarchy)GetValue(TagsSourceProperty); }
            set { SetValue(TagsSourceProperty, value); }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TagClass sourceClass = (TagClass)value;
            int index = TagsSource.Classes.IndexOf(sourceClass);
            return index + 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int adjustedIndex = (int)value - 1;
            if (adjustedIndex >= 0 && adjustedIndex < TagsSource.Classes.Count)
                return TagsSource.Classes[adjustedIndex];
            return null;
        }
    }

    [ValueConversion(typeof(TagClass), typeof(List<TagEntry>))]
    class TagEntryListRetriever : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TagClass sourceClass = value as TagClass;
            if (sourceClass == null)
                return null;
            return sourceClass.Children;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
