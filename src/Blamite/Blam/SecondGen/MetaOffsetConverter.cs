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

		public uint PointerToOffset(long pointer)
		{
			return PointerToOffset(pointer, _metaSegment.Offset);
		}

		public uint PointerToOffset(long pointer, uint areaStartOffset)
		{
			return (uint)(pointer - _mask + areaStartOffset);
		}

		public long OffsetToPointer(uint offset)
		{
			return OffsetToPointer(offset, _metaSegment.Offset);
		}

		public long OffsetToPointer(uint offset, uint areaStartOffset)
		{
			return (offset - areaStartOffset + _mask);
		}
	}
}