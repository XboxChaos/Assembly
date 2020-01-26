using Blamite.IO;

namespace Blamite.Blam.ThirdGen
{
	/// <summary>
	///     Provides methods for converting between memory addresses stored in cache files and file offsets.
	/// </summary>
	public class MetaAddressConverter : IPointerConverter
	{
		private readonly FileSegment _metaSegment;
		private long _virtualBase;

		/// <summary>
		///     Constructs a new MetaAddressConverter.
		/// </summary>
		/// <param name="metaSegment">The FileSegment where meta is stored.</param>
		/// <param name="virtualBase">The virtual base address of the meta.</param>
		public MetaAddressConverter(FileSegment metaSegment, long virtualBase)
		{
			_metaSegment = metaSegment;
			_virtualBase = virtualBase;
			metaSegment.Resized += MetaResized;
		}

		public int PointerToOffset(long pointer)
		{
			return PointerToOffset(pointer, _metaSegment.Offset);
		}

		public int PointerToOffset(long pointer, int areaStartOffset)
		{
			return (int) (pointer - _virtualBase + areaStartOffset);
		}

		public long OffsetToPointer(int offset)
		{
			return OffsetToPointer(offset, _metaSegment.Offset);
		}

		public long OffsetToPointer(int offset, int areaStartOffset)
		{
			return (offset - areaStartOffset + _virtualBase);
		}

		private void MetaResized(object sender, SegmentResizedEventArgs e)
		{
			// The meta segment grows downward in memory,
			// so change the virtual base inversely
			_virtualBase -= (uint) (e.NewSize - e.OldSize);
		}
	}
}