using System;
using System.Collections.Generic;
using System.Linq;

namespace Blamite.IO
{
	/// <summary>
	///     Defines the direction in which a file segment should be resized.
	/// </summary>
	public enum SegmentResizeOrigin
	{
		/// <summary>
		///     Resizing the segment causes space to be added/removed at the beginning of the segment.
		/// </summary>
		Beginning,

		/// <summary>
		///     Resizing the segment causes space to be added/removed at the end of the segment.
		/// </summary>
		End,

		/// <summary>
		///     The segment cannot be resized.
		/// </summary>
		None
	}

	/// <summary>
	///     Provides information about events related to a segment being resized.
	/// </summary>
	public class SegmentResizedEventArgs : EventArgs
	{
		public SegmentResizedEventArgs(int segmentId, uint oldSize, uint newSize, SegmentResizeOrigin origin)
		{
			SegmentID = segmentId;
			OldSize = oldSize;
			NewSize = newSize;
			ResizeOrigin = origin;
		}

		/// <summary>
		///     Gets the ID of the segment that was resized.
		/// </summary>
		public int SegmentID { get; private set; }

		/// <summary>
		///     Gets the old size of the segment.
		/// </summary>
		public uint OldSize { get; private set; }

		/// <summary>
		///     Gets the new size of the segment.
		/// </summary>
		public uint NewSize { get; private set; }

		/// <summary>
		///     The origin at which the segment was resized.
		/// </summary>
		public SegmentResizeOrigin ResizeOrigin { get; private set; }
	}

	/// <summary>
	///     Enables a file to be divided into several segments which can be resized dynamically.
	/// </summary>
	public class FileSegmenter
	{
		private readonly int _defaultOffsetAlign = 1;
		private readonly List<InternalSegment> _segmentsById = new List<InternalSegment>();
		private readonly SortedList<uint, InternalSegment> _segmentsByOffset = new SortedList<uint, InternalSegment>();

		/// <summary>
		///     Constructs a new FileSegmenter with a default offset alignment of 1.
		/// </summary>
		public FileSegmenter()
		{
		}

		/// <summary>
		///     Constructs a new FileSegmenter with a specified default offset alignment.
		/// </summary>
		/// <param name="defaultOffsetAlign">The value to align offsets to by default.</param>
		public FileSegmenter(int defaultOffsetAlign)
		{
			_defaultOffsetAlign = defaultOffsetAlign;
		}

		/// <summary>
		///     Gets an enumerable collection of segment IDs for the segmenter.
		/// </summary>
		public IEnumerable<int> Segments
		{
			get { return Enumerable.Range(0, _segmentsById.Count); }
		}

		/// <summary>
		///     Occurs when a segment has been resized.
		/// </summary>
		public event EventHandler<SegmentResizedEventArgs> SegmentResized;

		/// <summary>
		///     Defines a new segment in the file. It will be aligned to the segmenter's default offset alignment.
		/// </summary>
		/// <param name="offset">The offset at which the segment starts.</param>
		/// <param name="size">The size of the segment.</param>
		/// <param name="sizeAlignment">A power of two which the size must be a multiple of (e.g. 0x1000 = 4kb-aligned).</param>
		/// <param name="resizeOrigin">The origin at which to insert/remove data in the segment.</param>
		/// <returns>The segment's ID number which can be used to retrieve information about it.</returns>
		public int DefineSegment(uint offset, uint size, int sizeAlignment, SegmentResizeOrigin resizeOrigin)
		{
			return DefineSegment(offset, size, _defaultOffsetAlign, sizeAlignment, resizeOrigin);
		}

		/// <summary>
		///     Defines a new segment in the file.
		/// </summary>
		/// <param name="offset">The offset at which the segment starts.</param>
		/// <param name="size">The size of the segment.</param>
		/// <param name="offsetAlignment">A power of two which the offset must be a multiple of (e.g. 0x1000 = 4kb-aligned).</param>
		/// <param name="sizeAlignment">A power of two which the size must be a multiple of (e.g. 0x1000 = 4kb-aligned).</param>
		/// <param name="resizeOrigin">The origin at which to insert/remove data in the segment.</param>
		/// <returns>The segment's ID number which can be used to retrieve information about it.</returns>
		public int DefineSegment(uint offset, uint size, int offsetAlignment, int sizeAlignment,
			SegmentResizeOrigin resizeOrigin)
		{
			if (offset != Align(offset, (uint)offsetAlignment))
				throw new ArgumentException("The segment offset is not aligned to the given alignment.");
			if (size != Align((uint)size, (uint)sizeAlignment))
				throw new ArgumentException("The segment size is not aligned to the given alignment.");
			if (_segmentsByOffset.ContainsKey(offset))
				throw new ArgumentException("A segment has already been defined at the given offset.");

			// Create and add the segment definition
			int id = _segmentsById.Count;
			var segment = new InternalSegment
			{
				ID = id,
				Offset = offset,
				Size = size,
				OffsetAlignment = offsetAlignment,
				SizeAlignment = sizeAlignment,
				ResizeOrigin = resizeOrigin
			};
			_segmentsById.Add(segment);
			_segmentsByOffset[offset] = segment;

			RecalculateActualSizes();
			return id;
		}

		/// <summary>
		///     Defines an empty segment in the file representing the end of the file, aligned to the segmenter's default offset
		///     alignment.
		/// </summary>
		/// <param name="offset">The offset of the end of the file.</param>
		/// <returns>The segment's ID number which can be used to retrieve information about it.</returns>
		public int DefineEOF(uint offset)
		{
			return DefineSegment(offset, 0, _defaultOffsetAlign, 1, SegmentResizeOrigin.None);
		}

		/// <summary>
		///     Defines an empty segment in the file representing the end of the file.
		/// </summary>
		/// <param name="offset">The offset of the end of the file.</param>
		/// <param name="alignment">The file's size alignment.</param>
		/// <returns>The segment's ID number which can be used to retrieve information about it.</returns>
		public int DefineEOF(uint offset, int alignment)
		{
			return DefineSegment(offset, 0, alignment, 1, SegmentResizeOrigin.None);
		}

		/// <summary>
		///     Gets the offset of a segment in the file.
		/// </summary>
		/// <param name="id">The ID of the segment to get the offset of.</param>
		/// <returns>The segment's offset.</returns>
		public uint GetSegmentOffset(int id)
		{
			return GetSegmentFromID(id).Offset;
		}

		/// <summary>
		///     Gets the size of a segment.
		/// </summary>
		/// <param name="id">The ID of the segment to get the size of.</param>
		/// <returns>The segment's size.</returns>
		public uint GetSegmentSize(int id)
		{
			return GetSegmentFromID(id).Size;
		}

		/// <summary>
		///     Gets the amount of space that a file segment actually takes up.
		/// </summary>
		/// <param name="id">The ID of the segment to get the actual size of.</param>
		/// <returns>The actual size of the segment.</returns>
		public uint GetSegmentActualSize(int id)
		{
			return GetSegmentFromID(id).ActualSize;
		}

		/// <summary>
		///     Gets the resize origin of a segment.
		/// </summary>
		/// <param name="id">The ID of the segment to get the resize origin of.</param>
		/// <returns>The segment's resize origin.</returns>
		public SegmentResizeOrigin GetSegmentResizeOrigin(int id)
		{
			return GetSegmentFromID(id).ResizeOrigin;
		}

		/// <summary>
		///     Resizes a segment in the file.
		/// </summary>
		/// <param name="id">The ID of the segment to resize.</param>
		/// <param name="newSize">The minimum amount of space that the segment must occupy.</param>
		/// <param name="stream">The stream to write padding to.</param>
		/// <returns>The new size of the segment after alignment has been applied.</returns>
		public uint ResizeSegment(int id, uint newSize, IStream stream)
		{
			InternalSegment segment = GetSegmentFromID(id);

			uint oldSize = segment.Size;
			if (newSize == oldSize)
				return oldSize;

			// Change the size of the segment
			uint oldActualSize = segment.ActualSize;
			uint oldSegmentEnd = segment.Offset + segment.ActualSize;
			segment.Size = Align((uint)newSize, (uint)segment.SizeAlignment);
			segment.ActualSize = Math.Max(segment.Size, segment.ActualSize);

			// Recalculate the segment offsets,
			// and then use the recalculated offsets to recalculate the sizes
			RecalculateOffsets();
			RecalculateActualSizes();

			uint resizeOffset;
			switch (segment.ResizeOrigin)
			{
				case SegmentResizeOrigin.Beginning:
					resizeOffset = segment.Offset;
					break;

				case SegmentResizeOrigin.End:
					resizeOffset = oldSegmentEnd;
					break;

				default:
					throw new ArgumentException("The segment cannot be resized.");
			}

			// Insert/remove data into/from the stream
			// FIXME: There's a bug here where changing the size of a SegmentResizeOrigin.Beginning segment won't move the data back if ActualSize doesn't change
			//        Some sort of StreamUtil.Copy() method is necessary for that
			stream.SeekTo(resizeOffset);
			if (oldActualSize < segment.ActualSize)
				StreamUtil.Insert(stream, segment.ActualSize - oldActualSize, 0);
			else if (segment.ActualSize < oldActualSize)
				throw new NotImplementedException("Segment shrinking is not supported yet.");

			OnSegmentResized(new SegmentResizedEventArgs(id, oldSize, segment.Size, segment.ResizeOrigin));
			return segment.Size;
		}

		/// <summary>
		///     Raises the <see cref="SegmentResized" /> event.
		/// </summary>
		/// <param name="e">The <see cref="SegmentResizedEventArgs" /> that contains the event data.</param>
		protected void OnSegmentResized(SegmentResizedEventArgs e)
		{
			if (SegmentResized != null)
				SegmentResized(this, e);
		}

		/// <summary>
		///     Recalculates the offset of each segment in the file after a segment's size has been changed.
		/// </summary>
		private void RecalculateOffsets()
		{
			IList<InternalSegment> segments = _segmentsByOffset.Values;
			for (int i = 1; i < segments.Count; i++)
			{
				InternalSegment thisSegment = segments[i];
				InternalSegment prevSegment = segments[i - 1];
				thisSegment.Offset = Align(prevSegment.Offset + (uint)prevSegment.ActualSize, (uint)thisSegment.OffsetAlignment);
			}
		}

		/// <summary>
		///     Recalculates the actual size of each segment in the file after a segment's offset has been changed
		///     or after a segment has been added.
		/// </summary>
		private void RecalculateActualSizes()
		{
			// Loop through the segments sorted by offset
			// Set the size of each segment to the offset of the next segment - the offset of the current segment
			IList<InternalSegment> segments = _segmentsByOffset.Values;
			for (int i = 0; i < segments.Count - 1; i++)
			{
				segments[i].ActualSize = (segments[i + 1].Offset - segments[i].Offset) /*& ~(segments[i].SizeAlignment - 1)*/;
				if (segments[i].ActualSize < segments[i].Size)
					throw new InvalidOperationException("Segment size is too large");
			}
		}

		/// <summary>
		///     Finds a segment by its ID number.
		/// </summary>
		/// <param name="id">The ID of the segment to find.</param>
		/// <returns>The segment that was found.</returns>
		/// <exception cref="ArgumentException">Thrown if the segment ID does not exist.</exception>
		private InternalSegment GetSegmentFromID(int id)
		{
			if (id < 0 || id >= _segmentsById.Count)
				throw new ArgumentException("A segment with the given ID does not exist.");
			return _segmentsById[id];
		}

		/// <summary>
		///     Aligns a value to a power of two.
		/// </summary>
		/// <param name="val">The value to align.</param>
		/// <param name="alignment">The power of two that the value must be a multiple of.</param>
		/// <returns>The aligned value.</returns>
		private static uint Align(uint val, uint alignment)
		{
			return (val + alignment - 1) & ~(alignment - 1);
		}

		private class InternalSegment
		{
			public int ID { get; set; }
			public uint Offset { get; set; }
			public uint Size { get; set; }
			public uint ActualSize { get; set; }
			public int OffsetAlignment { get; set; }
			public int SizeAlignment { get; set; }
			public SegmentResizeOrigin ResizeOrigin { get; set; }
		}
	}
}