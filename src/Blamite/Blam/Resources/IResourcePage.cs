using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Resources
{
    /// <summary>
    /// Resource page compression methods.
    /// </summary>
    public enum ResourcePageCompression
    {
        None,
        Deflate,
        LZX
    }

    /// <summary>
    /// A page of resources in a cache file.
    /// </summary>
    public interface IResourcePage
    {
        /// <summary>
        /// Gets the path to the cache file that the resource is located in.
        /// Can be null if the resource is in the current file.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// Gets the offset of the resource page from the start of the cache file's resource pool.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Gets the uncompressed size of the resource page.
        /// </summary>
        int UncompressedSize { get; }

        /// <summary>
        /// Gets the method used to compress the resource page.
        /// </summary>
        ResourcePageCompression CompressionMethod { get; }

        /// <summary>
        /// Gets the compressed size of the resource page.
        /// Can be the same as <see cref="UncompressedSize"/> if the page is not compressed.
        /// </summary>
        int CompressedSize { get; }
    }
}
