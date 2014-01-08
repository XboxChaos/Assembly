namespace Atlas.Pages.CacheEditors.TagEditorComponents
{
	/// <summary>
	///     Interaction logic for MetaChunk.xaml
	/// </summary>
	public partial class MetaChunk
	{
		public MetaChunk()
		{
			InitializeComponent();

			// Set Information box
			infoToggle.IsChecked = App.Storage.Settings.ShowPluginInformation;
		}
	}
}