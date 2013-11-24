using System.Windows.Controls;
using Blamite.Blam;
using Blamite.Flexibility;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	/// <summary>
	///     Interaction logic for MetaInformation.xaml
	/// </summary>
	public partial class MetaInformation : UserControl
	{
		public MetaInformation(EngineDescription buildInfo, TagEntry tag, ICacheFile cache)
		{
			InitializeComponent();

			lblTagName.Text = tag.TagFileName != null
				? tag.TagFileName
				: "0x" + tag.RawTag.Index.Value.ToString("X");

			lblDatum.Text = string.Format("Datum Index: {0}", tag.RawTag.Index);
			lblAddress.Text = string.Format("Memory Address: 0x{0:X8}", tag.RawTag.MetaLocation.AsPointer());
			lblOffset.Text = string.Format("File Offset: 0x{0:X}", tag.RawTag.MetaLocation.AsOffset());
		}
	}
}