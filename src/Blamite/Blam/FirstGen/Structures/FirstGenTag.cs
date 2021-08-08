using System.Collections.Generic;
using Blamite.IO;
using Blamite.Serialization;

namespace Blamite.Blam.FirstGen.Structures
{
	public class FirstGenTag : ITag
	{
		public FirstGenTag(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagGroup> groupsById)
		{
			Load(values, metaArea, groupsById);
		}

		public ITagGroup Group { get; set; }

		public SegmentPointer MetaLocation { get; set; }

		public DatumIndex Index { get; private set; }

		public SegmentPointer FileNameOffset { get; set; }

		private void Load(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagGroup> groupsById)
		{
			// Load the tag group by looking up the magic value that's stored
			var groupMagic = (int)values.GetInteger("tag group magic");
			if (groupMagic != -1)
				Group = groupsById[groupMagic];

			Index = new DatumIndex(values.GetInteger("datum index"));

			// NOTE: cant really split filenames into a segment
			//       because offset is relative to the meta header
			uint nameOffset = (uint)values.GetInteger("name offset");
			if (nameOffset > 0)
				FileNameOffset = SegmentPointer.FromPointer(nameOffset, metaArea);
				//FileNameOffset = SegmentPointer.FromOffset((int)nameOffset, metaArea);

			uint offset = (uint)values.GetInteger("offset");

			// checking the meta area contains the offset
			// and that the tag element is not pointing to a data file
			if (offset > 0 && metaArea.ContainsPointer(offset) 
				&& (values.GetInteger("is in data file") != 1))
				MetaLocation = SegmentPointer.FromPointer(offset, metaArea);
			// TODO (Dragon): the offset can actually be 0 when used
			//                as a data file table index
			//else if (offset > 0 && !metaArea.ContainsPointer(offset))
			// TODO (Dragon): load the tag from a data file
			//                bitm: bitmaps.map
			//                font: loc.map
			//                ustr: loc.map
			//                hmt : loc.map

		}
	}
}
