using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam
{
    public interface IStringIDSource
    {
        /// <summary>
        /// All of the strings in the file.
        /// </summary>
        IList<string> RawStrings { get; }

        /// <summary>
        /// Translates a stringID read from the file into an index in the RawStrings list.
        /// </summary>
        /// <param name="id">The ID to translate.</param>
        /// <returns>The index of the string in the RawStrings list.</returns>
        int StringIDToIndex(int id);

        /// <summary>
        /// Translates a string index into a stringID which can be written to the file.
        /// </summary>
        /// <param name="index">The index of the string in the RawStrings list.</param>
        /// <returns>The StringID associated with the index.</returns>
        int IndexToStringID(int index);

        /// <summary>
        /// Returns the string that corresponds with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the string to retrieve.</param>
        /// <returns>The string if it exists, or null otherwise.</returns>
        string GetString(int id);
    }
}
