using System.Collections.Generic;
using Blamite.IO;

namespace Blamite.Blam.Scripting
{
	public class StringTableReader
	{
		private readonly SortedSet<uint> _requestedStrings = new SortedSet<uint>();

		public void RequestString(uint offset)
		{
			_requestedStrings.Add(offset);
		}

		public void ReadRequestedStrings(IReader reader, uint tableOffset, CachedStringTable output)
		{
			uint lastEnd = 0;
			foreach (uint offset in _requestedStrings)
			{
				if (offset < lastEnd)
                {
					continue;
				}

				reader.SeekTo(tableOffset + offset);
				string str = reader.ReadWin1252();
				output.CacheString(offset, str);
				lastEnd = offset + (uint)str.Length;
			}
		}
	}
}