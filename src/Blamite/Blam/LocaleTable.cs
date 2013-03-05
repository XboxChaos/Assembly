using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam
{
    /// <summary>
    /// A table of locale strings loaded from a language.
    /// </summary>
    /// <seealso cref="ILanguage"/>
    public class LocaleTable
    {
        /// <summary>
        /// Constructs a new LocaleTable.
        /// </summary>
        /// <param name="language">The ILanguage that the table belongs to.</param>
        public LocaleTable(ILanguage language)
        {
            Language = language;
            Strings = new List<Locale>();
        }

        /// <summary>
        /// The language the locale table was created from.
        /// </summary>
        public ILanguage Language { get; private set; }

        /// <summary>
        /// The strings in the language.
        /// </summary>
        public IList<Locale> Strings { get; private set; }
    }
}
