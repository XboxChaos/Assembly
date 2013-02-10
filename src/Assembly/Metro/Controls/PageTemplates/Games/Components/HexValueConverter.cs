using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
    /// <summary>
    /// Converts integers to and from hexadecimal strings starting with "0x".
    /// </summary>
    [ValueConversion(typeof(uint), typeof(string))]
    public class HexValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            uint v = (uint)value;
            return "0x" + v.ToString("X");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            if (str.StartsWith("0x"))
                return uint.Parse(str.Substring(2), NumberStyles.HexNumber);
            return uint.Parse(str);
        }
    }
}
