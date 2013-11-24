using System.Collections.Generic;
using Blamite.Flexibility;
using Blamite.Util;

namespace Blamite.Blam
{
	/// <summary>
	///     Implementation of IStringIDResolver that uses set definitions to translate stringIDs into array indices.
	/// </summary>
	/// <seealso cref="StringID" />
	public class StringIDSetResolver : IStringIDResolver
	{
		private readonly SortedList<int, StringIDSet> _setsByGlobalIndex = new SortedList<int, StringIDSet>();
		private readonly SortedList<int, StringIDSet> _setsByID = new SortedList<int, StringIDSet>();

		public StringIDSetResolver(StringIDLayout idLayout)
		{
			IDLayout = idLayout;
		}

		/// <summary>
		///     Translates a stringID into an index into the global debug strings array.
		/// </summary>
		/// <param name="id">The StringID to translate.</param>
		/// <returns>The index of the string in the global debug strings array.</returns>
		public int StringIDToIndex(StringID id)
		{
			// Find the index of the first set which is less than the set in the ID
			int closestSetIndex = ListSearching.BinarySearch(_setsByID.Keys, id.GetSet(IDLayout));
			if (closestSetIndex < 0)
			{
				// BinarySearch returns the bitwise complement of the index of the next highest value if not found
				// So, use the set that comes before it...
				closestSetIndex = ~closestSetIndex - 1;
				if (closestSetIndex < 0)
					return (int) id.Value; // No previous set defined - just return the handle
			}

			// If the index falls outside the set's min value, then put it into the previous set
			if (id.GetIndex(IDLayout) < _setsByID.Values[closestSetIndex].MinIndex)
			{
				closestSetIndex--;
				if (closestSetIndex < 0)
					return (int) id.Value;
			}

			// Calculate the array index by subtracting the value of the first ID in the set
			// and then adding the index in the global array of the set's first string
			StringIDSet set = _setsByID.Values[closestSetIndex];
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
			int closestSetIndex = ListSearching.BinarySearch(_setsByGlobalIndex.Keys, index);
			if (closestSetIndex < 0)
			{
				// BinarySearch returns the bitwise complement of the index of the next highest value if not found
				// So negate it and subtract 1 to get the closest global index that comes before it
				closestSetIndex = ~closestSetIndex - 1;
				if (closestSetIndex < 0)
					return new StringID((uint) index); // No previous set defined - just return the index
			}

			// Calculate the StringID by subtracting the set's global array index
			// and then adding the value of the first stringID in the set
			StringIDSet set = _setsByGlobalIndex.Values[closestSetIndex];
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
		public void RegisterSet(int id, int minIndex, int globalIndex)
		{
			var set = new StringIDSet(id, minIndex, globalIndex);
			_setsByID[id] = set;
			_setsByGlobalIndex[globalIndex] = set;
		}

		/// <summary>
		///     A stringID set definition.
		/// </summary>
		private class StringIDSet
		{
			/// <summary>
			///     Constructs a new set definition.
			/// </summary>
			/// <param name="id">The set's ID number.</param>
			/// <param name="minIndex">The minimum index that a stringID must have in order to be counted as part of the set.</param>
			/// <param name="globalIndex">The index of the set's first string in the global stringID table.</param>
			public StringIDSet(int id, int minIndex, int globalIndex)
			{
				ID = id;
				MinIndex = minIndex;
				GlobalIndex = globalIndex;
			}

			/// <summary>
			///     The set's ID number.
			/// </summary>
			public int ID { get; private set; }

			/// <summary>
			///     The minimum index that a stringID must have in order to be counted as part of the set.
			/// </summary>
			public int MinIndex { get; private set; }

			/// <summary>
			///     The index of the set's first string in the global stringID table.
			/// </summary>
			public int GlobalIndex { get; private set; }
		}
	}
}