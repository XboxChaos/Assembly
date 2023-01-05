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
		/// Tries to retrieve a global tag instance by its group id.
		/// </summary>
		/// <param name="magic">The magic number (ID) of the tag's group.</param>
		/// <returns></returns>
		public abstract ITag GetGlobalTag(int magic);

		/// <summary>
		///     Adds a tag to the table and allocates space for its base data.
		/// </summary>
		/// <param name="groupMagic">The magic number (ID) of the tag's group.</param>
		/// <param name="baseSize">The size of the data to initially allocate for the tag.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <returns>The tag that was added.</returns>
		public abstract ITag AddTag(int groupMagic, uint baseSize, IStream stream);

		/// <summary>
		///     Adds a tag to the table and allocates space for its base data.
		/// </summary>
		/// <param name="tagGroup">The tag's group.</param>
		/// <param name="baseSize">The size of the data to initially allocate for the tag.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <returns>The tag that was added.</returns>
		public ITag AddTag(ITagGroup tagGroup, uint baseSize, IStream stream)
		{
			return AddTag(tagGroup.Magic, baseSize, stream);
		}

		/// <summary>
		///     Adds a tag to the table and allocates space for its base data.
		/// </summary>
		/// <param name="groupName">The case-sensitive four-letter string representation of the name of the tag's group.</param>
		/// <param name="baseSize">The size of the data to initially allocate for the tag.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <returns>The tag that was added.</returns>
		public ITag AddTag(string groupName, uint baseSize, IStream stream)
		{
			return AddTag(CharConstant.FromString(groupName), baseSize, stream);
		}

		/// <summary>
		///     Finds the first tag which belongs to a given group.
		///     Tags which inherit from the group will be included as well.
		/// </summary>
		/// <param name="groupMagic">The magic number (ID) of the group to search for.</param>
		/// <returns>The first tag which is a member of the group, or null if not found.</returns>
		public ITag FindTagByGroup(int groupMagic)
		{
			foreach (ITag tag in this)
			{
				if (tag != null && tag.Group != null &&
				    (tag.Group.Magic == groupMagic || tag.Group.ParentMagic == groupMagic ||
				     tag.Group.GrandparentMagic == groupMagic))
					return tag;
			}
			return null;
		}

		/// <summary>
		///     Finds the first tag which belongs to a given group.
		///     Tags which inherit from the group will be included as well.
		/// </summary>
		/// <param name="groupName">
		///     The case-sensitive four-letter string representation of the name of the group to search for
		///     (e.g. "bipd").
		/// </param>
		/// <returns>The first tag which is a member of the group, or null if not found.</returns>
		public ITag FindTagByGroup(string groupName)
		{
			return FindTagByGroup(CharConstant.FromString(groupName));
		}

		/// <summary>
		///     Finds the first tag which belongs to a given group.
		///     Tags which inherit from the group will be included as well.
		/// </summary>
		/// <param name="tagGroup">The tag group to search for.</param>
		/// <returns>The first tag which is a member of the group, or null if not found.</returns>
		public ITag FindTagByGroup(ITagGroup tagGroup)
		{
			return FindTagByGroup(tagGroup.Magic);
		}

		/// <summary>
		///     Retrieves a collection of tags which are members of a given group.
		///     Tags which inherit from the group will be included as well.
		/// </summary>
		/// <param name="groupMagic">The magic number (ID) of the group to search for.</param>
		/// <returns>A collection of tags which are members of the group.</returns>
		public IEnumerable<ITag> FindTagsByGroup(int groupMagic)
		{
			foreach (ITag tag in this)
			{
				if (tag != null && tag.Group != null &&
				    (tag.Group.Magic == groupMagic || tag.Group.ParentMagic == groupMagic ||
				     tag.Group.GrandparentMagic == groupMagic))
					yield return tag;
			}
		}

		/// <summary>
		///     Retrieves a collection of tags which are members of a given group.
		///     Tags which inherit from the group will be included as well.
		/// </summary>
		/// <param name="groupName">
		///     The case-sensitive four-letter string representation of the name of the group to search for
		///     (e.g. "bipd").
		/// </param>
		/// <returns>A collection of tags which are members of the group.</returns>
		public IEnumerable<ITag> FindTagsByGroup(string groupName)
		{
			return FindTagsByGroup(CharConstant.FromString(groupName));
		}

		/// <summary>
		///     Retrieves a collection of tags which are members of a given group.
		///     Tags which inherit from the group will be included as well.
		/// </summary>
		/// <param name="tagGroup">The tag group to search for.</param>
		/// <returns>A collection of tags which are members of the group.</returns>
		public IEnumerable<ITag> FindTagsByGroup(ITagGroup tagGroup)
		{
			return FindTagsByGroup(tagGroup.Magic);
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
		///     Finds the first tag in a group which has a given name.
		/// </summary>
		/// <param name="name">The case-sensitive tag name to search for.</param>
		/// <param name="groupMagic">The magic number (ID) of the tag group to search in.</param>
		/// <param name="names">The <see cref="FileNameSource" /> containing tag names.</param>
		/// <returns>The first tag in the group which has the given name, or null if nothing was found.</returns>
		public ITag FindTagByName(string name, int groupMagic, FileNameSource names)
		{
			foreach (ITag tag in FindTagsByGroup(groupMagic))
			{
				if (names.GetTagName(tag) == name)
					return tag;
			}
			return null;
		}

		/// <summary>
		///     Finds the first tag in a group which has a given name.
		/// </summary>
		/// <param name="name">The case-sensitive tag name to search for.</param>
		/// <param name="groupName">
		///     The case-sensitive four-letter string representation of the name of the group to search in
		///     (e.g. "bipd").
		/// </param>
		/// <param name="names">The <see cref="FileNameSource" /> containing tag names.</param>
		/// <returns>The first tag in the group which has the given name, or null if nothing was found.</returns>
		public ITag FindTagByName(string name, string groupName, FileNameSource names)
		{
			return FindTagByName(name, CharConstant.FromString(groupName), names);
		}

		/// <summary>
		///     Finds the first tag in a group which has a given name.
		/// </summary>
		/// <param name="name">The case-sensitive tag name to search for.</param>
		/// <param name="tagGroup">The tag group to search in.</param>
		/// <param name="names">The <see cref="FileNameSource" /> containing tag names.</param>
		/// <returns>The first tag in the group which has the given name, or null if nothing was found.</returns>
		public ITag FindTagByName(string name, ITagGroup tagGroup, FileNameSource names)
		{
			return FindTagByName(name, tagGroup.Magic, names);
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
		///     Checks a datum index and group ID to see if they actually point to a tag.
		/// </summary>
		/// <param name="index">The datum index to check.</param>
		/// <param name="groupMagic">The magic number (ID) of the tag group which the tag should belong to.</param>
		/// <returns>true if the datum index points to a valid tag and its group ID matches.</returns>
		public bool IsValidIndex(DatumIndex index, int groupMagic)
		{
			if (!IsValidIndex(index))
				return false;

			ITag tag = this[index];
			return (tag.Group != null && tag.Group.Magic == groupMagic);
		}
	}
}