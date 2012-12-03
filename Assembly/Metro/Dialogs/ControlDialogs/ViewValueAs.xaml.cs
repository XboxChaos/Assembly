using Assembly.Backend;
using Assembly.Backend.Plugins;
using Assembly.Metro.Controls.PageTemplates.Games;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Assembly.Metro.Native;
using Assembly.Windows;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for ViewValueAs.xaml
    /// </summary>
    public partial class ViewValueAs : Window
    {
        private uint _memoryAddress, _cacheOffset, _cacheOffsetOriginal;
        private string _examplePath;
        private HaloMap _cache;
        private ThirdGenPluginVisitor _pluginVisitor;

        public ViewValueAs(uint memoryAddress, uint cacheOffset)
        {
            InitializeComponent();

            DwmDropShadow.DropShadowToWindow(this);

            _memoryAddress = memoryAddress;
            _cacheOffset = _cacheOffsetOriginal = cacheOffset;

            // Set Textbox
            txtOffset.Text = "0x" + _cacheOffset.ToString("X");
            btnRefresh_Click(null, null);

            // Load Plugin Path
            _examplePath = string.Format("{0}\\Examples\\ThirdGenExample.xml", VariousFunctions.GetApplicationLocation() + @"Plugins");

            // Halo Cache from selected Editor
            _cache = Settings.selectedHaloMap;

            // Load Meta
            RefreshMeta();
        }

        public void RefreshMeta()
        {
            if (File.Exists(_examplePath))
            {
                // Load Example Plugin File
                XmlReader xml = XmlReader.Create(_examplePath);

                // Load Meta from Example Plugin
                //try
                {
                    _pluginVisitor = new ThirdGenPluginVisitor(_cache.TagHierarch, Settings.pluginsShowInvisibles);
                    AssemblyPluginLoader.LoadPlugin(xml, _pluginVisitor);

                    ReflexiveFlattener flattener = new ReflexiveFlattener();
                    flattener.Flatten(_pluginVisitor.Values);

                    MetaReader metaReader = new MetaReader(_cache.Stream, _cacheOffset, _cache.Cache);
                    metaReader.ReadFields(_pluginVisitor.Values);

                    panelMetaComponents.ItemsSource = _pluginVisitor.Values;
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
            if (_cache != null)
            {
                int offset = -1;

                try
                {
                    // Validate Textbox
                    if (txtOffset.Text.ToLower().StartsWith("0x"))
                    {
                        // Is Hex
                        int.TryParse(txtOffset.Text.ToLower().Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
                    }
                    else
                    {
                        // Not Hex
                        int.TryParse(txtOffset.Text, out offset);
                    }

                    if (offset < 0 || offset > (_cache.Stream.Length - 0x200))
                        throw new InvalidOperationException();

                    _cacheOffset = (uint)offset;
                    RefreshMeta();
                }
                catch
                {
                    MetroMessageBox.Show(
                        "Invalid offset.",
                        "The meta offset you set is not valid. It might be out of the index of the cache, or contain invalid characters. Remember, if it's a hex number, it must start with a '0x'"
                        );
                }
            }
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
