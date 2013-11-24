using System.Windows.Controls;
using Blamite.Blam;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
	/// <summary>
	///     Interaction logic for StringEditor.xaml
	/// </summary>
	public partial class StringEditor : UserControl
	{
		private ICacheFile _cache;

		public StringEditor(ICacheFile cache)
		{
			InitializeComponent();

			_cache = cache;
		}
	}
}