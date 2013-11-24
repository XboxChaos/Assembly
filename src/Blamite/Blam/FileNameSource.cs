using System;
using System.Collections;
using System.Collections.Generic;

namespace Blamite.Blam
{
	/// <summary>
	///     Provides a list of tag filenames which can be used to retrieve names for tags.
	/// </summary>
	public abstract class FileNameSource : IEnumerable<string>
	{
		/// <summary>
		///     Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		public abstract IEnumerator<string> GetEnumerator();

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
		///     Given an index of a tag in the tag table, retrieves a tag's corresponding filename if it exists.
		/// </summary>
		/// <param name="tagIndex">The index of the tag to get the filename of.</param>
		/// <returns>The tag's name if available, or null otherwise.</returns>
		public abstract string GetTagName(int tagIndex);

		/// <summary>
		///     Given a datum index of a tag, retrieves a tag's corresponding filename if it exists.
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
		///     Given a tag, retrieves its filename if it exists.
		/// </summary>
		/// <param name="tag">The tag to get the filename of.</param>
		/// <returns>The tag's name if available, or null otherwise.</returns>
		public string GetTagName(ITag tag)
		{
			return GetTagName(tag.Index);
		}

		/// <summary>
		///     Sets the name of a tag based upon its index in the tag table.
		/// </summary>
		/// <param name="tagIndex">The index of the tag to set the name of.</param>
		/// <param name="name">The new name.</param>
		public abstract void SetTagName(int tagIndex, string name);

		/// <summary>
		///     Sets the name of a tag based upon its datum index.
		/// </summary>
		/// <param name="tagIndex">The datum index of the tag to set the name of.</param>
		/// <param name="name">The new name.</param>
		public void SetTagName(DatumIndex tagIndex, string name)
		{
			if (!tagIndex.IsValid)
				throw new ArgumentNullException("Invalid tag datum index");
			SetTagName(tagIndex.Index, name);
		}

		/// <summary>
		///     Sets the name of a tag.
		/// </summary>
		/// <param name="tag">The tag to set the name of.</param>
		/// <param name="name">The new name.</param>
		public void SetTagName(ITag tag, string name)
		{
			SetTagName(tag.Index, name);
		}

		/// <summary>
		///     Finds the index of the first tag which has a given name.
		/// </summary>
		/// <param name="name">The tag name to search for.</param>
		/// <returns>The index in the tag table of the first tag with the given name, or -1 if not found.</returns>
		public abstract int FindName(string name);
	}
}