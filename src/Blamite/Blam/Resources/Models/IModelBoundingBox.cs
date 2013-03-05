using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Resources.Models
{
    /// <summary>
    /// A bounding box for a model.
    /// </summary>
    public interface IModelBoundingBox
    {
        /// <summary>
        /// Gets the minimum X coordinate for vertex positions in the model.
        /// </summary>
        float MinX { get; }

        /// <summary>
        /// Gets the maximum X coordinate for vertex positions in the model.
        /// </summary>
        float MaxX { get; }

        /// <summary>
        /// Gets the minimum Y coordinate for vertex positions in the model.
        /// </summary>
        float MinY { get; }

        /// <summary>
        /// Gets the maximum Y coordinate for vertex positions in the model.
        /// </summary>
        float MaxY { get; }

        /// <summary>
        /// Gets the minimum Z coordinate for vertex positions in the model.
        /// </summary>
        float MinZ { get; }

        /// <summary>
        /// Gets the maximum Z coordinate for vertex positions in the model.
        /// </summary>
        float MaxZ { get; }

        /// <summary>
        /// Gets the minimum U coordinate for texture coordinates in the model.
        /// </summary>
        float MinU { get; }

        /// <summary>
        /// Gets the maximum U coordinate for texture coordinates in the model.
        /// </summary>
        float MaxU { get; }

        /// <summary>
        /// Gets the minimum V coordinate for texture coordinates in the model.
        /// </summary>
        float MinV { get; }

        /// <summary>
        /// Gets the maximum V coordinate for texture coordinates in the model.
        /// </summary>
        float MaxV { get; }
    }
}
