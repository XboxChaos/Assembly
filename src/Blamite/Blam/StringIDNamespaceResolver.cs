using System.Collections.Generic;
using Blamite.Serialization;
using Blamite.Util;

namespace Blamite.Blam
{
	/// <summary>
	///     Implementation of IStringIDResolver that uses set definitions to translate stringIDs into array indices.
	/// </summary>
	/// <seealso cref="StringID" />
	public class StringIDNamespaceResolver : IStringIDResolver
	{
		private readonly SortedList<int, StringIDNamespace> _namespacesByGlobalIndex = new SortedList<int, StringIDNamespace>();
		private readonly SortedList<int, StringIDNamespace> _namespacesByID = new SortedList<int, StringIDNamespace>();

		public StringIDNamespaceResolver(StringIDLayout idLayout)
		{
			IDLayout = idLayout;
		}

		public StringIDNamespaceResolver(StringIDInformation info)
		{
			IDLayout = info.IDLayout;
			foreach (StringIDNamespace ns in info.Namespaces)
				RegisterNamespace(ns);
		}

		/// <summary>
		///     Translates a stringID into an index into the global debug strings array.
		/// </summary>
		/// <param name="id">The StringID to translate.</param>
		/// <returns>The index of the string in the global debug strings array.</returns>
		public int StringIDToIndex(StringID id)
		{
			// Find the index of the first set which is less than the set in the ID
			int closestSetIndex = ListSearching.BinarySearch(_namespacesByID.Keys, id.GetNamespace(IDLayout));
			if (closestSetIndex < 0)
			{
				// BinarySearch returns the bitwise complement of the index of the next highest value if not found
				// So, use the set that comes before it...
				closestSetIndex = ~closestSetIndex - 1;
				if (closestSetIndex < 0)
					return (int) id.Value; // No previous set defined - just return the handle
			}

			// If the index falls outside the set's min value, then put it into the previous set
			if (id.GetIndex(IDLayout) < _namespacesByID.Values[closestSetIndex].MinIndex)
			{
				closestSetIndex--;
				if (closestSetIndex < 0)
					return (int) id.Value;
			}

			// Eldorado hackiness, fake set 0xFF is used to store the max index for any set
			if (_namespacesByID.ContainsKey(0xFF) && id.GetIndex(IDLayout) >= _namespacesByID[0xFF].GlobalIndex)
			{
				return (int)id.Value;
			}

			// Calculate the array index by subtracting the value of the first ID in the set
			// and then adding the index in the global array of the set's first string
			StringIDNamespace set = _namespacesByID.Values[closestSetIndex];
			var firstId = new StringID(set.ID, set.MinIndex, IDLayout);
			return (int) (id.Value - firstId.Value + set.GlobalIndex);
		}

		/// <summary>
		///     Translates a string index into a stringID which can be written to the file.
		/// </summary>
		/// <param name="index">The index of the string in the global strings array.</param>
		/// <returns>The stringID associated with the index.</returns>
		public StringID IndexToStringID(int index)
		{
			// Determine which set the index belongs to by finding the set with the closest global index that comes before it
			int closestSetIndex = ListSearching.BinarySearch(_namespacesByGlobalIndex.Keys, index);
			if (closestSetIndex < 0)
			{
				// BinarySearch returns the bitwise complement of the index of the next highest value if not found
				// So negate it and subtract 1 to get the closest global index that comes before it
				closestSetIndex = ~closestSetIndex - 1;
				if (closestSetIndex < 0)
					return new StringID((uint) index); // No previous set defined - just return the index
			}

			// Eldorado hackiness, fake set 0xFF is used to store the max index for any set
			if (_namespacesByID.ContainsKey(0xFF) && index >= _namespacesByID[0xFF].GlobalIndex)
			{
				return new StringID((uint)index);
			}

			// Calculate the StringID by subtracting the set's global array index
			// and then adding the value of the first stringID in the set
			StringIDNamespace set = _namespacesByGlobalIndex.Values[closestSetIndex];
			var firstId = new StringID(set.ID, set.MinIndex, IDLayout);
			return new StringID((uint) (index - set.GlobalIndex + firstId.Value));
		}

		/// <summary>
		///     Gets the layout of stringIDs.
		/// </summary>
		public StringIDLayout IDLayout { get; private set; }

		/// <summary>
		///     Registers a stringID set to use to translate stringIDs.
		/// </summary>
		/// <param name="id">The set's ID number.</param>
		/// <param name="minIndex">The minimum index that a stringID must have in order to be counted as part of the set.</param>
		/// <param name="globalIndex">The index of the set's first string in the global stringID table.</param>
		public void RegisterNamespace(int id, int minIndex, int globalIndex)
		{
			var set = new StringIDNamespace(id, minIndex, globalIndex);
			RegisterNamespace(set);
		}

		/// <summary>
		///     Registers a stringID set to use to translate stringIDs.
		/// </summary>
		/// <param name="id">The set's ID number.</param>
		/// <param name="minIndex">The minimum index that a stringID must have in order to be counted as part of the set.</param>
		/// <param name="globalIndex">The index of the set's first string in the global stringID table.</param>
		public void RegisterNamespace(StringIDNamespace ns)
		{
			_namespacesByID[ns.ID] = ns;
			_namespacesByGlobalIndex[ns.GlobalIndex] = ns;
		}
	}
}