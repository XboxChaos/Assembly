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

		public SegmentPointer FileNameOffset { get; set; }
		public int ResourceIndex { get; set; }

		public ITagGroup Group { get; set; }
		public SegmentPointer MetaLocation { get; set; }
		public DatumIndex Index { get; private set; }
		public TagSource Source { get; set; }

		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();

			result.SetInteger("tag group magic", (Group != null) ? (uint)Group.Magic : 0xFFFFFFFF);
			result.SetInteger("datum index", Index.Value);

			result.SetInteger("name address", (uint)FileNameOffset.AsPointer());

			uint addr = 0;
			if (Source == TagSource.MetaArea && MetaLocation != null)
				addr = (uint)MetaLocation.AsPointer();
			else if (Source == TagSource.Data)
				addr = (uint)ResourceIndex;

			result.SetInteger("memory address", addr);
			result.SetInteger("is external", (uint)(Source == TagSource.Data ? 1 : 0));

			return result;
		}

		private void Load(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagGroup> groupsById)
		{
			// Load the tag group by looking up the magic value that's stored
			var groupMagic = (int)values.GetInteger("tag group magic");
			if (groupMagic != -1)
				Group = groupsById[groupMagic];

			Index = new DatumIndex(values.GetInteger("datum index"));

			uint nameAddr = (uint)values.GetInteger("name address");
			if (nameAddr != 0)
				FileNameOffset = SegmentPointer.FromPointer(nameAddr, metaArea);

			uint addr = (uint)values.GetInteger("memory address");

			Source = TagSource.Null;

			if (values.GetInteger("is external") == 1)
			{
				Source = TagSource.Data;
				ResourceIndex = (int)addr;
			}
			else if (addr != 0 && addr != 0xFFFFFFFF)
			{
				Source = TagSource.MetaArea;
				MetaLocation = SegmentPointer.FromPointer(addr, metaArea);
			}

			/*
			 todo: load resource tags for custom edition based on TagSource.Data like bsps from FirstGenCacheFile
				bitm: bitmaps.map
				font: loc.map
				ustr: loc.map
				hmt : loc.map
			 */
		}
	}
}
