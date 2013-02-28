using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam.Resources.Models
{
    /// <summary>
    /// A model which can be drawn on the screen.
    /// </summary>
    public interface IRenderModel
    {
        /// <summary>
        /// Gets the permutation regions available for the model.
        /// </summary>
        IModelRegion[] Regions { get; }

        /// <summary>
        /// Gets the sections of the model.
        /// </summary>
        IModelSection[] Sections { get; }

        /// <summary>
        /// Gets the model's bounding box. Can be null.
        /// </summary>
        IModelBoundingBox BoundingBox { get; }

        /// <summary>
        /// Gets the datum index of the model's resource.
        /// </summary>
        /// <seealso cref="IResourceTable"/>
        DatumIndex ResourceIndex { get; }
    }
}
