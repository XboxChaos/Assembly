using Blamite.IO;

namespace Blamite.Blam
{
	/// <summary>
	///     Represents a partition in a cache file.
	/// </summary>
	public class Partition
	{
		/// <summary>
		///     Creates a new Partition object, given a base pointer and a size.
		/// </summary>
		/// <param name="basePointer">The pointer to the start of the partition.</param>
		/// <param name="size">The partition's size.</param>
		public Partition(SegmentPointer basePointer, uint size)
		{
			BasePointer = basePointer;
			Size = size;
		}

		/// <summary>
		///     The pointer to the start of the partition. Can be null if the partition is empty.
		/// </summary>
		public SegmentPointer BasePointer { get; set; }

		/// <summary>
		///     The size of the partition.
		/// </summary>
		public uint Size { get; set; }

		/// <summary>
		///     Returns whether the given memory address falls within this partition.
		/// </summary>
		/// <param name="address">The memory address to check.</param>
		public bool Contains(long address)
		{
			return (address >= BasePointer.AsPointer() && address < BasePointer.AsPointer() + Size);
		}
	}
}