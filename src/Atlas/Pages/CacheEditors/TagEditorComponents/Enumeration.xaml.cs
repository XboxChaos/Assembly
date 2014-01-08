using System.Windows.Controls;

namespace Atlas.Pages.CacheEditors.TagEditorComponents
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