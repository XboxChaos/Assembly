using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam
{
    /// <summary>
    /// Represents a stringID table in a map file.
    /// </summary>
    public interface IStringIDSource
    {
        /// <summary>
        /// All of the strings in the file.
        /// </summary>
        IList<string> RawStrings { get; }

        /// <summary>
        /// Translates a stringID read from the file into an index in the RawStrings list.
        /// </summary>
        /// <param name="id">The StringID to translate.</param>
        /// <returns>The index of the string in the RawStrings list.</returns>
        int StringIDToIndex(StringID id);

        /// <summary>
        /// Translates a string index into a stringID which can be written to the file.
        /// </summary>
        /// <param name="index">The index of the string in the RawStrings list.</param>
        /// <returns>The stringID associated with the index.</returns>
        StringID IndexToStringID(int index);

        /// <summary>
        /// Returns the string that corresponds with the specified StringID.
        /// </summary>
        /// <param name="id">The StringID of the string to retrieve.</param>
        /// <returns>The string if it exists, or null otherwise.</returns>
        string GetString(StringID id);
    }
}
