using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    /// <summary>
    /// Implementation of ILocaleGroup that uses data read from a unic (multilingual_unicode_string_list) tag.
    /// </summary>
    public class ThirdGenLocaleGroup : ILocaleGroup
    {
        public ThirdGenLocaleGroup(StructureValueCollection values, DatumIndex tagIndex)
        {
            Load(values);
        }

        /// <summary>
        /// The identifier of the tag that the locale list was loaded from.
        /// </summary>
        public DatumIndex TagIndex { get; private set; }

        /// <summary>
        /// The range of strings that this group occupies in each table, in order by language.
        /// </summary>
        public LocaleRange[] Ranges { get; private set; }

        /// <summary>
        /// Loads data from a StructureValueCollection containing data read from a unic tag.
        /// </summary>
        /// <param name="values">The StructureValueCollection to retrieve values from.</param>
        private void Load(StructureValueCollection values)
        {
            // Load ranges
            StructureValueCollection[] rangeValues = values.GetArray("language ranges");
            Ranges = new LocaleRange[rangeValues.Length];
            for (int i = 0; i < rangeValues.Length; i++)
            {
                int startIndex = (int)rangeValues[i].GetNumber("range start index");
                int size = (int)rangeValues[i].GetNumber("range size");
                Ranges[i] = new LocaleRange(startIndex, size);
            }
        }
    }
}
