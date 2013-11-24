using System.Windows.Controls;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	/// <summary>
	///     Interaction logic for MapHeaderEntry.xaml
	/// </summary>
	public partial class MapHeaderEntry : UserControl
	{
		public MapHeaderEntry(string entryType, string entryData)
		{
			InitializeComponent();

			lblEntryType.Text = entryType;
			lblEntryData.Text = entryData;
		}
	}
}