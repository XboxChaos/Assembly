using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Xml;
using Assembly.Backend;
using Assembly.Backend.Plugins;
using Assembly.Metro.Controls.PageTemplates.Games;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Assembly.Metro.Native;
using Assembly.Windows;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.IO;
using ExtryzeDLL.Plugins;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for ViewValueAs.xaml
    /// </summary>
    public partial class ViewValueAs : Window
    {
        private uint _cacheOffset, _cacheOffsetOriginal;
        private string _examplePath;
        private Stream _stream;
        private MetaReader _reader;
        private IList<MetaField> _fields;

        public ViewValueAs(ICacheFile cacheFile, Stream stream, IList<MetaField> fields, uint cacheOffset)
        {
            InitializeComponent();

            DwmDropShadow.DropShadowToWindow(this);

            _stream = stream;
            _reader = new MetaReader(new EndianReader(stream, Endian.BigEndian), cacheOffset, cacheFile);
            _fields = fields;
            _cacheOffset = _cacheOffsetOriginal = cacheOffset;

            // Set Textbox
            txtOffset.Text = "0x" + _cacheOffset.ToString("X");
            btnRefresh_Click(null, null);

            // Load Plugin Path
            _examplePath = string.Format("{0}\\Examples\\ThirdGenExample.xml", VariousFunctions.GetApplicationLocation() + @"Plugins");

            // Load Meta
            RefreshMeta();
        }

        public void RefreshMeta()
        {
            if (File.Exists(_examplePath))
            {
                // Load Example Plugin File
                XmlReader xml = XmlReader.Create(_examplePath);

                // Load Meta
                //try
                {
                    _reader.ReadFields(_fields);
                    panelMetaComponents.ItemsSource = _fields;
                }
                //catch (Exception ex)
                //{
                //    MetroException.Show(ex);
                //}
            }
            else
            {
                StatusUpdater.Update("Example Plugin doesn't exist... I don't know why you deleted it :(.");
                this.Close();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void headerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Left = Left + e.HorizontalChange;
            Top = Top + e.VerticalChange;
        }

        private void ResizeDrop_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double yadjust = this.Height + e.VerticalChange;
            double xadjust = this.Width + e.HorizontalChange;

            if (xadjust > this.MinWidth)
                this.Width = xadjust;
            if (yadjust > this.MinHeight)
                this.Height = yadjust;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            int offset = -1;

            // Validate Textbox
            bool success = false;
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

            if (!success || offset < 0 || offset >= _stream.Length)
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

        private void ViewValueAs_Closed_1(object sender, EventArgs e)
        {
            _stream.Close();
        }
    }
}
