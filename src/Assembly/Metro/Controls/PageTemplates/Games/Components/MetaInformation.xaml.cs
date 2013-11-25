using System.Windows;
using System.Windows.Input;
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
				? tag.TagFileName + "." + tag.ClassName
				: "0x" + tag.RawTag.Index.Value.ToString("X");

			lblDatum.Text = string.Format("Datum Index: {0}", tag.RawTag.Index);
			lblAddress.Text = string.Format("Memory Address: 0x{0:X8}", tag.RawTag.MetaLocation.AsPointer());
			lblOffset.Text = string.Format("File Offset: 0x{0:X}", tag.RawTag.MetaLocation.AsOffset());
		}
        private void MetaDatumValueData_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                Clipboard.SetText(((TextBlock)e.OriginalSource).Text.Substring(13));
        }
        private void MetaAddrValueData_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                Clipboard.SetText(((TextBlock)e.OriginalSource).Text.Substring(16));
        }
        private void MetaOffsetValueData_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                Clipboard.SetText(((TextBlock)e.OriginalSource).Text.Substring(13));
        }
        private void MetaNameValueData_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                Clipboard.SetText(((TextBlock)e.OriginalSource).Text);
        }
	}
}