using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Blamite.Blam;
using Blamite.Flexibility;
using Assembly.Helpers.Tags;
using Blamite.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	/// <summary>
	///     Interaction logic for MetaInformation.xaml
	/// </summary>
	public partial class MetaInformation : UserControl
	{
		public MetaInformation(EngineDescription buildInfo, ITag tag, ICacheFile cache)
		{
			InitializeComponent();

			var name = cache.FileNames.GetTagName(tag) ?? tag.Index.ToString();
			lblTagName.Text = name + "." + CharConstant.ToString(tag.Class.Magic);

			lblDatum.Text = string.Format("Datum Index: {0}", tag.Index);
			lblAddress.Text = string.Format("Memory Address: 0x{0:X8}", tag.MetaLocation.AsPointer());
			lblOffset.Text = string.Format("File Offset: 0x{0:X}", tag.MetaLocation.AsOffset());
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