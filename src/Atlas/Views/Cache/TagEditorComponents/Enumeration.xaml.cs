namespace Atlas.Views.Cache.TagEditorComponents
{
	/// <summary>
	///     Interaction logic for Enumeration.xaml
	/// </summary>
	public partial class Enumeration
	{
		public Enumeration()
		{
			InitializeComponent();

			indexToggle.IsChecked = App.Storage.Settings.TagEditorShowEnumIndex;
		}
	}
}