using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam
{
    /// <summary>
    /// Represents a range of strings in a locale table for a language.
    /// </summary>
    public class LocaleRange
    {
        /// <summary>
        /// Constructs a new LocaleRange.
        /// </summary>
        /// <param name="startIndex">The starting index of the range in the language's locale table.</param>
        /// <param name="size">The number of strings in the range.</param>
        public LocaleRange(int startIndex, int size)
        {
            StartIndex = startIndex;
            Size = size;
        }

        /// <summary>
        /// The starting index of the range in the language's locale table.
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// The number of strings in the range.
        /// </summary>
        public int Size { get; set; }
    }
}
