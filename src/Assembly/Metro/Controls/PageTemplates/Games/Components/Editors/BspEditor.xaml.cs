using System.Windows.Controls;
using Blamite.Blam;
using Blamite.IO;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
	/// <summary>
	///     Interaction logic for BspEditor.xaml
	/// </summary>
	public partial class BspEditor : Page
	{
		private readonly ICacheFile _cache;
		private readonly IStreamManager _streamManager;
		private readonly ITag _tag;

		public BspEditor(ITag tag, ICacheFile cache, IStreamManager streamManager)
		{
			InitializeComponent();

			_tag = tag;
			_cache = cache;
			_streamManager = streamManager;
		}
	}
}