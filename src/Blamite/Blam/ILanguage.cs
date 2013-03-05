using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.IO;

namespace Blamite.Blam
{
    /// <summary>
    /// A set of localized strings in a cache file for a particular language.
    /// Strings can be loaded and saved on-demand.
    /// </summary>
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
        LocaleTable LoadStrings(IReader reader);

        /// <summary>
        /// Saves all of the language's strings back to the file.
        /// Note that ICacheFile.SaveChanges() may need to be called after this.
        /// </summary>
        /// <param name="stream">The IStream to write strings to.</param>
        /// <param name="locales">The strings to save back.</param>
        void SaveStrings(IStream stream, LocaleTable locales);
    }
}
