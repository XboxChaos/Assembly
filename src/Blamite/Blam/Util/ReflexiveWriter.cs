using System.Collections.Generic;
using System.Linq;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.Util
{
	/// <summary>
	///     Utility class for reallocating reflexives and writing them to meta.
	/// </summary>
	public static class ReflexiveWriter
	{
		/// <summary>
		///     Writes data to a reflexive at a particular address.
		/// </summary>
		/// <param name="entries">The entries to write.</param>
		/// <param name="address">The address to write to.</param>
		/// <param name="layout">The layout of the data to write.</param>
		/// <param name="metaArea">The meta area of the cache file.</param>
		/// <param name="writer">The stream to write to.</param>
		public static void WriteReflexive(IEnumerable<StructureValueCollection> entries, uint address, StructureLayout layout,
			FileSegmentGroup metaArea, IWriter writer)
		{
			int offset = metaArea.PointerToOffset(address);
			int index = 0;
			foreach (StructureValueCollection entry in entries)
			{
				writer.SeekTo(offset + index*layout.Size);
				StructureWriter.WriteStructure(entry, layout, writer);
				index++;
			}
		}

		/// <summary>
		///     Allocates a new reflexive and writes data to it.
		/// </summary>
		/// <param name="entries">The entries to write.</param>
		/// <param name="layout">The layout of the data to write.</param>
		/// <param name="metaArea">The meta area of the cache file.</param>
		/// <param name="allocator">The cache file's meta allocator.</param>
		/// <param name="stream">The stream to manipulate.</param>
		/// <returns>The address of the new reflexive, or 0 if the entry list is empty.</returns>
		public static uint WriteReflexive(ICollection<StructureValueCollection> entries, StructureLayout layout,
			FileSegmentGroup metaArea, MetaAllocator allocator, IStream stream)
		{
			return WriteReflexive(entries, 0, 0, layout, metaArea, allocator, stream);
		}

		/// <summary>
		///     Writes data to a reflexive, reallocating the original.
		/// </summary>
		/// <param name="entries">The entries to write.</param>
		/// <param name="oldCount">The old count.</param>
		/// <param name="oldAddress">The old address.</param>
		/// <param name="layout">The layout of the data to write.</param>
		/// <param name="metaArea">The meta area of the cache file.</param>
		/// <param name="allocator">The cache file's meta allocator.</param>
		/// <param name="stream">The stream to manipulate.</param>
		/// <returns>The address of the new reflexive, or 0 if the entry list is empty and the reflexive was freed.</returns>
		public static uint WriteReflexive(ICollection<StructureValueCollection> entries, int oldCount, uint oldAddress,
			StructureLayout layout, FileSegmentGroup metaArea, MetaAllocator allocator, IStream stream)
		{
			return WriteReflexive(entries, oldCount, oldAddress, entries.Count, layout, metaArea, allocator, stream);
		}

		/// <summary>
		///     Writes data to a reflexive, reallocating the original.
		/// </summary>
		/// <param name="entries">The entries to write.</param>
		/// <param name="oldCount">The old count.</param>
		/// <param name="oldAddress">The old address.</param>
		/// <param name="newCount">The number of entries to write.</param>
		/// <param name="layout">The layout of the data to write.</param>
		/// <param name="metaArea">The meta area of the cache file.</param>
		/// <param name="allocator">The cache file's meta allocator.</param>
		/// <param name="stream">The stream to manipulate.</param>
		/// <returns>The address of the new reflexive, or 0 if the entry list is empty and the reflexive was freed.</returns>
		public static uint WriteReflexive(IEnumerable<StructureValueCollection> entries, int oldCount, uint oldAddress,
			int newCount, StructureLayout layout, FileSegmentGroup metaArea, MetaAllocator allocator, IStream stream)
		{
			if (newCount == 0)
			{
				// Free the old reflexive and return
				if (oldCount > 0 && oldAddress != 0)
					allocator.Free(oldAddress, oldCount*layout.Size);
				return 0;
			}

			uint newAddress = oldAddress;
			if (newCount != oldCount)
			{
				// Reallocate the reflexive
				int oldSize = oldCount*layout.Size;
				int newSize = newCount*layout.Size;
				if (oldCount > 0 && oldAddress != 0)
					newAddress = allocator.Reallocate(oldAddress, oldSize, newSize, stream);
				else
					newAddress = allocator.Allocate(newSize, stream);
			}

			// Write the new values
			WriteReflexive(entries.Take(newCount), newAddress, layout, metaArea, stream);
			return newAddress;
		}
	}
}