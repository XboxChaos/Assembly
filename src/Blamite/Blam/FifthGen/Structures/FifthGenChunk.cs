using System;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     One node of the universal chunk tree used throughout a fifth-generation tag payload.
	/// </summary>
	/// <remarks>
	///     Every chunk at every level of the payload - <c>tag!</c>, <c>blay</c>, <c>tgly</c>, the definition tables,
	///     <c>bdat</c> and every nested data section - shares this twelve byte shape:
	///     <code>
	///     +0x00  u32  four-CC magic
	///     +0x04  u32  version
	///     +0x08  u32  size, EXCLUDING these twelve bytes
	///     +0x0C  content[size]
	///     </code>
	///     There is no alignment padding between sibling chunks, so a chunk whose size is not a multiple of four is followed
	///     immediately by the next chunk. The layout's string blob relies on that.
	/// </remarks>
	public class FifthGenChunk
	{
		/// <summary>
		///     The size in bytes of a chunk header. The <see cref="Size" /> field does not include it.
		/// </summary>
		public const int HeaderSize = 0xC;

		private FifthGenChunk(int magic, uint version, uint size, long headerOffset)
		{
			Magic = magic;
			Version = version;
			Size = size;
			HeaderOffset = headerOffset;
		}

		/// <summary>
		///     Gets the chunk's four-CC magic number, packed the same way <see cref="CharConstant.FromString" /> packs it.
		/// </summary>
		public int Magic { get; private set; }

		/// <summary>
		///     Gets the chunk's four-CC magic number as a printable string.
		/// </summary>
		public string MagicString
		{
			get { return MagicToString(Magic); }
		}

		/// <summary>
		///     Gets the chunk's version number.
		/// </summary>
		public uint Version { get; private set; }

		/// <summary>
		///     Gets the size of the chunk's content in bytes. This excludes the twelve byte header.
		/// </summary>
		public uint Size { get; private set; }

		/// <summary>
		///     Gets the offset of the chunk's header.
		/// </summary>
		public long HeaderOffset { get; private set; }

		/// <summary>
		///     Gets the offset of the first byte of the chunk's content.
		/// </summary>
		public long ContentOffset
		{
			get { return HeaderOffset + HeaderSize; }
		}

		/// <summary>
		///     Gets the offset one past the last byte of the chunk's content.
		/// </summary>
		public long ContentEnd
		{
			get { return ContentOffset + Size; }
		}

		/// <summary>
		///     Reads a four-CC magic number from a stream, undoing the byte reversal the format stores it with.
		/// </summary>
		/// <param name="reader">The stream to read from. Its endianness must already be set to the payload's endianness.</param>
		/// <returns>The magic number, packed to match <see cref="CharConstant.FromString" />.</returns>
		/// <remarks>
		///     Four-CCs are stored reversed with respect to how they are spelled: a little-endian payload writes <c>blay</c> as
		///     <c>79 61 6C 62</c>. That is the same reversal the file header signature advertises - <c>BLAM</c> in a big-endian
		///     payload, <c>MALB</c> in a little-endian one - so reading the dword with the payload's own endianness undoes it in
		///     both cases and yields the magic in spelled order. Every magic in the format goes through this one method so that
		///     the assumption lives in a single place: the only payloads anyone has measured are little-endian, and if a
		///     big-endian payload ever turns up spelling its magics reversed as well, this is the only method that changes.
		/// </remarks>
		public static int ReadMagic(IReader reader)
		{
			return (int) reader.ReadUInt32();
		}

		/// <summary>
		///     Converts a four-CC magic number into a printable string.
		/// </summary>
		/// <param name="magic">The magic number to convert.</param>
		/// <returns>The string the magic number spells, with any unprintable byte shown as a period.</returns>
		/// <remarks>
		///     <see cref="CharConstant.ToString" /> does the packing, but it stops at the first zero byte and so cannot render the
		///     pageable resource section magic, which contains an embedded NUL. Magics are always compared as integers; this is
		///     for diagnostics only.
		/// </remarks>
		public static string MagicToString(int magic)
		{
			string packed = CharConstant.ToString(magic);
			if (packed.Length == 4)
			{
				var sanitized = new char[4];
				for (var i = 0; i < 4; i++)
					sanitized[i] = (packed[i] >= ' ' && packed[i] < 0x7F) ? packed[i] : '.';
				return new string(sanitized);
			}

			// A magic with a leading or embedded zero byte, or with the high bit set. Rebuild it byte by byte.
			var chars = new char[4];
			for (var i = 0; i < 4; i++)
			{
				var ch = (char) ((magic >> (8*(3 - i))) & 0xFF);
				chars[i] = (ch >= ' ' && ch < 0x7F) ? ch : '.';
			}
			return new string(chars);
		}

		/// <summary>
		///     Reads a chunk header from a stream and validates that its content fits inside the enclosing chunk.
		/// </summary>
		/// <param name="reader">The stream to read from, positioned at the chunk header.</param>
		/// <param name="limit">The offset one past the last byte the chunk is allowed to occupy.</param>
		/// <returns>The chunk that was read. The stream is left positioned at the start of its content.</returns>
		public static FifthGenChunk Read(IReader reader, long limit)
		{
			return Read(reader, limit, null);
		}

		/// <summary>
		///     Reads a chunk header from a stream and validates that its content fits inside the enclosing chunk.
		/// </summary>
		/// <param name="reader">The stream to read from, positioned at the chunk header.</param>
		/// <param name="limit">The offset one past the last byte the chunk is allowed to occupy.</param>
		/// <param name="context">A description of what is being read, named in any error message. Can be null.</param>
		/// <returns>The chunk that was read. The stream is left positioned at the start of its content.</returns>
		public static FifthGenChunk Read(IReader reader, long limit, string context)
		{
			string of = (context != null) ? $" for {context}" : string.Empty;

			long offset = reader.Position;
			if (offset + HeaderSize > limit)
			{
				throw new FifthGenFormatException(
					$"Truncated chunk header at 0x{offset:X}{of}: {limit - offset} byte(s) remain before the end of the enclosing chunk at 0x{limit:X} but a header needs {HeaderSize}.");
			}

			int magic = ReadMagic(reader);
			uint version = reader.ReadUInt32();
			uint size = reader.ReadUInt32();
			var result = new FifthGenChunk(magic, version, size, offset);

			if (result.ContentEnd > limit || result.ContentEnd < result.ContentOffset)
			{
				throw new FifthGenFormatException(
					$"'{result.MagicString}' chunk at 0x{offset:X}{of} declares {size} byte(s) of content, which overruns its container by {result.ContentEnd - limit} byte(s).");
			}

			return result;
		}

		/// <summary>
		///     Reads a chunk header and requires it to carry a particular magic number.
		/// </summary>
		/// <param name="reader">The stream to read from, positioned at the chunk header.</param>
		/// <param name="limit">The offset one past the last byte the chunk is allowed to occupy.</param>
		/// <param name="expectedMagic">The magic number the chunk must carry.</param>
		/// <param name="context">A description of what is being read, used in the error message.</param>
		/// <returns>The chunk that was read.</returns>
		public static FifthGenChunk ReadExpecting(IReader reader, long limit, int expectedMagic, string context)
		{
			FifthGenChunk result = Read(reader, limit, context);
			if (result.Magic != expectedMagic)
			{
				throw new FifthGenFormatException(
					$"Expected a '{MagicToString(expectedMagic)}' chunk for {context} at 0x{result.HeaderOffset:X} but found '{result.MagicString}'.");
			}
			return result;
		}

		/// <summary>
		///     Verifies that a walk of this chunk's content stopped exactly at its end.
		/// </summary>
		/// <param name="reader">The stream whose position should be checked.</param>
		/// <remarks>
		///     A parent chunk advances by a child's declared size, so bytes left unread inside a nested wrapper are invisible to
		///     any whole-payload byte count. Checking every wrapper individually is the only way to notice them.
		/// </remarks>
		public void EnsureConsumed(IReader reader)
		{
			if (reader.Position == ContentEnd)
				return;

			long difference = ContentEnd - reader.Position;
			string direction = (difference > 0) ? "unread" : "overread";
			throw new FifthGenFormatException(
				$"'{MagicString}' chunk at 0x{HeaderOffset:X} was not consumed exactly: its {Size} byte(s) of content end at 0x{ContentEnd:X} but the walk stopped at 0x{reader.Position:X}, leaving {Math.Abs(difference)} byte(s) {direction}.");
		}

		/// <summary>
		///     Reads the whole of this chunk's content as raw bytes, leaving the stream at the end of the chunk.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <returns>The chunk's content.</returns>
		public byte[] ReadContent(IReader reader)
		{
			reader.SeekTo(ContentOffset);
			return reader.ReadBlock((int) Size);
		}
	}
}
