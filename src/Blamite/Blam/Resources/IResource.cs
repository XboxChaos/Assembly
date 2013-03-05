using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Resources
{
    /// <summary>
    /// A resource in a cache file.
    /// </summary>
    public interface IResource
    {
        /// <summary>
        /// Gets the datum index of the resource.
        /// </summary>
        DatumIndex Index { get; }

        /// <summary>
        /// Gets the tag associated with the resource.
        /// </summary>
        ITag ParentTag { get; }

        /// <summary>
        /// Gets the primary resource page that the resource belongs to. Can be null.
        /// </summary>
        IResourcePage PrimaryPage { get; }

        /// <summary>
        /// Gets the offset of the resource data within the primary page. Can be -1.
        /// </summary>
        int PrimaryOffset { get; }

        /// <summary>
        /// Gets the secondary resource page that the resource belongs to. Can be null.
        /// </summary>
        IResourcePage SecondaryPage { get; }

        /// <summary>
        /// Gets the offset of the resource data within the secondary page. Can be -1.
        /// </summary>
        int SecondaryOffset { get; }
    }
}
