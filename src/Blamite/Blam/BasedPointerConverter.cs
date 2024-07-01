using Blamite.IO;

namespace Blamite.Blam
{
	public class BasedPointerConverter : IPointerConverter
	{
		private readonly int _defaultStartOffset;

		public BasedPointerConverter(long basePointer, int defaultStartOffset)
		{
			BasePointer = basePointer;
			_defaultStartOffset = defaultStartOffset;
		}

		public long BasePointer { get; set; }

		public uint PointerToOffset(long pointer)
		{
			return (uint) (pointer - BasePointer + _defaultStartOffset);
		}

		public uint PointerToOffset(long pointer, uint areaStartOffset)
		{
			return (uint) (pointer - BasePointer + areaStartOffset);
		}

		public long OffsetToPointer(uint offset)
		{
			return (uint) (offset - _defaultStartOffset + BasePointer);
		}

		public long OffsetToPointer(uint offset, uint areaStartOffset)
		{
			return (uint) (offset - areaStartOffset + BasePointer);
		}
	}
}