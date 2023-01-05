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
		private readonly SortedList<long, FreeArea> _freeAreasByAddr = new SortedList<long, FreeArea>();
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
		public long Allocate(uint size, IStream stream)
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
		public long Allocate(uint size, uint align, IStream stream)
		{
			// Find the smallest block that fits, or if nothing is found, expand the meta area
			FreeArea block = FindSmallestBlock(size, align);

			// if data is aligned, we will assume this data should not be in a readonly partition
			if (block != null && align > 4)
			{
				// readonly partitions are always 1 and 5 starting with halo 3. for halo 3 beta, only partition 0.
				if (_cacheFile.HeaderSize != 0x800 && _cacheFile.Partitions.Length == 6)
				{
					//throw out the found area if readonly
					if ((_cacheFile.Partitions[1] != null && _cacheFile.Partitions[1].Contains(block.Address))
					|| (_cacheFile.Partitions[5] != null && _cacheFile.Partitions[5].Contains(block.Address)))
						block = null;
				}
				// todo: h3beta support maybe
			}

			if (block == null)
				block = Expand(size, stream);

			if (block.Size == size)
			{
				// Perfect fit - just remove the block and we're done
				RemoveArea(block);
				return block.Address;
			}

			// Align the address
			long oldAddress = block.Address;
			long alignedAddress = (oldAddress + align - 1) & ~((long)align - 1);

			// Adjust the block's start address to free the data we're using
			ChangeStartAddress(block, (alignedAddress + size));

			// Add a block at the beginning if we had to align
			if (alignedAddress > oldAddress)
				Free(oldAddress, (uint) (alignedAddress - oldAddress));

			return alignedAddress;
		}

		/// <summary>
		/// Reallocates a block of memory in the cache file's meta area.
		/// The contents of the old block will be copied to the new block and then the old block will be zeroed.
		/// </summary>
		/// <param name="address">The starting address of the data to reallocate. If this is 0, a new block will be allocated.</param>
		/// <param name="oldSize">The old size of the data to reallocate. If this is 0, a new block will be allocated.</param>
		/// <param name="newSize">The requested size of the newly-allocated data block. If this is 0, the block will be freed and 0 will be returned.</param>
		/// <param name="stream">The stream to write cache file changes to.</param>
		/// <returns>The memory address of the new block, or 0 if the block was freed.</returns>
		public long Reallocate(long address, uint oldSize, uint newSize, IStream stream)
		{
			return Reallocate(address, oldSize, newSize, 4, stream);
		}

		/// <summary>
		/// Reallocates a block of memory in the cache file's meta area.
		/// The contents of the old block will be copied to the new block and then the old block will be zeroed.
		/// </summary>
		/// <param name="address">The starting address of the data to reallocate. If this is 0, a new block will be allocated.</param>
		/// <param name="oldSize">The old size of the data to reallocate. If this is 0, a new block will be allocated.</param>
		/// <param name="newSize">The requested size of the newly-allocated data block. If this is 0, the block will be freed and 0 will be returned.</param>
		/// <param name="align">The power of two to align the block to.</param>
		/// <param name="stream">The stream to write cache file changes to.</param>
		/// <returns>The memory address of the new block, or 0 if the block was freed.</returns>
		public long Reallocate(long address, uint oldSize, uint newSize, uint align, IStream stream)
		{
			if (newSize == oldSize)
				return address;

			// If the new size is 0, free the block
			if (newSize == 0)
			{
				Free(address, oldSize);
				return 0;
			}

			// If the old size or address is 0, allocate a new block
			if (address == 0 || oldSize == 0)
				return Allocate(newSize, align, stream);

			// If the block is being made smaller, just free and zero the data at the end
			if (newSize < oldSize)
			{
				Free(address + (uint)newSize, oldSize - newSize);
				long offset = _cacheFile.MetaArea.PointerToOffset(address);
				stream.SeekTo(offset + newSize);
				StreamUtil.Fill(stream, 0, oldSize - newSize);
				return address;
			}

			// If the block is being made larger, check if there's free space immediately after the block that can be used to avoid a copy
			FreeArea area;
			if (newSize > oldSize && _freeAreasByAddr.TryGetValue(address + (uint)oldSize, out area) &&
			    area.Size >= newSize - oldSize)
			{
				ChangeStartAddress(area, area.Address + (uint)(newSize - oldSize));
				return address;
			}

			// Free the block and allocate a new one
			Free(address, oldSize);
			long newAddress = Allocate(newSize, align, stream);

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
		public void Free(long address, uint size)
		{
			FreeBlock(address, size);
		}

		private FreeArea FreeBlock(long address, uint size)
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

		private FreeArea FindSmallestBlock(uint minSize, uint align)
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
				long alignedAddress = (area.Address + align - 1) & ~((long)align - 1);
				if (alignedAddress + minSize <= area.Address + area.Size)
					return area;

				index++;
			}
			return null;
		}

		private FreeArea Expand(uint minSize, IStream stream)
		{
			if (minSize <= 0)
				throw new ArgumentException("Invalid expansion amount");

			// Round the size up to the next multiple of the page size and expand the meta area
			uint roundedSize = (uint)((minSize + _pageSize - 1) & ~(_pageSize - 1));
			uint oldSize = _cacheFile.MetaArea.Size;
			_cacheFile.MetaArea.Resize(_cacheFile.MetaArea.Size + roundedSize, stream);

			// Free the newly-allocated area
			if (_cacheFile.MetaArea.Segments[_cacheFile.MetaArea.Segments.Count - 1].ResizeOrigin == SegmentResizeOrigin.End)
				return FreeBlock(_cacheFile.MetaArea.BasePointer + oldSize, roundedSize);
			else
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

		private void ResizeArea(FreeArea area, uint newSize)
		{
			if (newSize <= 0)
				throw new ArgumentException("Invalid area size");

			UnregisterAreaSize(area);
			area.Size = newSize;
			RegisterAreaSize(area);
		}

		private void ChangeStartAddress(FreeArea area, long newAddress)
		{
			var sizeDelta = (uint) (area.Address - newAddress);
			if (area.Size + sizeDelta < 0)
				throw new ArgumentException("Invalid start address");

			RemoveArea(area);
			area.Size += sizeDelta;
			if (area.Size == 0)
				return;
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
			public long Address { get; set; }

			/// <summary>
			///     The size of the free area.
			/// </summary>
			public uint Size { get; set; }
		}
	}
}