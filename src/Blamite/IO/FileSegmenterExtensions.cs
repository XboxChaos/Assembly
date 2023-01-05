using System.Collections.Generic;

namespace Blamite.IO
{
	public static class FileSegmenterExtensions
	{
		/// <summary>
		///     Defines a segment and returns a FileSegment that wraps it.
		/// </summary>
		/// <param name="segmenter">The FileSegmenter to define the segment on.</param>
		/// <param name="offset">The offset at which the segment starts.</param>
		/// <param name="size">The size of the segment.</param>
		/// <param name="sizeAlignment">A power of two which the size must be a multiple of (e.g. 0x1000 = 4kb-aligned).</param>
		/// <param name="resizeOrigin">The origin at which to insert/remove data in the segment.</param>
		/// <returns>A FileSegment wrapping the defined segment.</returns>
		public static FileSegment WrapSegment(this FileSegmenter segmenter, uint offset, uint size, int sizeAlignment,
			SegmentResizeOrigin resizeOrigin)
		{
			return new FileSegment(segmenter.DefineSegment(offset, size, sizeAlignment, resizeOrigin), segmenter);
		}

		/// <summary>
		///     Defines a segment and returns a FileSegment that wraps it.
		/// </summary>
		/// <param name="offset">The offset at which the segment starts.</param>
		/// <param name="size">The size of the segment.</param>
		/// <param name="offsetAlignment">A power of two which the offset must be a multiple of (e.g. 0x1000 = 4kb-aligned).</param>
		/// <param name="sizeAlignment">A power of two which the size must be a multiple of (e.g. 0x1000 = 4kb-aligned).</param>
		/// <param name="resizeOrigin">The origin at which to insert/remove data in the segment.</param>
		/// <returns>The segment's ID number which can be used to retrieve information about it.</returns>
		public static FileSegment WrapSegment(this FileSegmenter segmenter, uint offset, uint size, int offsetAlignment,
			int sizeAlignment, SegmentResizeOrigin resizeOrigin)
		{
			return new FileSegment(segmenter.DefineSegment(offset, size, offsetAlignment, sizeAlignment, resizeOrigin), segmenter);
		}

		/// <summary>
		///     Defines an empty segment in the file representing the end of the file, aligned to the segmenter's default offset
		///     alignment.
		/// </summary>
		/// <param name="offset">The offset of the end of the file.</param>
		/// <returns>The segment's ID number which can be used to retrieve information about it.</returns>
		public static FileSegment WrapEOF(this FileSegmenter segmenter, uint offset)
		{
			return new FileSegment(segmenter.DefineEOF(offset), segmenter);
		}

		/// <summary>
		///     Defines an empty segment in the file representing the end of the file and returns a FileSegment that wraps it.
		/// </summary>
		/// <param name="offset">The offset of the end of the file.</param>
		/// <param name="alignment">The file's size alignment.</param>
		/// <returns>The segment's ID number which can be used to retrieve information about it.</returns>
		public static FileSegment WrapEOF(this FileSegmenter segmenter, uint offset, int alignment)
		{
			return new FileSegment(segmenter.DefineEOF(offset, alignment), segmenter);
		}

		/// <summary>
		///     Gets an enumerable collection of FileSegments wrapping the segments in a FileSegmenter.
		/// </summary>
		/// <remarks>
		///     Note that the returned segments will be newly allocated and will not have the same references as other wrapper
		///     objects that have been created for the segmenter.
		/// </remarks>
		/// <returns>An enumerable collection of FileSegments for the segmenter.</returns>
		public static IEnumerable<FileSegment> GetWrappers(this FileSegmenter segmenter)
		{
			foreach (int segmentId in segmenter.Segments)
				yield return new FileSegment(segmentId, segmenter);
		}
	}
}