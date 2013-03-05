using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.ThirdGen.Structures;

namespace Blamite.Blam
{
    /// <summary>
    /// Provides a list of tag filenames which can be used to retrieve names for tags.
    /// </summary>
    public abstract class FileNameSource : IEnumerable<string>
    {
        /// <summary>
        /// Given an index of a tag in the tag table, retrieves a tag's corresponding filename if it exists.
        /// </summary>
        /// <param name="tagIndex">The index of the tag to get the filename of.</param>
        /// <returns>The tag's name if available, or null otherwise.</returns>
        public abstract string GetTagName(int tagIndex);

        /// <summary>
        /// Given a datum index of a tag, retrieves a tag's corresponding filename if it exists.
        /// </summary>
        /// <param name="tagIndex">The datum index of the tag to get the filename of.</param>
        /// <returns>The tag's name if available, or null otherwise.</returns>
        public string GetTagName(DatumIndex tagIndex)
        {
            if (!tagIndex.IsValid)
                throw new ArgumentNullException("Invalid tag datum index");
            return GetTagName(tagIndex.Index);
        }

        /// <summary>
        /// Given a tag, retrieves its filename if it exists.
        /// </summary>
        /// <param name="tag">The tag to get the filename of.</param>
        /// <returns>The tag's name if available, or null otherwise.</returns>
        public string GetTagName(ITag tag)
        {
            return GetTagName(tag.Index);
        }

        /// <summary>
        /// Finds the index of the first tag which has a given name.
        /// </summary>
        /// <param name="name">The tag name to search for.</param>
        /// <returns>The index in the tag table of the first tag with the given name, or -1 if not found.</returns>
        public abstract int FindName(string name);

        public abstract IEnumerator<string> GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
