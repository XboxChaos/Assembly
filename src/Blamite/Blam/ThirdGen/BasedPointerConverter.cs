using Blamite.IO;

namespace Blamite.Blam.ThirdGen
{
	public class BasedPointerConverter : IPointerConverter
	{
		private readonly int _defaultStartOffset;

		public BasedPointerConverter(uint basePointer, int defaultStartOffset)
		{
			BasePointer = basePointer;
			_defaultStartOffset = defaultStartOffset;
		}

		public uint BasePointer { get; set; }

		public int PointerToOffset(uint pointer)
		{
			return (int) (pointer - BasePointer + _defaultStartOffset);
		}

		public int PointerToOffset(uint pointer, int areaStartOffset)
		{
			return (int) (pointer - BasePointer + areaStartOffset);
		}

		public uint OffsetToPointer(int offset)
		{
			return (uint) (offset - _defaultStartOffset + BasePointer);
		}

		public uint OffsetToPointer(int offset, int areaStartOffset)
		{
			return (uint) (offset - areaStartOffset + BasePointer);
		}
	}
}