using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam
{
    /// <summary>
    /// Represents a stringID table in a cache file.
    /// </summary>
    public abstract class StringIDSource : IEnumerable<string>
    {
        /// <summary>
        /// Translates a stringID read from the file into an index in the table.
        /// </summary>
        /// <param name="id">The StringID to translate.</param>
        /// <returns>The index of the string in the table, or -1 if not found.</returns>
        public abstract int StringIDToIndex(StringID id);

        /// <summary>
        /// Translates a string index into a stringID which can be written to the file.
        /// </summary>
        /// <param name="index">The index of the string in the stringID table.</param>
        /// <returns>The stringID associated with the index.</returns>
        public abstract StringID IndexToStringID(int index);

        /// <summary>
        /// Gets the string at the given index in the table.
        /// </summary>
        /// <param name="index">The zero-based index of the string to retrieve.</param>
        /// <returns>The string at the given index.</returns>
        public abstract string GetString(int index);

        /// <summary>
        /// Returns the string that corresponds with the given StringID.
        /// </summary>
        /// <param name="id">The StringID of the string to retrieve.</param>
        /// <returns>The string if it exists, or null otherwise.</returns>
        public string GetString(StringID id)
        {
            int index = StringIDToIndex(id);
            if (index != -1)
                return GetString(index);
            return null;
        }

        /// <summary>
        /// Returns the zero-based index of a string in the table.
        /// </summary>
        /// <param name="str">The string to search for. Case-sensitive.</param>
        /// <returns>The zero-based index of the string in the table, or -1 if not found.</returns>
        public abstract int FindStringIndex(string str);

        /// <summary>
        /// Returns the stringID corresponding to a string in the table.
        /// </summary>
        /// <param name="str">The string to search for. Case-sensitive.</param>
        /// <returns>The stringID corresponding to the string, or StringID.Null if not found.</returns>
        public StringID FindStringID(string str)
        {
            int index = FindStringIndex(str);
            if (index != -1)
                return IndexToStringID(index);
            return StringID.Null;
        }

        public abstract IEnumerator<string> GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
