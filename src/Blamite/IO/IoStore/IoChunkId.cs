using System;

namespace Blamite.IO.IoStore
{
	/// <summary>
	///     Identifies one chunk of data inside an IoStore container.
	/// </summary>
	/// <remarks>
	///     <para>
	///         A chunk ID is 12 bytes on disk and is the one structure in the container format that
	///         mixes byte orders inside a single record:
	///     </para>
	///     <code>
	///     [0x0 .. 0x8)  uint64  package ID    LITTLE-endian
	///     [0x8 .. 0xA)  uint16  chunk index   BIG-endian     (0x8 is the high byte, 0x9 the low byte)
	///     [0xA]         byte    padding, always zero
	///     [0xB]         byte    chunk type    (see IoChunkType)
	///     </code>
	///     <para>
	///         The big-endian chunk index is the easiest thing in the whole format to get wrong,
	///         and getting it wrong does not fail: it silently yields byte-swapped indices, so
	///         chunk 1 reads as 256 and chunk 0 still reads as 0. That is why the read below flips
	///         <see cref="IBaseStream.Endianness" /> explicitly for those two bytes instead of
	///         reading the record as one endian-consistent blob.
	///     </para>
	///     <para>
	///         The two chunks making up one Halo tag share bytes 0x0-0xA and differ only in the
	///         type byte, so <see cref="WithType" /> is how the .uasset/.ubulk pairing is walked.
	///     </para>
	/// </remarks>
	public struct IoChunkId : IEquatable<IoChunkId>
	{
		/// <summary>
		///     The size of a chunk ID in bytes as stored in a .utoc file.
		/// </summary>
		public const int SizeInBytes = 12;

		/// <summary>
		///     Initializes a new instance of the <see cref="IoChunkId" /> struct.
		/// </summary>
		/// <param name="packageId">The ID of the package that the chunk belongs to.</param>
		/// <param name="chunkIndex">The index of the chunk within its package.</param>
		/// <param name="type">The raw type byte of the chunk.</param>
		public IoChunkId(ulong packageId, ushort chunkIndex, byte type)
		{
			PackageId = packageId;
			ChunkIndex = chunkIndex;
			RawType = type;
		}

		/// <summary>
		///     Gets the ID of the package that the chunk belongs to.
		/// </summary>
		public ulong PackageId { get; private set; }

		/// <summary>
		///     Gets the index of the chunk within its package.
		/// </summary>
		public ushort ChunkIndex { get; private set; }

		/// <summary>
		///     Gets the raw type byte of the chunk, including values which have no name yet.
		/// </summary>
		public byte RawType { get; private set; }

		/// <summary>
		///     Gets the type of the chunk. Values outside <see cref="IoChunkType" /> are returned as-is.
		/// </summary>
		public IoChunkType Type
		{
			get { return (IoChunkType) RawType; }
		}

		/// <summary>
		///     Reads a chunk ID from a stream, honoring the mixed endianness described on this type.
		/// </summary>
		/// <param name="reader">The stream to read from, positioned at the start of the chunk ID.</param>
		/// <returns>The chunk ID that was read.</returns>
		public static IoChunkId Read(IReader reader)
		{
			Endian originalEndianness = reader.Endianness;
			try
			{
				// The package ID is little-endian, like every other integer in the .utoc header.
				reader.Endianness = Endian.LittleEndian;
				ulong packageId = reader.ReadUInt64();

				// The chunk index that immediately follows it is big-endian. This is deliberate in
				// the format - the bytes are laid out so that a memcmp of the first ten bytes
				// orders chunks by package and then by index - and it is not a mistake here.
				reader.Endianness = Endian.BigEndian;
				ushort chunkIndex = reader.ReadUInt16();

				// Byte 0xA is padding and has only ever been observed as zero; it is skipped rather
				// than validated, because nothing is known to depend on it.
				reader.Skip(1);
				byte type = reader.ReadByte();

				return new IoChunkId(packageId, chunkIndex, type);
			}
			finally
			{
				reader.Endianness = originalEndianness;
			}
		}

		/// <summary>
		///     Produces the ID of the sibling chunk of a given type in the same package.
		/// </summary>
		/// <param name="type">The type of the sibling chunk.</param>
		/// <returns>A chunk ID identical to this one apart from its type.</returns>
		public IoChunkId WithType(IoChunkType type)
		{
			return new IoChunkId(PackageId, ChunkIndex, (byte) type);
		}

		/// <summary>
		///     Determines whether this chunk ID is equal to another one.
		/// </summary>
		/// <param name="other">The chunk ID to compare against.</param>
		/// <returns><c>true</c> if the two chunk IDs identify the same chunk.</returns>
		public bool Equals(IoChunkId other)
		{
			return PackageId == other.PackageId && ChunkIndex == other.ChunkIndex && RawType == other.RawType;
		}

		/// <summary>
		///     Determines whether this chunk ID is equal to another object.
		/// </summary>
		/// <param name="obj">The object to compare against.</param>
		/// <returns><c>true</c> if the object is an identical chunk ID.</returns>
		public override bool Equals(object obj)
		{
			return (obj is IoChunkId) && Equals((IoChunkId) obj);
		}

		/// <summary>
		///     Computes a hash code for the chunk ID.
		/// </summary>
		/// <returns>The chunk ID's hash code.</returns>
		public override int GetHashCode()
		{
			int hash = PackageId.GetHashCode();
			hash = (hash*397) ^ ChunkIndex;
			hash = (hash*397) ^ RawType;
			return hash;
		}

		/// <summary>
		///     Builds a human-readable description of the chunk ID.
		/// </summary>
		/// <returns>The chunk ID as a string.</returns>
		public override string ToString()
		{
			return string.Format("{0:X16}:{1}:{2}", PackageId, ChunkIndex, Type);
		}

		/// <summary>
		///     Determines whether two chunk IDs identify the same chunk.
		/// </summary>
		/// <param name="a">The first chunk ID.</param>
		/// <param name="b">The second chunk ID.</param>
		/// <returns><c>true</c> if the two chunk IDs are equal.</returns>
		public static bool operator ==(IoChunkId a, IoChunkId b)
		{
			return a.Equals(b);
		}

		/// <summary>
		///     Determines whether two chunk IDs identify different chunks.
		/// </summary>
		/// <param name="a">The first chunk ID.</param>
		/// <param name="b">The second chunk ID.</param>
		/// <returns><c>true</c> if the two chunk IDs are not equal.</returns>
		public static bool operator !=(IoChunkId a, IoChunkId b)
		{
			return !a.Equals(b);
		}
	}
}
