using Blamite.IO;

namespace Blamite.Blam.ThirdGen
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

		public int PointerToOffset(long pointer)
		{
			return (int) (pointer - BasePointer + _defaultStartOffset);
		}

		public int PointerToOffset(long pointer, int areaStartOffset)
		{
			return (int) (pointer - BasePointer + areaStartOffset);
		}

		public long OffsetToPointer(int offset)
		{
			return (uint) (offset - _defaultStartOffset + BasePointer);
		}

		public long OffsetToPointer(int offset, int areaStartOffset)
		{
			return (uint) (offset - areaStartOffset + BasePointer);
		}
	}
}