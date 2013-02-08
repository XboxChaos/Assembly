using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam
{
    /// <summary>
    /// A StringIDResolver which accepts and creates stringIDs with length information set.
    /// </summary>
    public class LengthBasedStringIDResolver : IStringIDResolver
    {
        private IndexedStringTable _strings;

        /// <summary>
        /// Constructs a new LengthBasedStringIDResolver.
        /// </summary>
        /// <param name="strings">The IndexedStringTable to reference to get string lengths.</param>
        public LengthBasedStringIDResolver(IndexedStringTable strings)
        {
            _strings = strings;
        }

        /// <summary>
        /// Translates a stringID into an index into the global debug strings array.
        /// </summary>
        /// <param name="id">The StringID to translate.</param>
        /// <returns>The index of the string in the global debug strings array.</returns>
        public int StringIDToIndex(StringID id)
        {
            return (int)id.Index;
        }

        /// <summary>
        /// Translates a string index into a stringID which can be written to the file.
        /// </summary>
        /// <param name="index">The index of the string in the global strings array.</param>
        /// <returns>The stringID associated with the index.</returns>
        public StringID IndexToStringID(int index)
        {
            if (index < 0 || index >= _strings.Strings.Count)
                return new StringID(0);

            string str = _strings.Strings[index];
            return new StringID((byte)str.Length, 0, (ushort)index);
        }
    }
}
