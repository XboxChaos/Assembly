using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Util;

namespace Blamite.Blam
{
    public abstract class TagTable : IEnumerable<ITag>
    {
        /// <summary>
        /// Gets the tag at a given index.
        /// </summary>
        /// <param name="index">The index of the tag to retrieve.</param>
        /// <returns>The tag at the given index. Can be null.</returns>
        public abstract ITag this[int index] { get; }

        /// <summary>
        /// Gets the tag with a given datum index.
        /// </summary>
        /// <param name="index">The datum index of the tag to retrieve.</param>
        /// <returns>The tag with the corresponding datum index. Can be null.</returns>
        public ITag this[DatumIndex index]
        {
            get
            {
                if (!index.IsValid)
                    throw new ArgumentException("Invalid tag datum index");
                return this[index.Index];
            }
        }

        /// <summary>
        /// Finds the first tag which belongs to a given class.
        /// Tags which inherit from the class will be included as well.
        /// </summary>
        /// <param name="classMagic">The magic number (ID) of the class to search for.</param>
        /// <returns>The first tag which is a member of the class, or null if not found.</returns>
        public ITag FindTagByClass(int classMagic)
        {
            foreach (ITag tag in this)
            {
                if (tag != null && tag.Class != null && (tag.Class.Magic == classMagic || tag.Class.ParentMagic == classMagic || tag.Class.GrandparentMagic == classMagic))
                    return tag;
            }
            return null;
        }

        /// <summary>
        /// Finds the first tag which belongs to a given class.
        /// Tags which inherit from the class will be included as well.
        /// </summary>
        /// <param name="className">The case-sensitive four-letter string representation of the name of the class to search for (e.g. "bipd").</param>
        /// <returns>The first tag which is a member of the class, or null if not found.</returns>
        public ITag FindTagByClass(string className)
        {
            return FindTagByClass(CharConstant.FromString(className));
        }

        /// <summary>
        /// Finds the first tag which belongs to a given class.
        /// Tags which inherit from the class will be included as well.
        /// </summary>
        /// <param name="tagClass">The tag class to search for.</param>
        /// <returns>The first tag which is a member of the class, or null if not found.</returns>
        public ITag FindTagByClass(ITagClass tagClass)
        {
            return FindTagByClass(tagClass.Magic);
        }

        /// <summary>
        /// Retrieves a collection of tags which are members of a given class.
        /// Tags which inherit from the class will be included as well.
        /// </summary>
        /// <param name="classMagic">The magic number (ID) of the class to search for.</param>
        /// <returns>A collection of tags which are members of the class.</returns>
        public IEnumerable<ITag> FindTagsByClass(int classMagic)
        {
            foreach (ITag tag in this)
            {
                if (tag != null && tag.Class != null && (tag.Class.Magic == classMagic || tag.Class.ParentMagic == classMagic || tag.Class.GrandparentMagic == classMagic))
                    yield return tag;
            }
        }

        /// <summary>
        /// Retrieves a collection of tags which are members of a given class.
        /// Tags which inherit from the class will be included as well.
        /// </summary>
        /// <param name="className">The case-sensitive four-letter string representation of the name of the class to search for (e.g. "bipd").</param>
        /// <returns>A collection of tags which are members of the class.</returns>
        public IEnumerable<ITag> FindTagsByClass(string className)
        {
            return FindTagsByClass(CharConstant.FromString(className));
        }

        /// <summary>
        /// Retrieves a collection of tags which are members of a given class.
        /// Tags which inherit from the class will be included as well.
        /// </summary>
        /// <param name="tagClass">The tag class to search for.</param>
        /// <returns>A collection of tags which are members of the class.</returns>
        public IEnumerable<ITag> FindTagsByClass(ITagClass tagClass)
        {
            return FindTagsByClass(tagClass.Magic);
        }

        /// <summary>
        /// Finds the first tag which has a given name.
        /// </summary>
        /// <param name="name">The case-sensitive tag name to search for.</param>
        /// <param name="names">The FileNameSource containing tag names.</param>
        /// <returns>The first tag which has the given name, or null if nothing was found.</returns>
        public ITag FindTagByName(string name, FileNameSource names)
        {
            int index = names.FindName(name);
            if (index != -1)
                return this[index];
            return null;
        }

        public abstract IEnumerator<ITag> GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
