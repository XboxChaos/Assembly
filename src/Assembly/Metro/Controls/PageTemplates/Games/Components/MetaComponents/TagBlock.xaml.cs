using System.Windows.Controls;
using System.Windows.Input;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for TagBlock.xaml
	/// </summary>
	public partial class TagBlock : UserControl
	{
		private static RoutedCommand _reallocateCommand = new RoutedCommand();
		private static RoutedCommand _isolateCommand = new RoutedCommand();
		public static RoutedCommand ReallocateCommand { get { return _reallocateCommand; } }
		public static RoutedCommand IsolateCommand { get { return _isolateCommand; } }

		public TagBlock()
		{
			InitializeComponent();

			// Set Information box
			infoToggle.IsChecked = App.AssemblyStorage.AssemblySettings.PluginsShowInformation;
		}
	}
}