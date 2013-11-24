using System.Collections.Generic;
using Blamite.IO;

namespace Blamite.Patching
{
	public static class DataPatcher
	{
		/// <summary>
		///     Applies a set of data changes to a stream.
		/// </summary>
		/// <param name="changes">The data changes to apply.</param>
		/// <param name="baseOffset">The offset of the start of the area where the changes should be applied.</param>
		/// <param name="writer">The stream to write changes to.</param>
		public static void PatchData(IEnumerable<DataChange> changes, uint baseOffset, IWriter writer)
		{
			foreach (DataChange change in changes)
			{
				writer.SeekTo(baseOffset + change.Offset);
				writer.WriteBlock(change.Data);
			}
		}
	}
}