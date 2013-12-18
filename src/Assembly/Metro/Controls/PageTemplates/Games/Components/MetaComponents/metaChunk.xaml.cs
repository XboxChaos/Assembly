using System.Windows.Controls;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for MetaChunk.xaml
	/// </summary>
	public partial class MetaChunk : UserControl
	{
		public MetaChunk()
		{
			InitializeComponent();

			// Set Invisibility box
			infoToggle.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowInformation;
		}
	}
}