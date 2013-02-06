using System;
using System.Collections.Generic;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam
{
    /// <summary>
    /// A .map file containing cached information about a map in the game.
    /// </summary>
    public interface ICacheFile
    {
        /// <summary>
        /// Saves any changes that were made to the file header.
        /// </summary>
        /// <param name="stream">The stream to write header changes to.</param>
        void SaveChanges(IStream stream);

        /// <summary>
        /// Information about the cache file (usually obtained from its header).
        /// </summary>
        ICacheFileInfo Info { get; }

        /// <summary>
        /// The PointerConverter that can be used to convert pointers to tag meta.
        /// </summary>
        PointerConverter MetaPointerConverter { get; }

        /// <summary>
        /// The PointerConverter that can be used to convert pointers to locale data.
        /// </summary>
        PointerConverter LocalePointerConverter { get; }

        /// <summary>
        /// The tag names in the file.
        /// </summary>
        IFileNameSource FileNames { get; }

        /// <summary>
        /// The stringIDs in the file.
        /// </summary>
        IStringIDSource StringIDs { get; }

        /// <summary>
        /// The languages stored in the file.
        /// </summary>
        IList<ILanguage> Languages { get; }

        /// <summary>
        /// The locale groups stored in the file.
        /// </summary>
        IList<ILocaleGroup> LocaleGroups { get; }

        /// <summary>
        /// The tag classes stored in the file.
        /// </summary>
        IList<ITagClass> TagClasses { get; }

        /// <summary>
        /// The tags stored in the file.
        /// </summary>
        IList<ITag> Tags { get; }

        /// <summary>
        /// The file's scenario data.
        /// </summary>
        IScenario Scenario { get; }
    }
}
