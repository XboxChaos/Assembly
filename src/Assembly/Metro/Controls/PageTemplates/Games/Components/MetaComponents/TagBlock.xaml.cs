using System.Windows.Controls;
using System.Windows.Input;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for TagBlock.xaml
	/// </summary>
	public partial class TagBlock : UserControl
	{
		public static RoutedCommand ReallocateCommand = new RoutedCommand();
		public static RoutedCommand IsolateCommand = new RoutedCommand();

		public TagBlock()
		{
			InitializeComponent();

			// Set Information box
			infoToggle.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowInformation;
		}

		private void ReallocateCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void IsolateCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}
	}
}