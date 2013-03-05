using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam
{
    /// <summary>
    /// Represents a group of localized strings.
    /// </summary>
    public interface ILocaleGroup
    {
        /// <summary>
        /// The identifier of the tag that the locale list was loaded from.
        /// </summary>
        DatumIndex TagIndex { get; }

        /// <summary>
        /// The range of strings that this group occupies in each table, in order by language.
        /// </summary>
        LocaleRange[] Ranges { get; }
    }
}
