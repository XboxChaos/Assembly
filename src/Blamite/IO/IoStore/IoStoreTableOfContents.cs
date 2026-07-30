using System;
using System.Collections.Generic;
using System.Text;
using Blamite.Serialization;

namespace Blamite.IO.IoStore
{
	/// <summary>
	///     A parsed IoStore table of contents: the .utoc half of a .utoc/.ucas container pair.
	/// </summary>
	/// <remarks>
	///     <para>
	///         A .utoc is a fixed 0x90 byte header - described by the "toc header" layout rather
	///         than by offsets in code - followed by a run of arrays whose lengths all come from
	///         counts in that header, in this order:
	///     </para>
	///     <code>
	///     chunk IDs                    entry count       * 12
	///     chunk offsets and lengths    entry count       * 10
	///     perfect hash seeds           seed count        * 4    (skipped)
	///     chunks without perfect hash  without count     * 4    (skipped)
	///     compressed blocks            block count       * 12
	///     compression method names      name count       * name length
	///     signature block              only when the Signed flag is set
	///     directory index              directory index size bytes
	///     </code>
	///     <para>
	///         Nothing separates one array from the next, so the arrays are walked in order and the
	///         cursor is checked against the position the header's counts predict for the directory
	///         index. A mismatch means a record width is wrong and everything after it would be
	///         nonsense, so it throws rather than carrying on.
	///     </para>
	///     <para>
	///         Both perfect hash arrays are skipped outright. Chunks are found either through the
	///         directory index or by scanning the chunk ID array, and neither needs the hash tables.
	///     </para>
	/// </remarks>
	public class IoStoreTableOfContents
	{
		/// <summary>
		///     The magic string every .utoc file starts with.
		/// </summary>
		public const string Magic = "-==--==--==--==-";

		/// <summary>
		///     The only table of contents version Campaign Evolved uses, and the only one this
		///     reader claims to understand.
		/// </summary>
		public const byte SupportedVersion = 8;

		/// <summary>
		///     The size of the fixed header in bytes.
		/// </summary>
		public const int HeaderSizeInBytes = 0x90;

		/// <summary>
		///     The name of the layout describing the fixed header.
		/// </summary>
		/// <remarks>
		///     The layout itself ships as data, in the Formats folder, so a future build changing a
		///     header field stays a data change. Callers hand the loaded collection in - normally
		///     the engine definition's own layouts - rather than this namespace resolving a path,
		///     which keeps container reading independent of any one game's Formats directory.
		/// </remarks>
		public const string TocHeaderLayoutName = "toc header";

		private readonly Dictionary<IoChunkId, int> _entriesByChunkId = new Dictionary<IoChunkId, int>();
		private IoChunkId[] _chunkIds;
		private IoChunkLocation[] _chunkLocations;
		private IoCompressedBlock[] _compressedBlocks;
		private string[] _compressionMethods;
		private long _baseOffset;

		/// <summary>
		///     Initializes a new instance of the <see cref="IoStoreTableOfContents" /> class.
		/// </summary>
		/// <param name="reader">The stream to read from, positioned at the start of the .utoc file.</param>
		/// <param name="layouts">The layout collection holding the <see cref="TocHeaderLayoutName" /> layout.</param>
		/// <exception cref="IoStoreException">Thrown if the file is not a table of contents this reader understands.</exception>
		public IoStoreTableOfContents(IReader reader, StructureLayoutCollection layouts)
		{
			if (layouts == null || !layouts.HasLayout(TocHeaderLayoutName))
			{
				throw new IoStoreException(string.Format(
					"IoStore containers need the \"{0}\" layout, which is missing from this engine definition.",
					TocHeaderLayoutName));
			}

			Load(reader, layouts);
		}

		/// <summary>
		///     Gets the version of the table of contents. Always <see cref="SupportedVersion" />.
		/// </summary>
		public byte Version { get; private set; }

		/// <summary>
		///     Gets the header size the file declares for itself. Read for diagnostics only; the
		///     reader always uses <see cref="HeaderSizeInBytes" />.
		/// </summary>
		public uint DeclaredHeaderSize { get; private set; }

		/// <summary>
		///     Gets the size the file declares for one compressed block entry. Read for diagnostics
		///     only; the reader always uses <see cref="IoCompressedBlock.SizeInBytes" />.
		/// </summary>
		public uint DeclaredCompressedBlockEntrySize { get; private set; }

		/// <summary>
		///     Gets the container's ID.
		/// </summary>
		public ulong ContainerId { get; private set; }

		/// <summary>
		///     Gets the container's feature flags.
		/// </summary>
		public IoContainerFlags Flags { get; private set; }

		/// <summary>
		///     Gets the size in bytes of one window of the container's logical data stream. 65536 in
		///     every container the game ships.
		/// </summary>
		public int CompressionBlockSize { get; private set; }

		/// <summary>
		///     Gets the number of partitions the container's data is split across. Always 1 here.
		/// </summary>
		public uint PartitionCount { get; private set; }

		/// <summary>
		///     Gets the maximum size of one partition.
		/// </summary>
		public ulong PartitionSize { get; private set; }

		/// <summary>
		///     Gets the GUID of the key the container is encrypted with. All zero for Campaign
		///     Evolved, which encrypts nothing.
		/// </summary>
		public byte[] EncryptionKeyGuid { get; private set; }

		/// <summary>
		///     Gets the file offset the directory index starts at.
		/// </summary>
		public long DirectoryIndexOffset { get; private set; }

		/// <summary>
		///     Gets the size of the directory index in bytes, which is zero for containers that
		///     carry none.
		/// </summary>
		public long DirectoryIndexSize { get; private set; }

		/// <summary>
		///     Gets the container's directory index, or <c>null</c> if it carries none.
		/// </summary>
		public IoDirectoryIndex DirectoryIndex { get; private set; }

		/// <summary>
		///     Gets the ID of every chunk in the container, indexed by table-of-contents entry.
		/// </summary>
		public IReadOnlyList<IoChunkId> ChunkIds
		{
			get { return _chunkIds; }
		}

		/// <summary>
		///     Gets the logical offset and length of every chunk, indexed by table-of-contents entry.
		/// </summary>
		public IReadOnlyList<IoChunkLocation> ChunkLocations
		{
			get { return _chunkLocations; }
		}

		/// <summary>
		///     Gets the container's compressed block table, indexed by block number.
		/// </summary>
		public IReadOnlyList<IoCompressedBlock> CompressedBlocks
		{
			get { return _compressedBlocks; }
		}

		/// <summary>
		///     Gets the container's compression method names, in the order they are stored.
		/// </summary>
		/// <remarks>
		///     A block's compression method is a one-based index into this list, so
		///     <c>CompressionMethods[0]</c> is method 1. The literal strings the game uses have
		///     never been written down anywhere, which is why they are surfaced verbatim here
		///     instead of being matched against a hardcoded list: reading one shipped .utoc answers
		///     the question outright.
		/// </remarks>
		public IReadOnlyList<string> CompressionMethods
		{
			get { return _compressionMethods; }
		}

		/// <summary>
		///     Gets the fixed width in bytes of one entry in <see cref="CompressionMethods" />, as the header declares it.
		///     Meaningless when <see cref="CompressionMethods" /> is empty.
		/// </summary>
		public int CompressionMethodNameLength { get; private set; }

		/// <summary>
		///     Gets the number of perfect hash seeds the header declares.
		/// </summary>
		/// <remarks>
		///     How a seed is constructed, or what a reader would do with one, has never been documented anywhere and this
		///     reader does not attempt it - chunks are found by ID or by directory index instead (see
		///     <see cref="FindChunk(IoChunkId)" />). This and <see cref="RawPerfectHashData" /> exist so that a writer which
		///     is not adding or removing chunks - <see cref="IoStoreContainerWriter" /> is the one that exists today - can
		///     replay this section verbatim rather than needing to understand it.
		/// </remarks>
		public int PerfectHashSeedCount { get; private set; }

		/// <summary>
		///     Gets the number of chunks the header declares as having no perfect hash. See <see cref="PerfectHashSeedCount" />.
		/// </summary>
		public int ChunksWithoutPerfectHashCount { get; private set; }

		/// <summary>
		///     Gets the perfect hash seed array and the chunks-without-a-hash array, concatenated, exactly as they appear
		///     on disk between the chunk location array and the compressed block array. See <see cref="PerfectHashSeedCount" />.
		/// </summary>
		public byte[] RawPerfectHashData { get; private set; }

		/// <summary>
		///     Gets whether any of the container's blocks are compressed.
		/// </summary>
		public bool IsCompressed
		{
			get { return (Flags & IoContainerFlags.Compressed) != 0; }
		}

		/// <summary>
		///     Gets whether the container carries signature hashes.
		/// </summary>
		public bool IsSigned
		{
			get { return (Flags & IoContainerFlags.Signed) != 0; }
		}

		/// <summary>
		///     Gets whether the container carries a directory index.
		/// </summary>
		public bool IsIndexed
		{
			get { return (Flags & IoContainerFlags.Indexed) != 0; }
		}

		/// <summary>
		///     Finds the table-of-contents entry a chunk ID belongs to.
		/// </summary>
		/// <param name="chunkId">The chunk ID to look for.</param>
		/// <returns>The chunk's entry index, or -1 if the container does not hold it.</returns>
		/// <remarks>
		///     This is the linear scan of the chunk ID array that the format expects of a reader,
		///     just done once at load time and kept, so callers can look chunks up repeatedly
		///     without rescanning. The container's perfect hash tables are not involved.
		/// </remarks>
		public int FindChunk(IoChunkId chunkId)
		{
			int index;
			if (_entriesByChunkId.TryGetValue(chunkId, out index))
				return index;
			return -1;
		}

		/// <summary>
		///     Finds the table-of-contents entry a cooked file path belongs to.
		/// </summary>
		/// <param name="path">The path to look for. See <see cref="IoDirectoryIndex.TryGetChunkIndex" />.</param>
		/// <returns>The chunk's entry index, or -1 if the path is not indexed.</returns>
		public int FindChunk(string path)
		{
			int index;
			if (DirectoryIndex != null && DirectoryIndex.TryGetChunkIndex(path, out index) && index < _chunkIds.Length)
				return index;
			return -1;
		}

		/// <summary>
		///     Resolves a block's compression method to the name the container gave it.
		/// </summary>
		/// <param name="compressionMethod">The method number taken from a compressed block entry.</param>
		/// <returns>The method's name, or <c>null</c> if the method is 0, meaning the block is stored.</returns>
		/// <exception cref="IoStoreException">Thrown if the method is not in the container's table.</exception>
		public string GetCompressionMethodName(int compressionMethod)
		{
			if (compressionMethod == 0)
				return null;
			if (compressionMethod < 0 || compressionMethod > _compressionMethods.Length)
			{
				throw new IoStoreException(string.Format(
					"A block claims compression method {0}, but the container only names {1}.", compressionMethod,
					_compressionMethods.Length));
			}

			// The method number is one-based so that 0 can mean "stored".
			return _compressionMethods[compressionMethod - 1];
		}

		/// <summary>
		///     Reads the whole table of contents.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="layouts">The layout collection holding the "toc header" layout.</param>
		private void Load(IReader reader, StructureLayoutCollection layouts)
		{
			_baseOffset = reader.Position;

			Endian originalEndianness = reader.Endianness;
			try
			{
				// Every scalar in the header and in the arrays that follow is little-endian, apart
				// from the two records that say otherwise for themselves.
				reader.Endianness = Endian.LittleEndian;

				StructureValueCollection values = StructureReader.ReadStructure(reader,
					layouts.GetLayout(TocHeaderLayoutName));
				int entryCount = LoadHeader(values, reader.Length);
				int blockCount = (int) values.GetInteger("compressed block count");
				var seedCount = (int) values.GetInteger("perfect hash seed count");
				var withoutHashCount = (int) values.GetInteger("chunks without perfect hash count");
				var methodNameCount = (int) values.GetInteger("compression method name count");
				var methodNameLength = (int) values.GetInteger("compression method name length");
				PerfectHashSeedCount = seedCount;
				ChunksWithoutPerfectHashCount = withoutHashCount;
				CompressionMethodNameLength = methodNameLength;

				// Where the header's counts say the directory index has to begin, computed before
				// any array is walked so that the walk can be checked against it afterwards.
				long expectedIndexOffset = _baseOffset + HeaderSizeInBytes +
					(long) entryCount*IoChunkId.SizeInBytes +
					(long) entryCount*IoChunkLocation.SizeInBytes +
					(long) seedCount*sizeof(uint) +
					(long) withoutHashCount*sizeof(uint) +
					(long) blockCount*IoCompressedBlock.SizeInBytes +
					(long) methodNameCount*methodNameLength;
				if (expectedIndexOffset > reader.Length)
				{
					throw new IoStoreException(string.Format(
						"The table of contents header describes 0x{0:X} bytes of arrays, but the file is only " +
						"0x{1:X} bytes long.", expectedIndexOffset - _baseOffset, reader.Length - _baseOffset));
				}

				LoadChunkIds(reader, entryCount);
				LoadChunkLocations(reader, entryCount);
				SkipPerfectHashData(reader, seedCount, withoutHashCount);
				LoadCompressedBlocks(reader, blockCount);
				LoadCompressionMethods(reader, methodNameCount, methodNameLength);

				if (reader.Position != expectedIndexOffset)
				{
					throw new IoStoreException(string.Format(
						"Walking the table of contents arrays ended at 0x{0:X}, but the header's counts put the " +
						"directory index at 0x{1:X}. A record width must be wrong.",
						reader.Position - _baseOffset, expectedIndexOffset - _baseOffset));
				}

				SkipSignatures(reader, blockCount);
				LoadDirectoryIndex(reader);
			}
			finally
			{
				reader.Endianness = originalEndianness;
			}
		}

		/// <summary>
		///     Validates the fixed header and copies its values onto this object.
		/// </summary>
		/// <param name="values">The values read from the "toc header" layout.</param>
		/// <param name="fileLength">The length of the whole file, for bounds checks.</param>
		/// <returns>The number of entries the table of contents holds.</returns>
		private int LoadHeader(StructureValueCollection values, long fileLength)
		{
			byte[] magic = values.GetRaw("magic");
			if (Encoding.ASCII.GetString(magic) != Magic)
			{
				throw new IoStoreException(string.Format(
					"This is not an IoStore table of contents: it starts with \"{0}\" rather than \"{1}\".",
					Encoding.ASCII.GetString(magic).Replace("\0", "\\0"), Magic));
			}

			Version = (byte) values.GetInteger("version");
			if (Version != SupportedVersion)
			{
				throw new IoStoreException(string.Format(
					"This table of contents is version {0}. Only version {1} is supported.", Version,
					SupportedVersion));
			}

			DeclaredHeaderSize = (uint) values.GetInteger("toc header size");
			DeclaredCompressedBlockEntrySize = (uint) values.GetInteger("compressed block entry size");
			ContainerId = values.GetInteger("container id");
			Flags = (IoContainerFlags) values.GetInteger("container flags");
			PartitionCount = (uint) values.GetInteger("partition count");
			PartitionSize = values.GetInteger("partition size");
			EncryptionKeyGuid = values.GetRaw("encryption key guid");

			if ((Flags & IoContainerFlags.Encrypted) != 0)
				throw new IoStoreException("Encrypted IoStore containers are not supported.");

			if (PartitionCount != 1)
			{
				throw new IoStoreException(string.Format(
					"This container is split across {0} partitions. Only single-partition containers are supported.",
					PartitionCount));
			}

			ulong blockSize = values.GetInteger("compression block size");
			if (blockSize == 0 || blockSize > int.MaxValue)
			{
				throw new IoStoreException(string.Format(
					"This container declares a compression block size of {0}, which cannot be used.", blockSize));
			}
			CompressionBlockSize = (int) blockSize;

			ulong entryCount = values.GetInteger("entry count");
			ulong blockCount = values.GetInteger("compressed block count");
			if (entryCount > int.MaxValue || blockCount > int.MaxValue)
			{
				throw new IoStoreException(string.Format(
					"This container declares {0} entries and {1} compressed blocks, which is not believable.",
					entryCount, blockCount));
			}

			ulong methodNameCount = values.GetInteger("compression method name count");
			ulong methodNameLength = values.GetInteger("compression method name length");
			if (methodNameCount > 0 && methodNameLength == 0)
				throw new IoStoreException("This container names compression methods but gives them no length.");
			if (methodNameCount > int.MaxValue || methodNameLength > int.MaxValue)
				throw new IoStoreException("This container's compression method name table is not believable.");

			ulong directoryIndexSize = values.GetInteger("directory index size");
			if (directoryIndexSize > (ulong) fileLength)
			{
				throw new IoStoreException(string.Format(
					"This container declares a 0x{0:X} byte directory index, which is larger than the whole " +
					"0x{1:X} byte file.", directoryIndexSize, fileLength));
			}
			DirectoryIndexSize = (long) directoryIndexSize;

			return (int) entryCount;
		}

		/// <summary>
		///     Reads the chunk ID array and indexes it for lookup.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="entryCount">The number of entries to read.</param>
		private void LoadChunkIds(IReader reader, int entryCount)
		{
			_chunkIds = new IoChunkId[entryCount];
			for (int i = 0; i < entryCount; i++)
			{
				_chunkIds[i] = IoChunkId.Read(reader);

				// Duplicate IDs would mean the container addresses the same chunk twice; keep the
				// first, which is what a linear scan would have found.
				if (!_entriesByChunkId.ContainsKey(_chunkIds[i]))
					_entriesByChunkId.Add(_chunkIds[i], i);
			}
		}

		/// <summary>
		///     Reads the chunk offset and length array.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="entryCount">The number of entries to read.</param>
		private void LoadChunkLocations(IReader reader, int entryCount)
		{
			_chunkLocations = new IoChunkLocation[entryCount];
			for (int i = 0; i < entryCount; i++)
				_chunkLocations[i] = IoChunkLocation.Read(reader);
		}

		/// <summary>
		///     Captures both perfect hash arrays verbatim, without interpreting them.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="seedCount">The number of hash seeds to capture.</param>
		/// <param name="withoutHashCount">The number of chunks-without-hash entries to capture.</param>
		/// <remarks>
		///     How the seeds are constructed is not documented anywhere and a reader needs none of
		///     it, so these are kept by their declared counts and never interpreted. The counts
		///     are not assumed to be zero: even a two-entry container writes two seeds. Captured as
		///     raw bytes, in <see cref="RawPerfectHashData" />, rather than as parsed arrays, both
		///     because there is nothing to parse them into and so that <see cref="IoStoreContainerWriter" />
		///     can replay them exactly rather than re-deriving them.
		/// </remarks>
		private void SkipPerfectHashData(IReader reader, int seedCount, int withoutHashCount)
		{
			RawPerfectHashData = reader.ReadBlock((int) ((long) seedCount*sizeof(uint) + (long) withoutHashCount*sizeof(uint)));
		}

		/// <summary>
		///     Reads the compressed block table.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="blockCount">The number of blocks to read.</param>
		private void LoadCompressedBlocks(IReader reader, int blockCount)
		{
			_compressedBlocks = new IoCompressedBlock[blockCount];
			for (int i = 0; i < blockCount; i++)
			{
				_compressedBlocks[i] = IoCompressedBlock.Read(reader);
				if (_compressedBlocks[i].UncompressedSize > CompressionBlockSize)
				{
					throw new IoStoreException(string.Format(
						"Compressed block {0} expands to 0x{1:X} bytes, which overflows the container's 0x{2:X} " +
						"byte block size.", i, _compressedBlocks[i].UncompressedSize, CompressionBlockSize));
				}
			}
		}

		/// <summary>
		///     Reads the compression method name table.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="count">The number of names to read.</param>
		/// <param name="length">The fixed width of each name in bytes.</param>
		/// <remarks>
		///     Names are fixed-width, null-padded ASCII rather than length-prefixed, so each one is
		///     read up to its terminator and the cursor is then placed at the end of the field.
		/// </remarks>
		private void LoadCompressionMethods(IReader reader, int count, int length)
		{
			_compressionMethods = new string[count];
			for (int i = 0; i < count; i++)
			{
				long start = reader.Position;
				_compressionMethods[i] = reader.ReadAscii(length);
				reader.SeekTo(start + length);
			}
		}

		/// <summary>
		///     Steps over the signature block, if the container has one.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="blockCount">The number of compressed blocks, which is also the number of block hashes.</param>
		/// <remarks>
		///     Campaign Evolved signs nothing, so this path is untested against a real file. It is
		///     implemented because the flag exists and skipping the block wrongly would silently
		///     move the directory index.
		/// </remarks>
		private void SkipSignatures(IReader reader, int blockCount)
		{
			if (!IsSigned)
				return;

			int hashSize = reader.ReadInt32();
			if (hashSize < 0)
				throw new IoStoreException(string.Format("This container declares a signature size of {0}.", hashSize));

			// A table of contents signature, a block signature, and then one SHA-1 hash per block.
			reader.Skip((long) hashSize*2);
			reader.Skip((long) blockCount*20);
		}

		/// <summary>
		///     Reads the directory index, if the container has one.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		private void LoadDirectoryIndex(IReader reader)
		{
			DirectoryIndexOffset = reader.Position;
			if (DirectoryIndexSize == 0)
			{
				// Override containers carry no index at all and are addressed purely by chunk ID.
				DirectoryIndex = null;
				return;
			}

			DirectoryIndex = IoDirectoryIndex.Read(reader, DirectoryIndexSize);
		}
	}
}
