using System.Collections.Generic;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.SecondGen.Structures
{
	public class SecondGenTag : ITag
	{
		public SecondGenTag(DatumIndex index, ITagGroup tagGroup, SegmentPointer metaLocation, int size)
		{
			Index = index;
			Group = tagGroup;
			MetaLocation = metaLocation;
			Offset = (int)MetaLocation.AsPointer();
			DataSize = size;
		}

		public SecondGenTag(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagGroup> groupsById)
		{
			Load(values, metaArea, groupsById);
		}

		public int DataSize { get; set; }

		public ITagGroup Group { get; set; }
		public SegmentPointer MetaLocation { get; set; }
		public DatumIndex Index { get; private set; }
		public int Offset { get; private set; }

		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();

			uint addr = 0;
			if (MetaLocation != null)
				addr = (uint)MetaLocation.AsPointer();

			result.SetInteger("tag group magic", (Group != null) ? (uint)Group.Magic : 0xFFFFFFFF);
			result.SetInteger("datum index", Index.Value);
			result.SetInteger("offset", (uint)Offset);
			result.SetInteger("data size", (uint)DataSize);

			return result;
		}

		private void Load(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagGroup> groupsById)
		{
			Offset = (int)values.GetInteger("offset");
			if ((uint)Offset > 0)
				MetaLocation = SegmentPointer.FromPointer((uint)Offset, metaArea);

			// Load the tag group by looking up the magic value that's stored
			var groupMagic = (int) values.GetInteger("tag group magic");
			if (groupMagic != -1)
				Group = groupsById[groupMagic];

			Index = new DatumIndex(values.GetInteger("datum index"));
			DataSize = (int) values.GetInteger("data size");
		}
	}
}