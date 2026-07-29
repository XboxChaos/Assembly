namespace Blamite.IO.IoStore
{
	/// <summary>
	///     One fixed-size window of a container's logical data stream, and where its bytes
	///     physically live in the .ucas file.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Block <c>n</c> always covers logical offsets
	///         <c>n * CompressionBlockSize</c> through <c>(n + 1) * CompressionBlockSize</c>, so a
	///         chunk's block range follows straight from its <see cref="IoChunkLocation" /> by
	///         division. Only the physical side of the mapping is stored.
	///     </para>
	///     <para>
	///         Stored as 12 bytes, bit-packed into a little-endian uint64 followed by a
	///         little-endian uint32:
	///     </para>
	///     <code>
	///     packedOffsetAndSize:  bits  0..40  physical offset into the .ucas
	///                           bits 40..64  compressed size
	///     packedSizeAndMethod:  bits  0..24  uncompressed size
	///                           bits 24..32  compression method
	///     </code>
	///     <para>
	///         The layout XML dialect cannot describe bit fields, so this record is decoded in code.
	///     </para>
	/// </remarks>
	public struct IoCompressedBlock
	{
		/// <summary>
		///     The size of a compressed block entry in bytes as stored in a .utoc file.
		/// </summary>
		public const int SizeInBytes = 12;

		// Field widths of the packed representation.
		private const ulong OffsetMask = (1UL << 40) - 1;
		private const int CompressedSizeShift = 40;
		private const uint SizeMask = 0xFFFFFF;
		private const int MethodShift = 24;

		/// <summary>
		///     Initializes a new instance of the <see cref="IoCompressedBlock" /> struct.
		/// </summary>
		/// <param name="offset">The physical offset of the block's bytes in the .ucas file.</param>
		/// <param name="compressedSize">The number of bytes the block occupies in the .ucas file.</param>
		/// <param name="uncompressedSize">The number of bytes the block expands to.</param>
		/// <param name="compressionMethod">The block's compression method, where 0 means stored.</param>
		public IoCompressedBlock(long offset, int compressedSize, int uncompressedSize, int compressionMethod)
		{
			Offset = offset;
			CompressedSize = compressedSize;
			UncompressedSize = uncompressedSize;
			CompressionMethod = compressionMethod;
		}

		/// <summary>
		///     Gets the physical offset of the block's bytes in the .ucas file.
		/// </summary>
		public long Offset { get; private set; }

		/// <summary>
		///     Gets the number of bytes the block occupies in the .ucas file.
		/// </summary>
		public int CompressedSize { get; private set; }

		/// <summary>
		///     Gets the number of bytes the block expands to once decompressed.
		/// </summary>
		public int UncompressedSize { get; private set; }

		/// <summary>
		///     Gets the block's compression method. 0 means the block is stored verbatim; any other
		///     value is a one-based index into the container's compression method name table.
		/// </summary>
		public int CompressionMethod { get; private set; }

		/// <summary>
		///     Gets whether the block is stored verbatim and needs no decompressor at all.
		/// </summary>
		/// <remarks>
		///     Stored and compressed blocks may be mixed freely within one container, so this is
		///     decided per block and never per container.
		/// </remarks>
		public bool IsStored
		{
			get { return CompressionMethod == 0; }
		}

		/// <summary>
		///     Reads a compressed block entry from a stream.
		/// </summary>
		/// <param name="reader">The stream to read from, positioned at the start of the entry.</param>
		/// <returns>The block entry that was read.</returns>
		public static IoCompressedBlock Read(IReader reader)
		{
			Endian originalEndianness = reader.Endianness;
			try
			{
				reader.Endianness = Endian.LittleEndian;
				ulong packedOffsetAndSize = reader.ReadUInt64();
				uint packedSizeAndMethod = reader.ReadUInt32();

				long offset = (long) (packedOffsetAndSize & OffsetMask);
				var compressedSize = (int) ((packedOffsetAndSize >> CompressedSizeShift) & SizeMask);
				var uncompressedSize = (int) (packedSizeAndMethod & SizeMask);
				var method = (int) (packedSizeAndMethod >> MethodShift);

				return new IoCompressedBlock(offset, compressedSize, uncompressedSize, method);
			}
			finally
			{
				reader.Endianness = originalEndianness;
			}
		}

		/// <summary>
		///     Builds a human-readable description of the block.
		/// </summary>
		/// <returns>The block as a string.</returns>
		public override string ToString()
		{
			return string.Format("0x{0:X} + 0x{1:X} -> 0x{2:X} bytes, method {3}", Offset, CompressedSize,
				UncompressedSize, CompressionMethod);
		}
	}
}
