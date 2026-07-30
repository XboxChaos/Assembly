using System;
using System.IO;
using Blamite.Serialization;

namespace Blamite.IO.IoStore
{
	/// <summary>
	///     A complete IoStore container: a .utoc table of contents paired with the .ucas file
	///     holding the bytes it describes.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Chunks are addressed in a logical, decompressed stream which is cut into fixed-size
	///         windows, and only the container's compressed block table says where a given window's
	///         bytes physically live. That indirection is not optional or skippable: logical offsets
	///         are quantized to the window size, so a chunk's logical offset routinely lands past
	///         the end of the entire .ucas file, and treating one as a file offset produces garbage
	///         or an out-of-range read rather than an obvious failure.
	///     </para>
	///     <para>
	///         Blocks stored verbatim need no decompressor, so a container built entirely from
	///         stored blocks - which is what a hand-built override container looks like - reads end
	///         to end with no Oodle implementation present.
	///     </para>
	/// </remarks>
	public class IoStoreContainer : IDisposable
	{
		private readonly IReader _dataReader;
		private readonly IOodleDecompressor _decompressor;
		private readonly bool _ownsDataReader;

		/// <summary>
		///     Initializes a new instance of the <see cref="IoStoreContainer" /> class around an
		///     already-open data stream, which the container will not dispose.
		/// </summary>
		/// <param name="tableOfContents">The container's parsed table of contents.</param>
		/// <param name="dataReader">A stream over the container's .ucas file.</param>
		/// <param name="decompressor">
		///     The decompressor to expand compressed blocks with, or <c>null</c> to use
		///     <see cref="UnsupportedOodleDecompressor" />, which reads stored blocks and refuses
		///     compressed ones.
		/// </param>
		public IoStoreContainer(IoStoreTableOfContents tableOfContents, IReader dataReader,
			IOodleDecompressor decompressor)
			: this(tableOfContents, dataReader, decompressor, false)
		{
		}

		private IoStoreContainer(IoStoreTableOfContents tableOfContents, IReader dataReader,
			IOodleDecompressor decompressor, bool ownsDataReader)
		{
			if (tableOfContents == null)
				throw new ArgumentNullException("tableOfContents");
			if (dataReader == null)
				throw new ArgumentNullException("dataReader");

			TableOfContents = tableOfContents;
			_dataReader = dataReader;
			_decompressor = decompressor ?? UnsupportedOodleDecompressor.Instance;
			_ownsDataReader = ownsDataReader;
		}

		/// <summary>
		///     Gets the container's table of contents.
		/// </summary>
		public IoStoreTableOfContents TableOfContents { get; private set; }

		/// <summary>
		///     Opens a container from a path to its .utoc file, deriving the .ucas path from it.
		/// </summary>
		/// <param name="tocPath">The path to the container's .utoc file.</param>
		/// <param name="layouts">
		///     The layout collection holding the <see cref="IoStoreTableOfContents.TocHeaderLayoutName" />
		///     layout.
		/// </param>
		/// <param name="decompressor">
		///     The decompressor to expand compressed blocks with, or <c>null</c> to read only stored
		///     blocks.
		/// </param>
		/// <returns>The container, which owns and will dispose the streams it opened.</returns>
		public static IoStoreContainer Open(string tocPath, StructureLayoutCollection layouts,
			IOodleDecompressor decompressor)
		{
			return Open(tocPath, Path.ChangeExtension(tocPath, ".ucas"), layouts, decompressor);
		}

		/// <summary>
		///     Opens a container from explicit .utoc and .ucas paths.
		/// </summary>
		/// <param name="tocPath">The path to the container's .utoc file.</param>
		/// <param name="dataPath">The path to the container's .ucas file.</param>
		/// <param name="layouts">
		///     The layout collection holding the <see cref="IoStoreTableOfContents.TocHeaderLayoutName" />
		///     layout.
		/// </param>
		/// <param name="decompressor">
		///     The decompressor to expand compressed blocks with, or <c>null</c> to read only stored
		///     blocks.
		/// </param>
		/// <returns>The container, which owns and will dispose the streams it opened.</returns>
		public static IoStoreContainer Open(string tocPath, string dataPath, StructureLayoutCollection layouts,
			IOodleDecompressor decompressor)
		{
			IoStoreTableOfContents toc;
			using (IReader tocReader = new FileStreamManager(tocPath, Endian.LittleEndian).OpenRead())
				toc = new IoStoreTableOfContents(tocReader, layouts);

			IReader dataReader = new FileStreamManager(dataPath, Endian.LittleEndian).OpenRead();
			try
			{
				return new IoStoreContainer(toc, dataReader, decompressor, true);
			}
			catch
			{
				dataReader.Dispose();
				throw;
			}
		}

		/// <summary>
		///     Determines whether the container's directory index names a given file.
		/// </summary>
		/// <param name="path">The path to look for. See <see cref="IoDirectoryIndex.TryGetChunkIndex" />.</param>
		/// <returns><c>true</c> if the path resolves to a chunk in this container.</returns>
		public bool ContainsFile(string path)
		{
			return TableOfContents.FindChunk(path) >= 0;
		}

		/// <summary>
		///     Reads the contents of a chunk, decompressing it if it needs it.
		/// </summary>
		/// <param name="entryIndex">The chunk's index in the table of contents.</param>
		/// <returns>The chunk's decompressed bytes.</returns>
		public byte[] ReadChunk(int entryIndex)
		{
			if (entryIndex < 0 || entryIndex >= TableOfContents.ChunkIds.Count)
			{
				throw new ArgumentOutOfRangeException("entryIndex", entryIndex, string.Format(
					"This container has {0} entries.", TableOfContents.ChunkIds.Count));
			}

			IoChunkLocation location = TableOfContents.ChunkLocations[entryIndex];
			return ReadLogicalRange(location.Offset, location.Length, entryIndex.ToString());
		}

		/// <summary>
		///     Reads the contents of a chunk identified by its ID.
		/// </summary>
		/// <param name="chunkId">The ID of the chunk to read.</param>
		/// <returns>The chunk's decompressed bytes.</returns>
		/// <exception cref="IoStoreException">Thrown if the container does not hold the chunk.</exception>
		public byte[] ReadChunk(IoChunkId chunkId)
		{
			int entryIndex = TableOfContents.FindChunk(chunkId);
			if (entryIndex < 0)
				throw new IoStoreException(string.Format("This container does not hold chunk {0}.", chunkId));
			return ReadChunk(entryIndex);
		}

		/// <summary>
		///     Reads the contents of a file named by the container's directory index.
		/// </summary>
		/// <param name="path">The path to read. See <see cref="IoDirectoryIndex.TryGetChunkIndex" />.</param>
		/// <returns>The file's decompressed bytes.</returns>
		/// <exception cref="IoStoreException">Thrown if the container does not name the path.</exception>
		public byte[] ReadFile(string path)
		{
			int entryIndex = TableOfContents.FindChunk(path);
			if (entryIndex < 0)
			{
				throw new IoStoreException(string.Format(
					TableOfContents.DirectoryIndex == null
						? "This container has no directory index, so \"{0}\" cannot be resolved by path."
						: "This container's directory index does not name \"{0}\".", path));
			}
			return ReadChunk(entryIndex);
		}

		/// <summary>
		///     Releases the streams the container opened for itself.
		/// </summary>
		public void Dispose()
		{
			if (_ownsDataReader)
				_dataReader.Dispose();
		}

		/// <summary>
		///     Gathers a range of the container's logical data stream.
		/// </summary>
		/// <param name="offset">The offset into the logical stream to start at.</param>
		/// <param name="length">The number of bytes to gather.</param>
		/// <param name="description">What is being read, for error messages.</param>
		/// <returns>The bytes that were gathered.</returns>
		private byte[] ReadLogicalRange(long offset, long length, string description)
		{
			if (offset < 0 || length < 0 || length > int.MaxValue)
			{
				throw new IoStoreException(string.Format(
					"Chunk {0} claims 0x{1:X} bytes at logical offset 0x{2:X}, which cannot be read.", description,
					length, offset));
			}
			if (length == 0)
				return new byte[0];

			int blockSize = TableOfContents.CompressionBlockSize;

			// The logical stream is cut into fixed-size windows, so which blocks a range touches is
			// pure division - the block table is not searched. A range starting exactly on a window
			// boundary begins at the start of that window, and a range ending exactly on one does
			// not reach into the window after it, which is what the -1 is for.
			long firstBlock = offset/blockSize;
			long lastBlock = (offset + length - 1)/blockSize;
			if (lastBlock >= TableOfContents.CompressedBlocks.Count)
			{
				throw new IoStoreException(string.Format(
					"Chunk {0} spans compressed blocks {1} to {2}, but the container only has {3}.", description,
					firstBlock, lastBlock, TableOfContents.CompressedBlocks.Count));
			}

			var output = new byte[length];
			int written = 0;
			for (long blockIndex = firstBlock; blockIndex <= lastBlock; blockIndex++)
			{
				byte[] blockData = ReadBlock((int) blockIndex);

				// Where the still-unwritten part of the range begins inside this window. Only the
				// first window can start part-way in; every later one starts at zero.
				long blockStart = blockIndex*blockSize;
				long logicalPosition = offset + written;
				var insideBlock = (int) (logicalPosition - blockStart);
				if (insideBlock < 0 || insideBlock > blockData.Length)
				{
					throw new IoStoreException(string.Format(
						"Chunk {0} needs logical offset 0x{1:X}, which block {2} does not cover.", description,
						logicalPosition, blockIndex));
				}

				int available = blockData.Length - insideBlock;
				var take = (int) Math.Min(available, length - written);
				if (take <= 0)
				{
					throw new IoStoreException(string.Format(
						"Chunk {0} still needs 0x{1:X} bytes, but block {2} expands to only 0x{3:X}.", description,
						length - written, blockIndex, blockData.Length));
				}

				Buffer.BlockCopy(blockData, insideBlock, output, written, take);
				written += take;
			}

			if (written != length)
			{
				throw new IoStoreException(string.Format(
					"Chunk {0} gathered 0x{1:X} of its 0x{2:X} bytes.", description, written, length));
			}
			return output;
		}

		/// <summary>
		///     Reads one block from the .ucas file and expands it if it is compressed.
		/// </summary>
		/// <param name="blockIndex">The index of the block in the container's block table.</param>
		/// <returns>The block's decompressed bytes.</returns>
		private byte[] ReadBlock(int blockIndex)
		{
			IoCompressedBlock block = TableOfContents.CompressedBlocks[blockIndex];
			if (block.Offset < 0 || block.CompressedSize < 0 ||
				block.Offset + block.CompressedSize > _dataReader.Length)
			{
				throw new IoStoreException(string.Format(
					"Compressed block {0} covers 0x{1:X} bytes at 0x{2:X}, which is outside the 0x{3:X} byte " +
					"container data file.", blockIndex, block.CompressedSize, block.Offset, _dataReader.Length));
			}

			var source = new byte[block.CompressedSize];
			_dataReader.SeekTo(block.Offset);
			int read = _dataReader.ReadBlock(source, 0, source.Length);
			if (read != source.Length)
			{
				throw new IoStoreException(string.Format(
					"Compressed block {0} is 0x{1:X} bytes but only 0x{2:X} could be read.", blockIndex,
					source.Length, read));
			}

			if (block.IsStored)
				return TrimStoredBlock(source, block, blockIndex);

			string methodName = TableOfContents.GetCompressionMethodName(block.CompressionMethod);
			byte[] expanded = _decompressor.Decompress(methodName, source, block.UncompressedSize);
			if (expanded == null || expanded.Length != block.UncompressedSize)
			{
				throw new IoStoreException(string.Format(
					"The \"{0}\" decompressor returned 0x{1:X} bytes for block {2}, which declares 0x{3:X}.",
					methodName, expanded != null ? expanded.Length : 0, blockIndex, block.UncompressedSize));
			}
			return expanded;
		}

		/// <summary>
		///     Handles a block that is stored verbatim.
		/// </summary>
		/// <param name="source">The block's bytes exactly as they appear in the .ucas file.</param>
		/// <param name="block">The block's table entry.</param>
		/// <param name="blockIndex">The index of the block, for error messages.</param>
		/// <returns>The block's bytes, trimmed to its uncompressed size if it was padded.</returns>
		private static byte[] TrimStoredBlock(byte[] source, IoCompressedBlock block, int blockIndex)
		{
			// Method 0 means the bytes are already what they should be, so this path never touches a
			// decompressor. It is also the only path a container of stored blocks ever takes.
			if (block.UncompressedSize == source.Length)
				return source;

			if (block.UncompressedSize > source.Length)
			{
				throw new IoStoreException(string.Format(
					"Stored block {0} occupies 0x{1:X} bytes but claims to expand to 0x{2:X}.", blockIndex,
					source.Length, block.UncompressedSize));
			}

			// A stored block may be padded out on disk; only the declared size is real data.
			var trimmed = new byte[block.UncompressedSize];
			Buffer.BlockCopy(source, 0, trimmed, 0, trimmed.Length);
			return trimmed;
		}
	}
}
