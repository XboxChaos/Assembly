using System;
using Blamite.IO;

namespace Blamite.Blam.Resources
{
	/// <summary>
	/// 
	/// </summary>
	public class ResourcePageInjector
	{
		private readonly FileSegment _rawTable;

		/// <summary>
		///     Creates a new ResourcePageInjector which can inject resource pages into a cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to inject resource pages into.</param>
		public ResourcePageInjector(ICacheFile cacheFile)
		{
			_rawTable = cacheFile.RawTable;
		}

		public int InjectPage(IStream cacheStream, ResourcePage page, byte[] data)
		{
			const int pageSize = 0x1000;

			// Calculate how many pages we need to add to the raw table
			var pagesNeeded = (int)(Math.Ceiling(page.CompressedSize / (double)pageSize));

			// calculate sizes, positions and resize the raw table
			var injectSize = pagesNeeded * pageSize;
			var offsetInCache = (_rawTable.Offset + _rawTable.Size);
			var offsetInRaw = (_rawTable.Size);
			_rawTable.Resize(_rawTable.Size + injectSize, cacheStream);

			// write the raw page into the table
			cacheStream.SeekTo(offsetInCache);
			cacheStream.WriteBlock(data);

			return offsetInRaw;
		}
	}
}
