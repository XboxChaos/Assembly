using System.Collections.Generic;
using Blamite.Blam;
using Blamite.IO;

namespace Blamite.Patching
{
	public static class PatchBuilder
	{
		/// <summary>
		///     Builds a Patch by comparing two cache files.
		/// </summary>
		/// <param name="originalFile">The original cache file.</param>
		/// <param name="originalReader">The stream to use to read from the original cache file.</param>
		/// <param name="newFile">The modified cache file.</param>
		/// <param name="newReader">The stream to use to read from the modified cache file.</param>
		/// <param name="output">The Patch to store differences to.</param>
		public static void BuildPatch(ICacheFile originalFile, IReader originalReader, ICacheFile newFile, IReader newReader,
			Patch output)
		{
			output.MapInternalName = originalFile.InternalName;
			output.MetaPokeBase = newFile.MetaArea.BasePointer;

			List<SegmentChange> segmentChanges = SegmentComparer.CompareSegments(originalFile.Segments, originalReader,
				newFile.Segments, newReader);
			output.SegmentChanges.AddRange(segmentChanges);
			output.MetaChangesIndex = FindMetaChanges(segmentChanges, newFile);
		}

		/// <summary>
		///     Scans a list of segment changes to find the index of the change data related to a cache file's meta area.
		/// </summary>
		/// <param name="segmentChanges">The list of segment changes to scan.</param>
		/// <param name="newFile">The modified cache file.</param>
		/// <returns>The index of the meta change data in <paramref name="segmentChanges" />, or -1 if not found.</returns>
		private static int FindMetaChanges(IList<SegmentChange> segmentChanges, ICacheFile newFile)
		{
			for (int i = 0; i < segmentChanges.Count; i++)
			{
				if (segmentChanges[i].NewOffset == newFile.MetaArea.Offset)
					return i;
			}
			return -1;
		}
	}
}