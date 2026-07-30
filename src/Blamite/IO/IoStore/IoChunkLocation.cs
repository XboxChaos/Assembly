namespace Blamite.IO.IoStore
{
	/// <summary>
	///     Where a chunk lives within a container's logical, decompressed data stream.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Stored as 10 bytes holding two 40-bit BIG-endian integers back to back: the offset in
	///         bytes 0x0-0x5 and the length in bytes 0x5-0xA. Note that the pair is big-endian even
	///         though the .utoc header around it is little-endian.
	///     </para>
	///     <para>
	///         <see cref="Offset" /> is <em>not</em> a file offset. It addresses the container's
	///         logical decompressed stream, and that stream is quantized to the container's
	///         compression block size, so a chunk's logical offset can easily be larger than the
	///         entire .ucas file. Reaching a chunk's bytes always means going through the
	///         compressed block table; see <see cref="IoStoreContainer.ReadChunk(int)" />.
	///     </para>
	/// </remarks>
	public struct IoChunkLocation
	{
		/// <summary>
		///     The size of an offset/length pair in bytes as stored in a .utoc file.
		/// </summary>
		public const int SizeInBytes = 10;

		/// <summary>
		///     Initializes a new instance of the <see cref="IoChunkLocation" /> struct.
		/// </summary>
		/// <param name="offset">The offset of the chunk within the container's logical data stream.</param>
		/// <param name="length">The length of the chunk in bytes.</param>
		public IoChunkLocation(long offset, long length)
		{
			Offset = offset;
			Length = length;
		}

		/// <summary>
		///     Gets the offset of the chunk within the container's logical, decompressed data stream.
		/// </summary>
		public long Offset { get; private set; }

		/// <summary>
		///     Gets the length of the chunk in bytes.
		/// </summary>
		public long Length { get; private set; }

		/// <summary>
		///     Reads an offset/length pair from a stream.
		/// </summary>
		/// <param name="reader">The stream to read from, positioned at the start of the pair.</param>
		/// <returns>The location that was read.</returns>
		public static IoChunkLocation Read(IReader reader)
		{
			Endian originalEndianness = reader.Endianness;
			try
			{
				reader.Endianness = Endian.BigEndian;
				long offset = ReadUInt40(reader);
				long length = ReadUInt40(reader);
				return new IoChunkLocation(offset, length);
			}
			finally
			{
				reader.Endianness = originalEndianness;
			}
		}

		/// <summary>
		///     Builds a human-readable description of the location.
		/// </summary>
		/// <returns>The location as a string.</returns>
		public override string ToString()
		{
			return string.Format("logical 0x{0:X}, length 0x{1:X}", Offset, Length);
		}

		/// <summary>
		///     Writes this offset/length pair to a stream.
		/// </summary>
		/// <param name="writer">The stream to write to, positioned at the start of the pair.</param>
		/// <exception cref="IoStoreException">Thrown if <see cref="Offset" /> or <see cref="Length" /> does not fit in 40 bits.</exception>
		public void Write(IWriter writer)
		{
			Endian originalEndianness = writer.Endianness;
			try
			{
				writer.Endianness = Endian.BigEndian;
				WriteUInt40(writer, Offset);
				WriteUInt40(writer, Length);
			}
			finally
			{
				writer.Endianness = originalEndianness;
			}
		}

		/// <summary>
		///     Reads a 40-bit big-endian integer.
		/// </summary>
		/// <param name="reader">The stream to read from, already set to big-endian.</param>
		/// <returns>The value that was read.</returns>
		private static long ReadUInt40(IReader reader)
		{
			// There is no five-byte read on IReader, so the value is assembled from its
			// most-significant byte followed by a big-endian uint32. The result always fits in a
			// long: 40 bits is at most 1 TiB.
			long high = reader.ReadByte();
			uint low = reader.ReadUInt32();
			return (high << 32) | low;
		}

		/// <summary>
		///     Writes a 40-bit big-endian integer, the counterpart of <see cref="ReadUInt40" />.
		/// </summary>
		/// <param name="writer">The stream to write to, already set to big-endian.</param>
		/// <param name="value">The value to write. Must fit in 40 bits.</param>
		private static void WriteUInt40(IWriter writer, long value)
		{
			if (value < 0 || value > 0xFFFFFFFFFFL)
			{
				throw new IoStoreException(string.Format(
					"0x{0:X} does not fit in the 40 bits an IoChunkLocation field has to hold it in.", value));
			}
			writer.WriteByte((byte) (value >> 32));
			writer.WriteUInt32((uint) value);
		}
	}
}
