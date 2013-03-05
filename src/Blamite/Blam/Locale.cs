using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam
{
    /// <summary>
    /// A localized string in a cache file.
    /// </summary>
    public class Locale
    {
        /// <summary>
        /// Constructs a new Locale.
        /// </summary>
        /// <param name="id">The locale's stringID.</param>
        /// <param name="value">The localized string.</param>
        public Locale(StringID id, string value)
        {
            ID = id;
            Value = value;
        }

        /// <summary>
        /// The locale's stringID.
        /// </summary>
        public StringID ID { get; set; }

        /// <summary>
        /// The localized string.
        /// </summary>
        public string Value { get; set; }
    }
}
