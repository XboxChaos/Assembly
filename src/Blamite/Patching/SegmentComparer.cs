using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.IO;

namespace Blamite.Patching
{
	public static class SegmentComparer
	{
		/// <summary>
		///     Compares two sets of file segments and the segment contents.
		/// </summary>
		/// <param name="originalSegments">The original set of file segments.</param>
		/// <param name="originalReader">The stream to use to read from the original file.</param>
		/// <param name="newSegments">The modified set of file segments.</param>
		/// <param name="newReader">The stream to use to read from the modified file.</param>
		/// <returns>The differences that were found.</returns>
		public static List<SegmentChange> CompareSegments(IEnumerable<FileSegment> originalSegments, IReader originalReader,
			IEnumerable<FileSegment> newSegments, IReader newReader)
		{
			List<FileSegment> originalList = originalSegments.ToList();
			List<FileSegment> newList = newSegments.ToList();

			if (originalList.Count != newList.Count)
				throw new InvalidOperationException("The files have different segment counts");

			var results = new List<SegmentChange>();
			for (int i = 0; i < originalList.Count; i++)
			{
				FileSegment originalSegment = originalList[i];
				FileSegment newSegment = newList[i];
				List<DataChange> changes = DataComparer.CompareData(originalReader, (uint) originalSegment.Offset,
					originalSegment.ActualSize, newReader, (uint) newSegment.Offset, newSegment.ActualSize,
					originalSegment.ResizeOrigin != SegmentResizeOrigin.Beginning);
				if (changes.Count > 0 || originalSegment.Size != newSegment.Size)
				{
					var change = new SegmentChange((uint) originalSegment.Offset, originalSegment.ActualSize, (uint) newSegment.Offset,
						newSegment.ActualSize, originalSegment.ResizeOrigin != SegmentResizeOrigin.Beginning);
					change.DataChanges.AddRange(changes);
					results.Add(change);
				}
			}
			return results;
		}
	}
}