using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam.Resources.Models
{
    /// <summary>
    /// A section of a model which can be optionally drawn based upon the currently active permutations.
    /// </summary>
    public interface IModelSection
    {
        /// <summary>
        /// Gets an array of submeshes for the section.
        /// </summary>
        IModelSubmesh[] Submeshes { get; }

        /// <summary>
        /// Gets an array of vertex groups in the section.
        /// </summary>
        IModelVertexGroup[] VertexGroups { get; }

        /// <summary>
        /// The vertex format index for the section's vertex buffer.
        /// </summary>
        /// <remarks>
        /// This is heavily dependent upon the cache file's target engine.
        /// </remarks>
        int VertexFormat { get; }
    }
}
