using System.Windows.Controls;
using System.Windows.Input;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for MetaChunk.xaml
	/// </summary>
	public partial class MetaChunk : UserControl
	{
		public static RoutedCommand ReallocateCommand = new RoutedCommand();

		public MetaChunk()
		{
			InitializeComponent();

			// Set Information box
			infoToggle.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowInformation;
		}

		private void ReallocateCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}
	}
}