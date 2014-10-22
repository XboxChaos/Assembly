using System;
using System.IO;
using System.IO.Compression;
using Blamite.IO;

namespace Blamite.Blam.Resources
{
	/// <summary>
	///     Extracts resource pages from cache files.
	/// </summary>
	public class ResourcePageExtractor
	{
		private readonly FileSegment _rawTable;

		/// <summary>
		///     Creates a new ResourcePageExtractor which can extract resource pages from a cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to extract resource pages from.</param>
		public ResourcePageExtractor(ICacheFile cacheFile)
		{
			_rawTable = cacheFile.RawTable;
		}

		/// <summary>
		///     Extracts a page, copying it to an output stream.
		/// </summary>
		/// <param name="page">The page to decompress and extract.</param>
		/// <param name="inStream">The Stream open on the extractor's cache file to read the page from.</param>
		/// <param name="outStream">The Stream to write the extracted page to.</param>
		public void ExtractPage(ResourcePage page, Stream inStream, Stream outStream)
		{
			inStream.Position = _rawTable.Offset + page.Offset;
			StreamUtil.Copy(inStream, outStream, page.CompressedSize);
		}

		/// <summary>
		///     Extracts and decompresses a page, copying it to an output stream.
		/// </summary>
		/// <param name="page">The page to decompress and extract.</param>
		/// <param name="inStream">The Stream open on the extractor's cache file to read the page from.</param>
		/// <param name="outStream">The Stream to write the extracted page to.</param>
		public void ExtractDecompressPage(ResourcePage page, Stream inStream, Stream outStream)
		{
			inStream.Position = _rawTable.Offset + page.Offset;

			switch (page.CompressionMethod)
			{
				case ResourcePageCompression.None:
					StreamUtil.Copy(inStream, outStream, page.UncompressedSize);
					break;

				case ResourcePageCompression.Deflate:
					var deflate = new DeflateStream(inStream, CompressionMode.Decompress, true);
					StreamUtil.Copy(deflate, outStream, page.UncompressedSize);
					break;

				default:
					throw new NotSupportedException("Unsupported compression method");
			}
		}
	}
}