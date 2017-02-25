using System.Windows;
using Assembly.Helpers.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for MessageBox.xaml
	/// </summary>
	public partial class TagSearchBox
	{
		public TagSearchBox(string title, string message)
		{
			InitializeComponent();
			DwmDropShadow.DropShadowToWindow(this);

			lblTitle.Text = title;
			lblSubInfo.Text = message;
		}

		private void btnOkay_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

        /// <summary>
        ///     Converts a TagClass to an index in the class list.
        /// </summary>
        [ValueConversion(typeof(TagClass), typeof(int))]
        internal class TagClassConverter : DependencyObject, IValueConverter
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

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var sourceClass = (TagClass)value;
                int index = TagsSource.Classes.IndexOf(sourceClass);
                return index + 1;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                int adjustedIndex = (int)value - 1;
                if (adjustedIndex >= 0 && adjustedIndex < TagsSource.Classes.Count)
                    return TagsSource.Classes[adjustedIndex];
                return null;
            }
        }
    }
}