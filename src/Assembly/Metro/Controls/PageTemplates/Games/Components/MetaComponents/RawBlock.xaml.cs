using System.Windows.Controls;
using System.Windows.Input;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for metaBlock.xaml
	/// </summary>
	public partial class RawBlock : UserControl
	{
		public static RoutedCommand AllocateCommand = new RoutedCommand();
		public static RoutedCommand IsolateCommand = new RoutedCommand();

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