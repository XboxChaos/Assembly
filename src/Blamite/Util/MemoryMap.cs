using System.Collections.Generic;

namespace Blamite.Util
{
	public class MemoryMap
	{
		// Sorted list containing the boundary addresses in the map
		private readonly List<uint> _addresses = new List<uint>();
		private readonly List<long> _sourceOffsets = new List<long>();

		/// <summary>
		///     Adds a "boundary" address to the map.
		///     This is an address which is known for sure.
		/// </summary>
		/// <param name="address">The address to add to the map.</param>
		/// <seealso cref="BlockCrossesBoundary" />
		public void AddBoundaryAddress(uint address)
		{
			AddAddress(address, 0);
		}

		/// <summary>
		///     Adds an address to the map.
		/// </summary>
		/// <param name="address">The address to add to the map.</param>
		/// <param name="sourceOffset">The address's file offset, for debugging purposes. Cannot be zero.</param>
		public void AddAddress(uint address, long sourceOffset)
		{
			// Binary-search the address list, and insert the address at the appropriate
			// location to maintain the list's sorting if it isn't already in there
			int index = _addresses.BinarySearch(address);
			if (index >= 0) return;
			// BinarySearch returns the complement of the next-highest value's index
			// if the value isn't found
			_addresses.Insert(~index, address);
			_sourceOffsets.Insert(~index, sourceOffset);
		}

		/// <summary>
		///     Removes an address from the map.
		/// </summary>
		/// <param name="address">The address to remove.</param>
		/// <returns>true if the address was found in the map and removed.</returns>
		public bool RemoveAddress(uint address)
		{
			// Binary-search it, and if it's found (result is >= 0), remove it
			int index = _addresses.BinarySearch(address);
			if (index >= 0)
			{
				_addresses.RemoveAt(index);
				_sourceOffsets.RemoveAt(index);
				return true;
			}
			return false;
		}

		/// <summary>
		///     Returns whether or not a block crosses any boundary addresses.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public bool BlockCrossesBoundary(uint start, int size)
		{
			int index = _addresses.BinarySearch(start);
			if (index >= 0)
				index++;
			else
				index = ~index;

			while (index < _addresses.Count && _addresses[index] < start + size)
			{
				if (_sourceOffsets[index] == 0)
					return true;
				index++;
			}

			return false;
		}

		public uint GetNextHighestAddress(uint address)
		{
			// Binary-search the address list
			int index = _addresses.BinarySearch(address);
			if (index >= 0)
			{
				// The address is in the list, so return the next-highest if there is one
				if (index < _addresses.Count - 1)
					return _addresses[index + 1];
			}
			else
			{
				// BinarySearch returns the complement of the next-highest value's index
				// if the value isn't found
				index = ~index;
				if (index < _addresses.Count)
					return _addresses[index];
			}
			return 0xFFFFFFFF; // Address is at or beyond the end of the map
		}

		public int EstimateBlockSize(uint startAddress)
		{
			// Just get the next-highest address and subtract
			return (int) (GetNextHighestAddress(startAddress) - startAddress);
		}
	}
}