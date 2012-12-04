using System;
using System.Collections.Generic;
using ExtryzeDLL.Blam.Util;

namespace ExtryzeDLL.Blam.ThirdGen
{
    public interface ICacheFile
    {
        /// <summary>
        /// Tag classes stored in the cache file.
        /// </summary>
        IList<ITagClass> TagClasses { get; }

        /// <summary>
        /// The tags in the cache file.
        /// </summary>
        IList<ITag> Tags { get; }

        /// <summary>
        /// Information about the cache file.
        /// </summary>
        ICacheFileInfo Info { get; }

        /// <summary>
        /// A PointerConverter that can be used to convert between meta addresses and file offsets.
        /// </summary>
        PointerConverter MetaPointerConverter { get; }

        /// <summary>
        /// A PointerConverter that can be used to convert between locale pointers and file offsets.
        /// </summary>
        PointerConverter LocalePointerConverter { get; }

        /// <summary>
        /// Languages in the cache file. These can be used to extract locale information.
        /// </summary>
        IList<ILanguage> Languages { get; }

        /// <summary>
        /// Information about the cache file's scenario.
        /// </summary>
        IScenario Scenario { get; }
    }
}
