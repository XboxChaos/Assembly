using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using System.Windows.Input;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
    /// <summary>
    /// Interaction logic for metaBlock.xaml
    /// </summary>
    public partial class rawBlock
    {
        public rawBlock()
        {
            InitializeComponent();

            // Fix WPF's stupidity by forcing the command targets for the editor's context menu items
            // Without this, the items are always disabled
            cutItem.CommandTarget = txtValue.TextArea;
            copyItem.CommandTarget = txtValue.TextArea;
            pasteItem.CommandTarget = txtValue.TextArea;
        }

        private void txtValue_MouseRightButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            // Move the cursor to the place where the click occurred (AvalonEdit doesn't do this by default)
            // http://community.sharpdevelop.net/forums/p/12521/34105.aspx
            var position = txtValue.GetPositionFromPoint(e.GetPosition(txtValue));
            if (position.HasValue)
                txtValue.TextArea.Caret.Position = position.Value;
        }

		private void txtValue_TextChanged(object sender, System.EventArgs e)
		{
			if (DataContext == null) return;

			var rawData = (RawData) DataContext;
			rawData.Document = txtValue.Document;
		}
    }
}
