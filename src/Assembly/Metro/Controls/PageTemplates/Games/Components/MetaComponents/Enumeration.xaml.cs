using System.Windows.Controls;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for Enumeration.xaml
	/// </summary>
	public partial class Enumeration : UserControl
	{
		public Enumeration()
		{
			InitializeComponent();

			indexToggle.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowEnumIndex;
		}
	}
}