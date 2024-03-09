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
			DataSize = size;
			Source = TagSource.MetaArea;
		}

		public SecondGenTag(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagGroup> groupsById)
		{
			Load(values, metaArea, groupsById);
		}

		public int DataSize { get; set; }

		public ITagGroup Group { get; set; }
		public SegmentPointer MetaLocation { get; set; }
		public DatumIndex Index { get; private set; }
		public TagSource Source { get; set; }

		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();

			result.SetInteger("tag group magic", (Group != null) ? (uint)Group.Magic : 0xFFFFFFFF);
			result.SetInteger("datum index", Index.Value);

			uint addr = 0;
			if (Source == TagSource.MetaArea && MetaLocation != null)
				addr = (uint)MetaLocation.AsPointer();

			result.SetInteger("memory address", addr);
			result.SetInteger("data size", Source != TagSource.BSP ? (uint)DataSize : 0);

			return result;
		}

		private void Load(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagGroup> groupsById)
		{
			uint address = (uint)values.GetInteger("memory address");
			Source = TagSource.Null;

			if (address != 0 && address != 0xFFFFFFFF)
			{
				Source = TagSource.MetaArea;
				MetaLocation = SegmentPointer.FromPointer(address, metaArea);
			}

			// Load the tag group by looking up the magic value that's stored
			var groupMagic = (int) values.GetInteger("tag group magic");
			if (groupMagic != -1)
				Group = groupsById[groupMagic];

			Index = new DatumIndex(values.GetInteger("datum index"));
			DataSize = (int) values.GetInteger("data size");
		}
	}
}