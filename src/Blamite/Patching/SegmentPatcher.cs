using System.Collections.Generic;
using Blamite.IO;

namespace Blamite.Patching
{
	public static class SegmentPatcher
	{
		/// <summary>
		///     Patches the file segments in a stream.
		/// </summary>
		/// <param name="changes">The changes to make to the segments and their data.</param>
		/// <param name="stream">The stream to write changes to.</param>
		public static void PatchSegments(IEnumerable<SegmentChange> changes, IStream stream)
		{
			// Sort changes by their offsets
			var changesByOffset = new SortedList<uint, SegmentChange>();
			foreach (SegmentChange change in changes)
				changesByOffset[change.OldOffset] = change;

			// Now adjust each segment
			foreach (SegmentChange change in changesByOffset.Values)
			{
				// Resize it if necessary
				if (change.NewSize != change.OldSize)
				{
					if (change.ResizeAtEnd)
						stream.SeekTo(change.NewOffset + change.OldSize);
					else
						stream.SeekTo(change.NewOffset);

					StreamUtil.Insert(stream, change.NewSize - change.OldSize, 0);
				}

				// Patch its data
				DataPatcher.PatchData(change.DataChanges, change.NewOffset, stream);
			}
		}
	}
}