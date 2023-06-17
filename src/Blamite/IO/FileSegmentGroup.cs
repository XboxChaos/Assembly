using System;
using System.Collections.Generic;

namespace Blamite.IO
{
	/// <summary>
	///     A group of file segments which share a common pointer system.
	/// </summary>
	public class FileSegmentGroup
	{
		private readonly IPointerConverter _pointerConverter;
		private readonly SortedList<uint, FileSegment> _segmentsByOffset = new SortedList<uint, FileSegment>();

		/// <summary>
		///     Constructs a new FileSegmentGroup where pointers are equivalent to file offsets.
		/// </summary>
		public FileSegmentGroup()
		{
		}

		/// <summary>
		///     Constructs a new FileSegmentGroup.
		/// </summary>
		/// <param name="pointerConverter">The IPointerConverter to use to convert pointers to and from file offsets.</param>
		public FileSegmentGroup(IPointerConverter pointerConverter)
		{
			_pointerConverter = pointerConverter;
		}

		/// <summary>
		///     Gets the lowest possible value a valid pointer can have.
		/// </summary>
		public long BasePointer
		{
			get
			{
				if (_pointerConverter != null)
					return OffsetToPointer(Offset);
				return 0;
			}
		}

		/// <summary>
		///     Gets the value to subtract from a pointer to convert it to a file offset.
		/// </summary>
		public long PointerMask
		{
			get
			{
				if (_pointerConverter != null)
					return OffsetToPointer(0);
				return 0;
			}
		}

		/// <summary>
		///     Gets the offset of the first segment in the group.
		/// </summary>
		public uint Offset
		{
			get
			{
				if (Segments.Count > 0)
					return Segments[0].Offset;
				return 0;
			}
		}

		/// <summary>
		///     Gets the actual total size of the group.
		/// </summary>
		public uint Size
		{
			get
			{
				if (Segments.Count > 0)
					return Segments[Segments.Count - 1].Offset - Segments[0].Offset + Segments[Segments.Count - 1].ActualSize;
				return 0;
			}
		}

		/// <summary>
		///     Gets the virtual size of the group.
		/// </summary>
		public uint VirtualSize
		{
			get
			{
				if (Segments.Count > 0)
					return Segments[Segments.Count - 1].Offset - Segments[0].Offset + Segments[Segments.Count - 1].Size;
				return 0;
			}
		}

		/// <summary>
		///     Gets a list of the segments in the group sorted by their offset in the file.
		/// </summary>
		public IList<FileSegment> Segments
		{
			get { return _segmentsByOffset.Values; }
		}

		/// <summary>
		///     Occurs when a segment in the group has been resized.
		/// </summary>
		public event EventHandler<SegmentResizedEventArgs> MemberSegmentResized;

		/// <summary>
		///     Adds a segment to the group.
		/// </summary>
		/// <param name="segment">The segment to add.</param>
		/// <returns>A SegmentPointer pointing to the beginning of the segment.</returns>
		public SegmentPointer AddSegment(FileSegment segment)
		{
			uint offset = segment.Offset;
			if (_segmentsByOffset.ContainsKey(offset))
				throw new ArgumentException("A segment has already been added at the given offset.");

			_segmentsByOffset[segment.Offset] = segment;
			segment.Resized += SegmentResized;

			return SegmentPointer.FromOffset(segment.Offset, this);
		}

		/// <summary>
		///     Returns whether or not a given pointer falls inside the group.
		/// </summary>
		/// <param name="pointer">The pointer to test.</param>
		/// <returns>true if the pointer falls inside a segment in the group.</returns>
		public bool ContainsPointer(long pointer)
		{
			if (Segments.Count == 0)
				return false;

			long basePointer = BasePointer;
			return (pointer >= basePointer && pointer < basePointer + Size);
		}

		/// <summary>
		///     Returns whether or not a block of data defined by a pointer and a size is completely inside the group.
		/// </summary>
		/// <param name="pointer">The pointer to the start of the block.</param>
		/// <param name="size">The size of the block in bytes.</param>
		/// <returns>true if the block falls completely inside the group.</returns>
		public bool ContainsBlockPointer(long pointer, uint size)
		{
			if (Segments.Count == 0)
				return false;

			long basePointer = BasePointer;
			return ((long)(pointer + size) >= pointer && pointer >= basePointer && pointer + size <= basePointer + Size);
		}

		/// <summary>
		///     Returns whether or not a given offset falls inside the group.
		/// </summary>
		/// <param name="pointer">The offset to test.</param>
		/// <returns>true if the offset falls inside a segment in the group.</returns>
		public bool ContainsOffset(uint offset)
		{
			if (Segments.Count == 0)
				return false;

			return (offset >= Offset && offset < Offset + Size);
		}

		/// <summary>
		///     Returns whether or not a block of data defined by a file offset and a size is completely inside the group.
		/// </summary>
		/// <param name="offset">The file offset of the start of the block.</param>
		/// <param name="size">The size of the block in bytes.</param>
		/// <returns>true if the block falls completely inside the group.</returns>
		public bool ContainsBlockOffset(uint offset, int size)
		{
			if (Segments.Count == 0)
				return false;

			uint groupOffset = Offset;
			return (offset + size >= offset && offset >= groupOffset && offset + size <= groupOffset + Size);
		}

		/// <summary>
		///     Finds the segment in the group which contains a given file offset.
		/// </summary>
		/// <param name="offset">The offset to search for.</param>
		/// <returns>The FileSegment containing the offset if found, or null otherwise.</returns>
		public FileSegment FindSegmentWithOffset(uint offset)
		{
			// Just do a linear search for now, if this gets slow then it can be converted to binary search or something
			foreach (FileSegment segment in Segments)
			{
				if (offset >= segment.Offset && offset < segment.Offset + segment.Size)
					return segment;
			}
			return null;
		}

		/// <summary>
		///     Finds the segment in the group which contains a given pointer.
		/// </summary>
		/// <param name="pointer">The pointer to search for.</param>
		/// <returns>The FileSegment containing the pointer if found, or null otherwise.</returns>
		public FileSegment FindSegmentWithPointer(long pointer)
		{
			if (Segments.Count == 0)
				return null;

			// Just do a linear search for now, if this gets slow then it can be converted to binary search or something
			long currentPointer = OffsetToPointer(Segments[0].Offset);
			foreach (FileSegment segment in Segments)
			{
				if (pointer >= currentPointer && pointer < currentPointer + segment.Size)
					return segment;
				currentPointer += (uint) segment.Size;
			}
			return null;
		}

		/// <summary>
		///     Converts a pointer into the group to a file offset.
		/// </summary>
		/// <param name="pointer">The pointer to convert.</param>
		/// <returns>The file offset corresponding to the pointer.</returns>
		public uint PointerToOffset(long pointer)
		{
			if (_pointerConverter != null)
			{
				if (Segments.Count > 0)
					return _pointerConverter.PointerToOffset(pointer, Offset);
				return _pointerConverter.PointerToOffset(pointer);
			}
			return (uint) pointer;
		}

		/// <summary>
		///     Converts a file offset to a pointer into the group.
		/// </summary>
		/// <param name="offset">The file offset to convert.</param>
		/// <returns>The pointer corresponding to the file offset.</returns>
		public long OffsetToPointer(uint offset)
		{
			if (_pointerConverter != null)
			{
				if (Segments.Count > 0)
					return _pointerConverter.OffsetToPointer(offset, Offset);
				return _pointerConverter.OffsetToPointer(offset);
			}
			return (uint) offset;
		}

		/// <summary>
		///     Resizes the last segment in the group, changing the total size of the group to be at least a specified size.
		/// </summary>
		/// <param name="newSize">The total amount of space that the resized group should at least occupy.</param>
		/// <param name="stream">The stream to write changes to.</param>
		public void Resize(uint newSize, IStream stream)
		{
			if (Segments.Count == 0)
				return;

			FileSegment lastSegment = Segments[Segments.Count - 1];
			uint newLastSegmentSize = newSize - (lastSegment.Offset - Offset);
			if (newLastSegmentSize <= 0)
				throw new ArgumentException("Cannot shrink the group enough without deleting the last segment");

			lastSegment.Resize(newLastSegmentSize, stream);
		}

		/// <summary>
		///     Raises the <see cref="MemberSegmentResized" /> event.
		/// </summary>
		/// <param name="e">The <see cref="SegmentResizedEventArgs" /> that contains the event data.</param>
		protected void OnMemberSegmentResized(SegmentResizedEventArgs e)
		{
			if (MemberSegmentResized != null)
				MemberSegmentResized(this, e);
		}

		private void SegmentResized(object sender, SegmentResizedEventArgs e)
		{
			OnMemberSegmentResized(e);
		}
	}
}