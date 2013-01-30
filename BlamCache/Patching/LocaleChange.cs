using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Patching
{
    /// <summary>
    /// Represents a change that should be made to a locale string in a cache file.
    /// </summary>
    public class LocaleChange
    {
        public LocaleChange(int index, string newValue)
        {
            Index = index;
            NewValue = newValue;
        }

        /// <summary>
        /// The index of the locale to change.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// The new string that the locale should be set to.
        /// </summary>
        public string NewValue { get; private set; }
    }
}
