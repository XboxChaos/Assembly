using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen.Structures;

namespace ExtryzeDLL.Blam
{
    public interface IFileNameSource : IEnumerable<string>
    {
        /// <summary>
        /// Given a datum index, retrieves the tag's corresponding filename if it exists.
        /// </summary>
        /// <param name="tagIndex">The datum index of the tag to get the filename of.</param>
        /// <returns>The tag's name if available or null otherwise.</returns>
        string FindTagName(DatumIndex tagIndex);

        string FindTagName(ITag tag);
    }
}
