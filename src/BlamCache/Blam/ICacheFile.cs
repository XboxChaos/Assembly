using System;
using System.Collections.Generic;
using ExtryzeDLL.Blam.Resources;
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
        /// Saves any changes that were made to the file.
        /// </summary>
        /// <param name="stream">The stream to write changes to.</param>
        void SaveChanges(IStream stream);

        /// <summary>
        /// The size of the file header.
        /// </summary>
        int HeaderSize { get; }

        /// <summary>
        /// The size of the cache file.
        /// </summary>
        uint FileSize { get; }

        /// <summary>
        /// The purpose of the cache file.
        /// </summary>
        CacheFileType Type { get; }

        /// <summary>
        /// The cache file's internal name.
        /// </summary>
        string InternalName { get; }

        /// <summary>
        /// The name of the cache file's scenario tag (can be null if none).
        /// </summary>
        string ScenarioName { get; }

        /// <summary>
        /// The engine build string that the cache file is intended for.
        /// </summary>
        string BuildString { get; }

        /// <summary>
        /// The XDK version that the cache file was developed with, or 0 if unknown.
        /// </summary>
        int XDKVersion { get; set; }

        /// <summary>
        /// The meta area of the cache file.
        /// Can be null.
        /// </summary>
        FileSegmentGroup MetaArea { get; }

        /// <summary>
        /// The location of the tag table header in the file.
        /// Can be null.
        /// </summary>
        SegmentPointer IndexHeaderLocation { get; set; }

        /// <summary>
        /// The meta partitions in the cache file.
        /// </summary>
        Partition[] Partitions { get; }

        /// <summary>
        /// The raw table in the cache file.
        /// Can be null.
        /// </summary>
        FileSegment RawTable { get; }

        /// <summary>
        /// The locale area of the cache file.
        /// Can be null.
        /// </summary>
        FileSegmentGroup LocaleArea { get; }

        /// <summary>
        /// The debug string area of the cache file.
        /// Can be null.
        /// </summary>
        FileSegmentGroup StringArea { get; }

        /// <summary>
        /// The tag names in the file.
        /// Can be null.
        /// </summary>
        IFileNameSource FileNames { get; }

        /// <summary>
        /// The stringIDs in the file.
        /// Can be null.
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
        /// Can be null.
        /// </summary>
        IScenario Scenario { get; }

        /// <summary>
        /// The resources stored in the cache file.
        /// Can be null.
        /// </summary>
        IResourceTable Resources { get; }

        /// <summary>
        /// The IResourceMetaLoader which can be used to load metadata for resources.
        /// </summary>
        IResourceMetaLoader ResourceMetaLoader { get; }

        /// <summary>
        /// A collection of segments that the file has been arbitrarily divided into.
        /// For a given cache file, these segments will always be in the same order.
        /// This could be used to compare two cache files and determine sizing differences between them.
        /// </summary>
        IEnumerable<FileSegment> Segments { get; }
    }
}
