using System;
using System.Collections;
using System.Collections.Generic;
using Blamite.Flexibility;

namespace Blamite.Blam
{
	/// <summary>
	///     Represents a stringID table in a cache file.
	/// </summary>
	public abstract class StringIDSource : IEnumerable<string>
	{
		/// <summary>
		///     Gets the total number of stringIDs in the cache file.
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		///     Gets the layout of each stringID.
		/// </summary>
		public abstract StringIDLayout IDLayout { get; }

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
		///     Translates a stringID read from the file into an index in the table.
		/// </summary>
		/// <param name="id">The StringID to translate.</param>
		/// <returns>The index of the string in the table, or -1 if not found.</returns>
		public abstract int StringIDToIndex(StringID id);

		/// <summary>
		///     Translates a string index into a stringID which can be written to the file.
		/// </summary>
		/// <param name="index">The index of the string in the stringID table.</param>
		/// <returns>The stringID associated with the index.</returns>
		public abstract StringID IndexToStringID(int index);

		/// <summary>
		///     Gets the string at the given index in the table.
		/// </summary>
		/// <param name="index">The zero-based index of the string to retrieve.</param>
		/// <returns>The string at the given index.</returns>
		public abstract string GetString(int index);

		/// <summary>
		///     Returns the string that corresponds with the given StringID.
		/// </summary>
		/// <param name="id">The StringID of the string to retrieve.</param>
		/// <returns>The string if it exists, or null otherwise.</returns>
		public string GetString(StringID id)
		{
			int index = StringIDToIndex(id);
			if (index != -1)
				return GetString(index);
			return null;
		}

		/// <summary>
		///     Returns the zero-based index of a string in the table.
		/// </summary>
		/// <param name="str">The string to search for. Case-sensitive.</param>
		/// <returns>The zero-based index of the string in the table, or -1 if not found.</returns>
		public abstract int FindStringIndex(string str);

		/// <summary>
		///     Returns the stringID corresponding to a string in the table.
		/// </summary>
		/// <param name="str">The string to search for. Case-sensitive.</param>
		/// <returns>The stringID corresponding to the string, or StringID.Null if not found.</returns>
		public StringID FindStringID(string str)
		{
			int index = FindStringIndex(str);
			if (index != -1)
				return IndexToStringID(index);
			return StringID.Null;
		}

		/// <summary>
		///     Adds a string and returns its corresponding stringID.
		/// </summary>
		/// <param name="str">The string to add.</param>
		/// <returns>The added string's stringID.</returns>
		public abstract StringID AddString(string str);

		/// <summary>
		///     Sets a stringID's corresponding string.
		/// </summary>
		/// <param name="index">The index of the string to set.</param>
		/// <param name="str">The new value of the string.</param>
		public abstract void SetString(int index, string str);

		/// <summary>
		///     Sets a stringID's corresponding string.
		/// </summary>
		/// <param name="id">The stringID of the string to set.</param>
		/// <param name="str">The new value of the string.</param>
		public void SetString(StringID id, string str)
		{
			int index = StringIDToIndex(id);
			if (index == -1)
				throw new ArgumentException("StringID does not exist");
			SetString(index, str);
		}
	}
}