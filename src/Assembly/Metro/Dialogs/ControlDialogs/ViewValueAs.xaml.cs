using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Assembly.Metro.Native;
using Assembly.Windows;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for ViewValueAs.xaml
    /// </summary>
    public partial class ViewValueAs
    {
	    private uint _cacheOffset;
	    private readonly uint _cacheOffsetOriginal;
        private readonly ICacheFile _cacheFile;
        private readonly MetaReader _reader;
        private readonly IList<MetaField> _fields;

        public ViewValueAs(ICacheFile cacheFile, BuildInformation buildInfo, IStreamManager streamManager, IList<MetaField> fields, uint cacheOffset)
        {
            InitializeComponent();

            DwmDropShadow.DropShadowToWindow(this);

            _cacheFile = cacheFile;
            _reader = new MetaReader(streamManager, cacheOffset, cacheFile, buildInfo);
            _fields = fields;
            _cacheOffset = _cacheOffsetOriginal = cacheOffset;

            // Set Textbox
            txtOffset.Text = "0x" + _cacheOffset.ToString("X");

            // Load Meta
            RefreshMeta();
        }

        public void RefreshMeta()
        {
            _reader.ReadFields(_fields);
            panelMetaComponents.ItemsSource = _fields;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ResizeDrop_DragDelta(object sender, DragDeltaEventArgs e)
        {
			var yadjust = Height + e.VerticalChange;
			var xadjust = Width + e.HorizontalChange;

            if (xadjust > MinWidth)
                Width = xadjust;
            if (yadjust > MinHeight)
                Height = yadjust;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
			int offset;

            // Validate Textbox
	        bool success;
            if (txtOffset.Text.StartsWith("0x") || txtOffset.Text.StartsWith("0X"))
            {
                // Is Hex
                success = int.TryParse(txtOffset.Text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
            }
            else
            {
                // Not Hex
                success = int.TryParse(txtOffset.Text, out offset);
            }

            if (!success || offset < _cacheFile.MetaArea.Offset || offset >= _cacheFile.MetaArea.Offset + _cacheFile.MetaArea.Size)
            {
                MetroMessageBox.Show(
                    "Invalid offset.",
                    "The meta offset you set is not valid. It might be beyond the boundaries of the file or contain invalid characters. Remember, if it's a hex number, it must start with a '0x'."
                );
            }

            _cacheOffset = (uint)offset;
            RefreshMeta();
        }
        private void txtOffset_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                btnRefresh_Click(null, null);
        }
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            _cacheOffset = _cacheOffsetOriginal;
            txtOffset.Text = "0x" + _cacheOffset.ToString("X");
            btnRefresh_Click(null, null);
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            _cacheOffset -= 1;
            txtOffset.Text = "0x" + _cacheOffset.ToString("X");
            btnRefresh_Click(null, null);
        }
        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            _cacheOffset += 1;
            txtOffset.Text = "0x" + _cacheOffset.ToString("X");
            btnRefresh_Click(null, null);
        }
    }
}
