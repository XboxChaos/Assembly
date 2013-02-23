using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam.Resources
{
    /// <summary>
    /// A table of resources in a cache file.
    /// </summary>
    public interface IResourceTable : IEnumerable<IResource>
    {
        /// <summary>
        /// Looks up a resource by its datum index and returns it.
        /// </summary>
        /// <param name="index">The datum index of the resource to look up.</param>
        /// <returns>The resource with the corresponding index.</returns>
        IResource this[DatumIndex index] { get; }
    }
}
