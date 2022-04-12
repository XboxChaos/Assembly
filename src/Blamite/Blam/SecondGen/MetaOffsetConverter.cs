using Blamite.IO;

namespace Blamite.Blam.SecondGen
{
	public class MetaOffsetConverter : IPointerConverter
	{
		private readonly uint _mask;
		private readonly FileSegment _metaSegment;

		public MetaOffsetConverter(FileSegment metaSegment, uint mask)
		{
			_metaSegment = metaSegment;
			_mask = mask;
		}

		public int PointerToOffset(long pointer)
		{
			return PointerToOffset(pointer, _metaSegment.Offset);
		}

		public int PointerToOffset(long pointer, int areaStartOffset)
		{
			return (int)(pointer - _mask + areaStartOffset);
		}

		public long OffsetToPointer(int offset)
		{
			return OffsetToPointer(offset, _metaSegment.Offset);
		}

		public long OffsetToPointer(int offset, int areaStartOffset)
		{
			// NOTE: this *was* casting as an int, which was causing arithmetic problems
			//		 while loading tags from ogbox cache files. it should probably be tested
			//		 to make sure it doesnt break something else by casting it as unsigned
			return (uint)(offset - areaStartOffset + _mask);
		}
	}
}