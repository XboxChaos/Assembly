using System;
using System.Collections.Generic;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.Util
{
	/// <summary>
	///     Provides methods for allocating and managing blocks of tag meta in a cache file.
	/// </summary>
	public class MetaAllocator
	{
		private readonly ICacheFile _cacheFile;

		// Using SortedLists for these may not be the most efficient, but allocations are done
		// so rarely that it *shouldn't* matter
		private readonly SortedList<uint, FreeArea> _freeAreasByAddr = new SortedList<uint, FreeArea>();
		private readonly List<FreeArea> _freeAreasBySize = new List<FreeArea>();
		private readonly int _pageSize;

		public MetaAllocator(ICacheFile cacheFile, int pageSize)
		{
			_cacheFile = cacheFile;
			_pageSize = pageSize;
		}

		/// <summary>
		///     Allocates a free block of memory in the cache file's meta area.
		/// </summary>
		/// <param name="size">The size of the memory block to allocate.</param>
		/// <param name="stream">The stream to write cache file changes to.</param>
		/// <returns>The memory address of the allocated area.</returns>
		public uint Allocate(int size, IStream stream)
		{
			return Allocate(size, 4, stream);
		}

		/// <summary>
		///     Allocates a free block of memory in the cache file's meta area.
		/// </summary>
		/// <param name="size">The size of the memory block to allocate.</param>
		/// <param name="align">The power of two to align the block to.</param>
		/// <param name="stream">The stream to write cache file changes to.</param>
		/// <returns></returns>
		public uint Allocate(int size, uint align, IStream stream)
		{
			// Find the smallest block that fits, or if nothing is found, expand the meta area
			FreeArea block = FindSmallestBlock(size, align);
			if (block == null)
				block = Expand(size, stream);

			if (block.Size == size)
			{
				// Perfect fit - just remove the block and we're done
				RemoveArea(block);
				return block.Address;
			}

			// Align the address
			uint oldAddress = block.Address;
			uint alignedAddress = (oldAddress + align - 1) & ~(align - 1);

			// Adjust the block's start address to free the data we're using
			ChangeStartAddress(block, (uint) (alignedAddress + size));

			// Add a block at the beginning if we had to align
			if (alignedAddress > oldAddress)
				Free(oldAddress, (int) (alignedAddress - oldAddress));

			return alignedAddress;
		}

		/// <summary>
		///     Reallocates a block of memory in the cache file's meta area.
		///     The contents of the old block will be copied to the new block and then zeroed.
		/// </summary>
		/// <param name="address">The starting address of the data to reallocate.</param>
		/// <param name="oldSize">The old size of the data to reallocate.</param>
		/// <param name="newSize">The requested size of the newly-allocated data block.</param>
		/// <param name="stream">The stream to write cache file changes to.</param>
		/// <returns>The memory address of the new block.</returns>
		public uint Reallocate(uint address, int oldSize, int newSize, IStream stream)
		{
			// Pretty basic for now
			// In the future, we could make an allocator that's biased toward the old address in order to prevent copying
			Free(address, oldSize);
			uint newAddress = Allocate(newSize, stream);

			// If the addresses differ, then copy the data across and zero the old data
			if (newAddress != address)
			{
				long oldOffset = _cacheFile.MetaArea.PointerToOffset(address);
				long newOffset = _cacheFile.MetaArea.PointerToOffset(newAddress);
				StreamUtil.Copy(stream, oldOffset, newOffset, oldSize);
				stream.SeekTo(oldOffset);
				StreamUtil.Fill(stream, 0, oldSize);
			}

			return newAddress;
		}

		/// <summary>
		///     Frees a block of memory, making it available for use in future allocations.
		/// </summary>
		/// <param name="address">The starting address of the block to free.</param>
		/// <param name="size">The size of the block to free.</param>
		public void Free(uint address, int size)
		{
			FreeBlock(address, size);
		}

		private FreeArea FreeBlock(uint address, int size)
		{
			if (size <= 0)
				throw new ArgumentException("Invalid block size");

			int index = ListSearching.BinarySearch(_freeAreasByAddr.Keys, address);
			if (index >= 0)
			{
				// Address is already free, but if the size doesn't overlap anything, then allow it
				FreeArea freedArea = _freeAreasByAddr.Values[index];
				if (freedArea.Address + freedArea.Size < address + size)
					throw new InvalidOperationException("Part of the block is already free");
				return freedArea;
			}

			// Get pointers to the previous and next free blocks
			index = ~index; // Get index of first largest area
			FreeArea next = null, previous = null;
			if (index > 0)
			{
				previous = _freeAreasByAddr.Values[index - 1];
				if (previous.Address + previous.Size >= address + size)
					return previous;
				if (previous.Address + previous.Size > address && previous.Address + previous.Size < address + size)
					throw new InvalidOperationException("Part of the block is already free");
			}
			if (index < _freeAreasByAddr.Count)
			{
				next = _freeAreasByAddr.Values[index];
				if (address + size > next.Address)
					throw new InvalidOperationException("Part of the block is already free");
			}

			// Four possible cases here:
			// 1. We're filling in a hole between two blocks, in which case we delete the second and expand the first
			// 2. The block being freed is immediately after another block, in which case we expand the previous block
			// 3. The block being freed is immediately before another block, in which case we change the next block's start address and size
			// 4. The block being freed has no neighbors, in which case it's just added to the list
			if (next != null && previous != null)
			{
				// Check if we're filling in a hole between two free areas
				// If we are, then merge the three areas together
				if (previous.Address + previous.Size == address && address + size == next.Address)
				{
					RemoveArea(next);
					ResizeArea(previous, previous.Size + size + next.Size);
					return previous;
				}
			}
			if (previous != null)
			{
				// Check if the previous block extends up to the address that's being freed
				// If it does, resize it to help prevent heap fragmentation
				if (previous.Address + previous.Size == address)
				{
					// Just resize the block before it
					ResizeArea(previous, previous.Size + size);
					return previous;
				}
			}
			if (next != null)
			{
				// Check if the next block starts at the end of the block that's being freed
				// If it does, resize it to help prevent heap fragmentation
				if (address + size == next.Address)
				{
					// Change the start address of the block after it
					ChangeStartAddress(next, address);
					return next;
				}
			}

			// Just create a new free area for the block, we're done
			var area = new FreeArea {Address = address, Size = size};
			AddArea(area);
			return area;
		}

		private FreeArea FindSmallestBlock(int minSize, uint align)
		{
			if (minSize <= 0)
				throw new ArgumentException("Invalid block size");

			int index = ListSearching.BinarySearch(_freeAreasBySize, minSize, a => a.Size);
			if (index < 0)
				index = ~index; // Get the index of the next largest block

			// Search until a block is found where the data can be aligned in
			while (index < _freeAreasBySize.Count)
			{
				FreeArea area = _freeAreasBySize[index];
				uint alignedAddress = (area.Address + align - 1) & ~(align - 1);
				if (alignedAddress + minSize <= area.Address + area.Size)
					return area;

				index++;
			}
			return null;
		}

		private FreeArea Expand(int minSize, IStream stream)
		{
			if (minSize <= 0)
				throw new ArgumentException("Invalid expansion amount");

			// Round the size up to the next multiple of the page size and expand the meta area
			int roundedSize = (minSize + _pageSize - 1) & ~(_pageSize - 1);
			_cacheFile.MetaArea.Resize(_cacheFile.MetaArea.Size + roundedSize, stream);

			// Free the newly-allocated area
			return FreeBlock(_cacheFile.MetaArea.BasePointer, roundedSize);
		}

		private void RegisterAreaAddress(FreeArea area)
		{
			_freeAreasByAddr[area.Address] = area;
		}

		private void UnregisterAreaAddress(FreeArea area)
		{
			_freeAreasByAddr.Remove(area.Address);
		}

		private void RegisterAreaSize(FreeArea area)
		{
			int index = ListSearching.BinarySearch(_freeAreasBySize, area.Size, a => a.Size);
			if (index < 0)
				index = ~index;
			_freeAreasBySize.Insert(index, area);
		}

		private void UnregisterAreaSize(FreeArea area)
		{
			_freeAreasBySize.Remove(area); // TODO: This needs to be optimized because it's O(n)
		}

		private void AddArea(FreeArea area)
		{
			RegisterAreaAddress(area);
			RegisterAreaSize(area);
		}

		private void RemoveArea(FreeArea area)
		{
			UnregisterAreaAddress(area);
			UnregisterAreaSize(area);
		}

		private void ResizeArea(FreeArea area, int newSize)
		{
			if (newSize <= 0)
				throw new ArgumentException("Invalid area size");

			UnregisterAreaSize(area);
			area.Size = newSize;
			RegisterAreaSize(area);
		}

		private void ChangeStartAddress(FreeArea area, uint newAddress)
		{
			var sizeDelta = (int) (area.Address - newAddress);
			if (area.Size + sizeDelta < 0)
				throw new ArgumentException("Invalid start address");

			RemoveArea(area);
			area.Size += sizeDelta;
			area.Address = newAddress;
			AddArea(area);
		}

		/// <summary>
		///     A free area of memory.
		/// </summary>
		private class FreeArea
		{
			/// <summary>
			///     The address of the free area.
			/// </summary>
			public uint Address { get; set; }

			/// <summary>
			///     The size of the free area.
			/// </summary>
			public int Size { get; set; }
		}
	}
}