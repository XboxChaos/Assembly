namespace Atlas.Views.CacheEditors.TagEditorComponents
{
	/// <summary>
	///     Interaction logic for TagBlock.xaml
	/// </summary>
	public partial class TagBlock
	{
		public TagBlock()
		{
			InitializeComponent();

			// Set Information box
			infoToggle.IsChecked = App.Storage.Settings.TagEditorShowBlockInformation;
		}
	}
}