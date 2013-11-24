using System;
using System.Collections;
using System.Collections.Generic;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam
{
	/// <summary>
	///     A tag table in a cache file.
	/// </summary>
	public abstract class TagTable : IEnumerable<ITag>
	{
		/// <summary>
		///     Gets the tag at a given index.
		/// </summary>
		/// <param name="index">The index of the tag to retrieve.</param>
		/// <returns>The tag at the given index.</returns>
		public abstract ITag this[int index] { get; }

		/// <summary>
		///     Gets the tag with a given datum index.
		/// </summary>
		/// <param name="index">The datum index of the tag to retrieve.</param>
		/// <returns>The tag with the corresponding datum index.</returns>
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
		///     Gets the number of tags in the table.
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		///     Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		public abstract IEnumerator<ITag> GetEnumerator();

		/// <summary>
		///     Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		///     Adds a tag to the table and allocates space for its base data.
		/// </summary>
		/// <param name="classMagic">The magic number (ID) of the tag's class.</param>
		/// <param name="baseSize">The size of the data to initially allocate for the tag.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <returns>The tag that was added.</returns>
		public abstract ITag AddTag(int classMagic, int baseSize, IStream stream);

		/// <summary>
		///     Adds a tag to the table and allocates space for its base data.
		/// </summary>
		/// <param name="tagClass">The tag's class.</param>
		/// <param name="baseSize">The size of the data to initially allocate for the tag.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <returns>The tag that was added.</returns>
		public ITag AddTag(ITagClass tagClass, int baseSize, IStream stream)
		{
			return AddTag(tagClass.Magic, baseSize, stream);
		}

		/// <summary>
		///     Adds a tag to the table and allocates space for its base data.
		/// </summary>
		/// <param name="className">The case-sensitive four-letter string representation of the name of the tag's class.</param>
		/// <param name="baseSize">The size of the data to initially allocate for the tag.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <returns>The tag that was added.</returns>
		public ITag AddTag(string className, int baseSize, IStream stream)
		{
			return AddTag(CharConstant.FromString(className), baseSize, stream);
		}

		/// <summary>
		///     Finds the first tag which belongs to a given class.
		///     Tags which inherit from the class will be included as well.
		/// </summary>
		/// <param name="classMagic">The magic number (ID) of the class to search for.</param>
		/// <returns>The first tag which is a member of the class, or null if not found.</returns>
		public ITag FindTagByClass(int classMagic)
		{
			foreach (ITag tag in this)
			{
				if (tag != null && tag.Class != null &&
				    (tag.Class.Magic == classMagic || tag.Class.ParentMagic == classMagic ||
				     tag.Class.GrandparentMagic == classMagic))
					return tag;
			}
			return null;
		}

		/// <summary>
		///     Finds the first tag which belongs to a given class.
		///     Tags which inherit from the class will be included as well.
		/// </summary>
		/// <param name="className">
		///     The case-sensitive four-letter string representation of the name of the class to search for
		///     (e.g. "bipd").
		/// </param>
		/// <returns>The first tag which is a member of the class, or null if not found.</returns>
		public ITag FindTagByClass(string className)
		{
			return FindTagByClass(CharConstant.FromString(className));
		}

		/// <summary>
		///     Finds the first tag which belongs to a given class.
		///     Tags which inherit from the class will be included as well.
		/// </summary>
		/// <param name="tagClass">The tag class to search for.</param>
		/// <returns>The first tag which is a member of the class, or null if not found.</returns>
		public ITag FindTagByClass(ITagClass tagClass)
		{
			return FindTagByClass(tagClass.Magic);
		}

		/// <summary>
		///     Retrieves a collection of tags which are members of a given class.
		///     Tags which inherit from the class will be included as well.
		/// </summary>
		/// <param name="classMagic">The magic number (ID) of the class to search for.</param>
		/// <returns>A collection of tags which are members of the class.</returns>
		public IEnumerable<ITag> FindTagsByClass(int classMagic)
		{
			foreach (ITag tag in this)
			{
				if (tag != null && tag.Class != null &&
				    (tag.Class.Magic == classMagic || tag.Class.ParentMagic == classMagic ||
				     tag.Class.GrandparentMagic == classMagic))
					yield return tag;
			}
		}

		/// <summary>
		///     Retrieves a collection of tags which are members of a given class.
		///     Tags which inherit from the class will be included as well.
		/// </summary>
		/// <param name="className">
		///     The case-sensitive four-letter string representation of the name of the class to search for
		///     (e.g. "bipd").
		/// </param>
		/// <returns>A collection of tags which are members of the class.</returns>
		public IEnumerable<ITag> FindTagsByClass(string className)
		{
			return FindTagsByClass(CharConstant.FromString(className));
		}

		/// <summary>
		///     Retrieves a collection of tags which are members of a given class.
		///     Tags which inherit from the class will be included as well.
		/// </summary>
		/// <param name="tagClass">The tag class to search for.</param>
		/// <returns>A collection of tags which are members of the class.</returns>
		public IEnumerable<ITag> FindTagsByClass(ITagClass tagClass)
		{
			return FindTagsByClass(tagClass.Magic);
		}

		/// <summary>
		///     Finds the first tag which has a given name.
		/// </summary>
		/// <param name="name">The case-sensitive tag name to search for.</param>
		/// <param name="names">The <see cref="FileNameSource" /> containing tag names.</param>
		/// <returns>The first tag which has the given name, or null if nothing was found.</returns>
		public ITag FindTagByName(string name, FileNameSource names)
		{
			int index = names.FindName(name);
			if (index != -1)
				return this[index];
			return null;
		}

		/// <summary>
		///     Finds the first tag in a class which has a given name.
		/// </summary>
		/// <param name="name">The case-sensitive tag name to search for.</param>
		/// <param name="classMagic">The magic number (ID) of the tag class to search in.</param>
		/// <param name="names">The <see cref="FileNameSource" /> containing tag names.</param>
		/// <returns>The first tag in the class which has the given name, or null if nothing was found.</returns>
		public ITag FindTagByName(string name, int classMagic, FileNameSource names)
		{
			foreach (ITag tag in FindTagsByClass(classMagic))
			{
				if (names.GetTagName(tag) == name)
					return tag;
			}
			return null;
		}

		/// <summary>
		///     Finds the first tag in a class which has a given name.
		/// </summary>
		/// <param name="name">The case-sensitive tag name to search for.</param>
		/// <param name="className">
		///     The case-sensitive four-letter string representation of the name of the class to search in
		///     (e.g. "bipd").
		/// </param>
		/// <param name="names">The <see cref="FileNameSource" /> containing tag names.</param>
		/// <returns>The first tag in the class which has the given name, or null if nothing was found.</returns>
		public ITag FindTagByName(string name, string className, FileNameSource names)
		{
			return FindTagByName(name, CharConstant.FromString(className), names);
		}

		/// <summary>
		///     Finds the first tag in a class which has a given name.
		/// </summary>
		/// <param name="name">The case-sensitive tag name to search for.</param>
		/// <param name="tagClass">The tag class to search in.</param>
		/// <param name="names">The <see cref="FileNameSource" /> containing tag names.</param>
		/// <returns>The first tag in the class which has the given name, or null if nothing was found.</returns>
		public ITag FindTagByName(string name, ITagClass tagClass, FileNameSource names)
		{
			return FindTagByName(name, tagClass.Magic, names);
		}

		/// <summary>
		///     Checks a datum index to see if it actually points to a tag.
		/// </summary>
		/// <param name="index">The datum index to check.</param>
		/// <returns>true if the datum index points to a valid tag.</returns>
		public bool IsValidIndex(DatumIndex index)
		{
			if (!index.IsValid || index.Index >= Count)
				return false;

			ITag tag = this[index];
			return (tag != null && tag.Index == index);
		}

		/// <summary>
		///     Checks a datum index and class ID to see if they actually point to a tag.
		/// </summary>
		/// <param name="index">The datum index to check.</param>
		/// <param name="classMagic">The magic number (ID) of the tag class which the tag should belong to.</param>
		/// <returns>true if the datum index points to a valid tag and its class ID matches.</returns>
		public bool IsValidIndex(DatumIndex index, int classMagic)
		{
			if (!IsValidIndex(index))
				return false;

			ITag tag = this[index];
			return (tag.Class != null && tag.Class.Magic == classMagic);
		}
	}
}