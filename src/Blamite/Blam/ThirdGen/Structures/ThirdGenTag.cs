using System.Collections.Generic;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Structures
{
	public class ThirdGenTag : ITag
	{
		public ThirdGenTag(DatumIndex index, ITagGroup tagGroup, SegmentPointer metaLocation)
		{
			Index = index;
			Group = tagGroup;
			MetaLocation = metaLocation;
		}

		public ThirdGenTag(StructureValueCollection values, ushort index, FileSegmentGroup metaArea,
			IList<ITagGroup> groupList, IPointerExpander expander)
		{
			Load(values, index, metaArea, groupList, expander);
		}

		public DatumIndex Index { get; private set; }
		public ITagGroup Group { get; set; }
		public SegmentPointer MetaLocation { get; set; }
		public TagSource Source { get; set; }

		public StructureValueCollection Serialize(IList<ITagGroup> groupList, IPointerExpander expander)
		{
			var result = new StructureValueCollection();

			uint cont = 0;
			if (MetaLocation != null)
				cont = expander.Contract(MetaLocation.AsPointer());

			result.SetInteger("memory address", cont);
			result.SetInteger("tag group index", (Group != null) ? (uint) groupList.IndexOf(Group) : 0xFFFFFFFF);
			result.SetInteger("datum index salt", Index.Salt);
			return result;
		}

		private void Load(StructureValueCollection values, ushort index, FileSegmentGroup metaArea, IList<ITagGroup> groupList, IPointerExpander expander)
		{
			uint address = (uint)values.GetInteger("memory address");

			Source = TagSource.Null;
			if (address != 0 && address != 0xFFFFFFFF)
			{
				long expanded = expander.Expand(address);

				Source = TagSource.MetaArea;
				MetaLocation = SegmentPointer.FromPointer(expanded, metaArea);
			}

			var groupIndex = (int) values.GetInteger("tag group index");
			if (groupIndex >= 0 && groupIndex < groupList.Count)
				Group = groupList[groupIndex];

			var salt = (ushort) values.GetInteger("datum index salt");
			if (salt != 0xFFFF)
				Index = new DatumIndex(salt, index);
			else
				Index = DatumIndex.Null;
		}
	}
}