using System.Collections.Generic;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.SecondGen.Structures
{
	public class SecondGenTag : ITag
	{
		public SecondGenTag(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagGroup> groupsById)
		{
			Load(values, metaArea, groupsById);
		}

		public int DataSize { get; set; }

		public ITagGroup Group { get; set; }
		public SegmentPointer MetaLocation { get; set; }
		public DatumIndex Index { get; private set; }

		private void Load(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagGroup> groupsById)
		{
			uint offset = (uint)values.GetInteger("offset");
			if (offset > 0)
				MetaLocation = SegmentPointer.FromPointer(offset, metaArea);

			// Load the tag group by looking up the magic value that's stored
			var groupMagic = (int) values.GetInteger("tag group magic");
			if (groupMagic != -1)
				Group = groupsById[groupMagic];

			Index = new DatumIndex(values.GetInteger("datum index"));
			DataSize = (int) values.GetInteger("data size");
		}
	}
}