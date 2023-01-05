using System;

namespace Blamite.IO
{
	/// <summary>
	///     Wraps a segment ID in a FileSegmenter.
	/// </summary>
	public class FileSegment
	{
		private readonly FileSegmenter _segmenter;

		/// <summary>
		///     Constructs a new FileSegment, wrapping a segment ID in a FileSegmenter.
		/// </summary>
		/// <param name="id">The ID of the segment to wrap.</param>
		/// <param name="segmenter">The FileSegmenter that the segment belongs to.</param>
		public FileSegment(int id, FileSegmenter segmenter)
		{
			ID = id;
			_segmenter = segmenter;
			_segmenter.SegmentResized += SegmentResized;
		}

		/// <summary>
		///     Gets the FileSegmenter-specific ID of the segment.
		/// </summary>
		public int ID { get; private set; }

		/// <summary>
		///     Gets the offset of the start of the segment in the file.
		/// </summary>
		public uint Offset
		{
			get { return _segmenter.GetSegmentOffset(ID); }
		}

		/// <summary>
		///     Gets the size of the segment.
		/// </summary>
		public uint Size
		{
			get { return _segmenter.GetSegmentSize(ID); }
		}

		/// <summary>
		///     Gets how much space the segment actually takes up in the file.
		/// </summary>
		public uint ActualSize
		{
			get { return _segmenter.GetSegmentActualSize(ID); }
		}

		/// <summary>
		///     Gets the resize origin of the segment.
		/// </summary>
		public SegmentResizeOrigin ResizeOrigin
		{
			get { return _segmenter.GetSegmentResizeOrigin(ID); }
		}

		/// <summary>
		///     Occurs when the segment has been resized.
		/// </summary>
		public event EventHandler<SegmentResizedEventArgs> Resized;

		/// <summary>
		///     Resizes the segment.
		/// </summary>
		/// <param name="newSize">The minimum amount of space that the segment must occupy.</param>
		/// <param name="stream">The stream to write padding to.</param>
		/// <returns>The new size of the segment after alignment has been applied.</returns>
		public uint Resize(uint newSize, IStream stream)
		{
			return _segmenter.ResizeSegment(ID, newSize, stream);
		}

		public override bool Equals(object obj)
		{
			var otherSegment = obj as FileSegment;
			if (otherSegment == null)
				return false;
			return (ID == otherSegment.ID);
		}

		public override int GetHashCode()
		{
			return ID + _segmenter.GetHashCode();
		}

		/// <summary>
		///     Raises the <see cref="Resized" /> event.
		/// </summary>
		/// <param name="e">The <see cref="SegmentResizedEventArgs" /> that contains the event data.</param>
		protected void OnResized(SegmentResizedEventArgs e)
		{
			if (Resized != null)
				Resized(this, e);
		}

		private void SegmentResized(object sender, SegmentResizedEventArgs e)
		{
			if (e.SegmentID == ID)
				OnResized(e);
		}
	}
}