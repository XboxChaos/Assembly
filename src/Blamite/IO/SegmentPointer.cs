using System;

namespace Blamite.IO
{
	/// <summary>
	///     A dynamic pointer to an offset within a segment.
	/// </summary>
	public class SegmentPointer
	{
		private readonly bool _baseBottomResizes;
		private readonly FileSegmentGroup _baseGroup;
		private readonly FileSegment _baseSegment;
		private readonly uint _baseSegmentDelta;
		private readonly uint _originalBaseSize;

		private SegmentPointer(FileSegment baseSegment, FileSegmentGroup baseGroup, uint baseSegmentDelta)
		{
			_baseSegment = baseSegment;
			_baseGroup = baseGroup;
			_baseSegmentDelta = baseSegmentDelta;
			_originalBaseSize = baseSegment.Size;
			_baseBottomResizes = (baseSegment.ResizeOrigin == SegmentResizeOrigin.Beginning);
		}

		/// <summary>
		///     Given a file offset, creates a SegmentPointer which points to a segment in a certain group.
		/// </summary>
		/// <param name="fileOffset">The file offset to create a SegmentPointer to.</param>
		/// <param name="baseGroup">The segment group containing the file offset.</param>
		/// <returns>The SegmentPointer corresponding to the file offset.</returns>
		public static SegmentPointer FromOffset(uint fileOffset, FileSegmentGroup baseGroup)
		{
			if (baseGroup.Segments.Count == 0)
				throw new ArgumentException("Cannot create a SegmentPointer from an empty group");

			FileSegment baseSegment = baseGroup.FindSegmentWithOffset(fileOffset);
			if (baseSegment == null)
				throw new ArgumentException("Cannot create a SegmentPointer from an invalid offset");

			uint baseSegmentDelta = fileOffset - baseSegment.Offset;
			return new SegmentPointer(baseSegment, baseGroup, baseSegmentDelta);
		}

		/// <summary>
		///     Given a pointer, creates a SegmentPointer which points to a segment in a certain group.
		/// </summary>
		/// <param name="pointer">The pointer to create a SegmentPointer to.</param>
		/// <param name="baseGroup">The segment group containing the pointer.</param>
		/// <returns>The SegmentPointer corresponding to the file pointer.</returns>
		public static SegmentPointer FromPointer(long pointer, FileSegmentGroup baseGroup)
		{
			if (baseGroup.Segments.Count == 0)
				throw new ArgumentException("Cannot create a SegmentPointer from an empty group");

			FileSegment baseSegment = baseGroup.FindSegmentWithPointer(pointer);
			if (baseSegment == null)
				throw new ArgumentException("Cannot create a SegmentPointer from an invalid pointer");

			uint baseSegmentDelta = baseGroup.PointerToOffset(pointer) - baseSegment.Offset;
			return new SegmentPointer(baseSegment, baseGroup, baseSegmentDelta);
		}

		/// <summary>
		///     Gets the file offset corresponding to the SegmentPointer.
		/// </summary>
		/// <returns>The file offset corresponding to the SegmentPointer.</returns>
		public uint AsOffset()
		{
			if (_baseBottomResizes)
				return (uint)(_baseSegment.Offset + _baseSegment.Size - _originalBaseSize + _baseSegmentDelta);
			// Account for data inserted at the beginning
			return _baseSegment.Offset + _baseSegmentDelta;
		}

		/// <summary>
		///     Gets the raw pointer corresponding to the SegmentPointer.
		/// </summary>
		/// <returns>The raw pointer corresponding to the SegmentPointer.</returns>
		public long AsPointer()
		{
			return _baseGroup.OffsetToPointer(AsOffset());
		}

		/// <summary>
		/// Gets the FileSegmentGroup that contains this Segment
		/// </summary>
		public FileSegmentGroup BaseGroup
		{
			get { return _baseGroup; }
		}
	}
}