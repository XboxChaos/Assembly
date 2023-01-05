using System.Collections.Generic;
using System.Linq;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.Util
{
	/// <summary>
	///     Utility class for reallocating blocks and writing them to meta.
	/// </summary>
	public static class TagBlockWriter
	{
		public static void WriteContractedTagBlock(IEnumerable<StructureValueCollection> elements, uint address, StructureLayout layout,
			FileSegmentGroup metaArea, IWriter writer, IPointerExpander expander)
		{
			long cont = expander.Expand(address);
			uint offset = metaArea.PointerToOffset(address);
			int index = 0;
			foreach (StructureValueCollection entry in elements)
			{
				writer.SeekTo(offset + index * layout.Size);
				StructureWriter.WriteStructure(entry, layout, writer);
				index++;
			}
		}

		/// <summary>
		///     Writes data to a block at a particular address.
		/// </summary>
		/// <param name="elements">The entries to write.</param>
		/// <param name="address">The address to write to.</param>
		/// <param name="layout">The layout of the data to write.</param>
		/// <param name="metaArea">The meta area of the cache file.</param>
		/// <param name="writer">The stream to write to.</param>
		public static void WriteTagBlock(IEnumerable<StructureValueCollection> elements, long address, StructureLayout layout,
			FileSegmentGroup metaArea, IWriter writer)
		{
			uint offset = metaArea.PointerToOffset(address);
			int index = 0;
			foreach (StructureValueCollection element in elements)
			{
				writer.SeekTo(offset + index*layout.Size);
				StructureWriter.WriteStructure(element, layout, writer);
				index++;
			}
		}

		/// <summary>
		///     Allocates a new block and writes data to it.
		/// </summary>
		/// <param name="elements">The entries to write.</param>
		/// <param name="layout">The layout of the data to write.</param>
		/// <param name="metaArea">The meta area of the cache file.</param>
		/// <param name="allocator">The cache file's meta allocator.</param>
		/// <param name="stream">The stream to manipulate.</param>
		/// <returns>The address of the new tag block, or 0 if the entry list is empty.</returns>
		public static long WriteTagBlock(ICollection<StructureValueCollection> elements, StructureLayout layout,
			FileSegmentGroup metaArea, MetaAllocator allocator, IStream stream)
		{
			return WriteTagBlock(elements, 0, 0, layout, metaArea, allocator, stream);
		}

		/// <summary>
		///     Writes data to a block, reallocating the original.
		/// </summary>
		/// <param name="elements">The entries to write.</param>
		/// <param name="oldCount">The old count.</param>
		/// <param name="oldAddress">The old address.</param>
		/// <param name="layout">The layout of the data to write.</param>
		/// <param name="metaArea">The meta area of the cache file.</param>
		/// <param name="allocator">The cache file's meta allocator.</param>
		/// <param name="stream">The stream to manipulate.</param>
		/// <returns>The address of the new tag block, or 0 if the entry list is empty and the tag block was freed.</returns>
		public static long WriteTagBlock(ICollection<StructureValueCollection> elements, int oldCount, long oldAddress,
			StructureLayout layout, FileSegmentGroup metaArea, MetaAllocator allocator, IStream stream)
		{
			return WriteTagBlock(elements, oldCount, oldAddress, elements.Count, layout, metaArea, allocator, stream);
		}

		/// <summary>
		///     Writes data to a block, reallocating the original.
		/// </summary>
		/// <param name="elements">The entries to write.</param>
		/// <param name="oldCount">The old count.</param>
		/// <param name="oldAddress">The old address.</param>
		/// <param name="newCount">The number of entries to write.</param>
		/// <param name="layout">The layout of the data to write.</param>
		/// <param name="metaArea">The meta area of the cache file.</param>
		/// <param name="allocator">The cache file's meta allocator.</param>
		/// <param name="stream">The stream to manipulate.</param>
		/// <returns>The address of the new tag block, or 0 if the entry list is empty and the tag block was freed.</returns>
		public static long WriteTagBlock(IEnumerable<StructureValueCollection> elements, int oldCount, long oldAddress,
			int newCount, StructureLayout layout, FileSegmentGroup metaArea, MetaAllocator allocator, IStream stream)
		{
			if (newCount == 0)
			{
				// Free the old block and return
				if (oldCount > 0 && oldAddress != 0)
					allocator.Free(oldAddress, (uint)(oldCount*layout.Size));
				return 0;
			}

			long newAddress = oldAddress;
			if (newCount != oldCount)
			{
				// Reallocate the block
				uint oldSize = (uint)(oldCount*layout.Size);
				uint newSize = (uint)(newCount*layout.Size);
				if (oldCount > 0 && oldAddress != 0)
					newAddress = allocator.Reallocate(oldAddress, oldSize, newSize, stream);
				else
					newAddress = allocator.Allocate(newSize, stream);
			}

			// Write the new values
			WriteTagBlock(elements.Take(newCount), newAddress, layout, metaArea, stream);
			return newAddress;
		}
	}
}