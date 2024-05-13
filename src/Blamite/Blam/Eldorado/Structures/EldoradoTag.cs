using Blamite.IO;

namespace Blamite.Blam.Eldorado.Structures
{
	public class EldoradoTag : ITag
	{
		public EldoradoTag(DatumIndex index, ITagGroup tagGroup, SegmentPointer headerLocation, SegmentPointer metaLocation)
		{
			Index = index;
			Group = tagGroup;
			MetaLocation = metaLocation;

			Source = MetaLocation == null ? TagSource.Null : TagSource.Eldorado;
		}

		public DatumIndex Index { get; private set; }
		public ITagGroup Group { get; set; }
		public SegmentPointer MetaLocation { get; set; }
		public TagSource Source { get; set; }
	}
}
