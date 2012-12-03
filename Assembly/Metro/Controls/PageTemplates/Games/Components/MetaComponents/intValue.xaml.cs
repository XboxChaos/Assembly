using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Assembly.Metro.Dialogs;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
    /// <summary>
    /// Interaction logic for intValue.xaml
    /// </summary>
    public partial class intValue : UserControl
    {
        public intValue()
        {
            InitializeComponent();
        }

        private void viewValueAs_Click(object sender, RoutedEventArgs e)
        {
            MetaData.ValueField value = this.DataContext as MetaData.ValueField;
            MetroViewValueAs.Show(value.MemoryAddress, value.CacheOffset);
        }

        // Alex: Fix your textbox validation XAML.
        // It's throwing exceptions for me.
        // We can't use TextChanged because it's too slow.
        /*private void txtValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_initialized)
                return;

            bool valid = true;
            switch (Type)
            {
                case IntType.Float32:
                    float tmpf = 0;
                    valid = (float.TryParse(txtValue.Text, out tmpf));
                    break;
                case IntType.Int16: 
                    Int16 tmp16 = 0;
                    valid = (Int16.TryParse(txtValue.Text, out tmp16));
                    break;
                case IntType.Int32: 
                    Int32 tmp32 = 0;
                    valid = (Int32.TryParse(txtValue.Text, out tmp32));
                    break;
                case IntType.Int8:
                    sbyte tmp8 = 0;
                    valid = (sbyte.TryParse(txtValue.Text, out tmp8));
                    break;
                case IntType.Uint16: 
                    UInt16 tmpu16 = 0;
                    valid = (UInt16.TryParse(txtValue.Text, out tmpu16));
                    break;
                case IntType.Uint32: 
                    UInt32 tmpu32 = 0;
                    valid = (UInt32.TryParse(txtValue.Text, out tmpu32));
                    break;
                case IntType.Uint8:
                    byte tmpu8 = 0;
                    valid = (byte.TryParse(txtValue.Text, out tmpu8));
                    break;
            }

            if (valid)
                txtValue.BorderBrush = (Brush)Brushes.Green;
            else
                txtValue.BorderBrush = (Brush)Brushes.Red;
        }*/
    }
}
