using System;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.Util
{
	/// <summary>
	///     Utility class for reading reflexives from meta.
	/// </summary>
	public static class ReflexiveReader
	{
		public static StructureValueCollection[] ReadReflexive(IReader reader, int count, uint address,
			StructureLayout entryLayout, FileSegmentGroup metaArea)
		{
			if (entryLayout.Size == 0)
				throw new ArgumentException("The entry layout must have a size associated with it.");

			// Handle null pointers
			if (count <= 0 || !metaArea.ContainsPointer(address))
				return new StructureValueCollection[0];

			// Convert the address to an offset and seek to it
			int offset = metaArea.PointerToOffset(address);
			reader.SeekTo(offset);

			// Read the entries
			var result = new StructureValueCollection[count];
			for (int i = 0; i < count; i++)
				result[i] = StructureReader.ReadStructure(reader, entryLayout);

			return result;
		}
	}
}