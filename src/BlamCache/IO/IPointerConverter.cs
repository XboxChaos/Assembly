using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.IO
{
    /// <summary>
    /// Interface for a class which converts pointers to and from file offsets.
    /// </summary>
    public interface IPointerConverter
    {
        /// <summary>
        /// Converts a pointer to a file offset.
        /// </summary>
        /// <param name="pointer">The pointer to convert.</param>
        /// <returns>The pointer's equivalent file offset.</returns>
        int PointerToOffset(uint pointer);

        /// <summary>
        /// Converts a file offset to a pointer.
        /// </summary>
        /// <param name="offset">The offset to convert.</param>
        /// <returns>The offset's equivalent pointer.</returns>
        uint OffsetToPointer(int offset);
    }
}
