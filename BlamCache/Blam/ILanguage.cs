using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam
{
    public interface ILanguage
    {
        /// <summary>
        /// The number of strings available in the language.
        /// </summary>
        int StringCount { get; }

        /// <summary>
        /// Loads all of the language's strings.
        /// </summary>
        /// <param name="reader">The IReader to read the strings from.</param>
        /// <returns>The strings that were read.</returns>
        List<string> LoadStrings(IReader reader);
    }
}
