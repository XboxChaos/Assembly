using System.Windows.Controls;
using System.Windows.Input;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for metaBlock.xaml
	/// </summary>
	public partial class RawBlock : UserControl
	{
		private static RoutedCommand _allocateCommand = new RoutedCommand();
		private static RoutedCommand _isolateCommand = new RoutedCommand();
		public static RoutedCommand AllocateCommand { get { return _allocateCommand; } }
		public static RoutedCommand IsolateCommand { get { return _isolateCommand; } }

		public RawBlock()
		{
			InitializeComponent();
		}

		private void AllocateCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void IsolateCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}
	}
}