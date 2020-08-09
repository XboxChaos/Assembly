using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using System.Windows.Controls;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for DatumValue.xaml
	/// </summary>
	public partial class DatumValue : UserControl
	{
		public DatumValue()
		{
			InitializeComponent();
		}

		private void btnNull_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			DatumData datum = (DatumData)DataContext;
			datum.Salt = datum.Index = ushort.MaxValue;
		}
	}
}