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

		public uint PointerToOffset(long pointer)
		{
			return PointerToOffset(pointer, _metaSegment.Offset);
		}

		public uint PointerToOffset(long pointer, uint areaStartOffset)
		{
			return (uint) (pointer - _virtualBase + areaStartOffset);
		}

		public long OffsetToPointer(uint offset)
		{
			return OffsetToPointer(offset, _metaSegment.Offset);
		}

		public long OffsetToPointer(uint offset, uint areaStartOffset)
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