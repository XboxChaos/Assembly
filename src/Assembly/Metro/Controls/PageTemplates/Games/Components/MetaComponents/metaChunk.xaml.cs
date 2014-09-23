using System.Windows.Controls;
using System.Windows.Input;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for MetaChunk.xaml
	/// </summary>
	public partial class MetaChunk : UserControl
	{
		public static RoutedCommand EditTagBlockCommand = new RoutedCommand();

		public MetaChunk()
		{
			InitializeComponent();

			// Set Information box
			infoToggle.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowInformation;
		}
	}
}