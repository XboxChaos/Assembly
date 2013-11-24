using System.Collections.Generic;
using Blamite.Blam;
using Blamite.IO;

namespace Blamite.Patching
{
	/// <summary>
	///     [DEPRECATED] Provides static methods for patching tag meta in a cache file.
	/// </summary>
	public static class MetaPatcher
	{
		/// <summary>
		///     Writes a series of meta changes back to a cache file.
		/// </summary>
		/// <param name="changes">The changes to write.</param>
		/// <param name="cacheFile">The cache file to write the changes to.</param>
		/// <param name="output">The stream to write the changes to.</param>
		public static void WriteChanges(IEnumerable<DataChange> changes, ICacheFile cacheFile, IWriter output)
		{
			foreach (DataChange change in changes)
				WriteChange(cacheFile, change, output);
		}

		/// <summary>
		///     Writes a meta change back to a cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to write the change to.</param>
		/// <param name="change">The change to write.</param>
		/// <param name="output">The stream to write the change to.</param>
		public static void WriteChange(ICacheFile cacheFile, DataChange change, IWriter output)
		{
			output.SeekTo(cacheFile.MetaArea.PointerToOffset(change.Offset));
			output.WriteBlock(change.Data);
		}
	}
}