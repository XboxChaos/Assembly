using System;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.Util
{
	/// <summary>
	///     Utility class for reading blocks from meta.
	/// </summary>
	public static class TagBlockReader
	{
		public static StructureValueCollection[] ReadTagBlock(IReader reader, int count, long address,
			StructureLayout elementLayout, FileSegmentGroup metaArea)
		{
			if (elementLayout.Size == 0)
				throw new ArgumentException("The element layout must have a size associated with it.");

			// Handle null pointers
			if (count <= 0 || !metaArea.ContainsPointer(address))
				return new StructureValueCollection[0];

			// Convert the address to an offset and seek to it
			uint offset = metaArea.PointerToOffset(address);
			reader.SeekTo(offset);

			// Read the entries
			var result = new StructureValueCollection[count];
			for (int i = 0; i < count; i++)
				result[i] = StructureReader.ReadStructure(reader, elementLayout);

			return result;
		}
	}
}