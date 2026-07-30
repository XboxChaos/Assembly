using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     The sixty-four byte header at the front of a fifth-generation tag payload.
	/// </summary>
	/// <remarks>
	///     Blamite normally describes a fixed-size header in Layout XML and reads it with
	///     <see cref="Blamite.Serialization.StructureReader" />. That is not possible here: the endianness of every field in
	///     this header - including its own group four-CC - is only known once the signature at
	///     <see cref="SignatureOffset" /> has been inspected, and a payload parser has no
	///     <see cref="Blamite.Serialization.EngineDescription" /> to fetch a layout from in the first place. The offsets are
	///     therefore stated here, in one place, rather than in a layout file the loader could not reach.
	/// </remarks>
	public class FifthGenTagHeader
	{
		/// <summary>
		///     The size in bytes of the tag payload header. The tag body's <c>tag!</c> chunk header follows it immediately, which
		///     puts the body's content at 0x4C.
		/// </summary>
		public const int Size = 0x40;

		/// <summary>
		///     The offset of the signature dword, which doubles as the payload's endianness marker.
		/// </summary>
		public const int SignatureOffset = 0x3C;

		/// <summary>
		///     The number of leading bytes which are zero in every payload anyone has looked at, and which are preserved verbatim
		///     rather than interpreted.
		/// </summary>
		public const int PaddingSize = 0x24;

		private FifthGenTagHeader()
		{
		}

		/// <summary>
		///     Gets the leading padding bytes, preserved verbatim because nothing is known to live in them.
		/// </summary>
		public byte[] Padding { get; private set; }

		/// <summary>
		///     Gets the engine build version the tag was compiled against.
		/// </summary>
		public int BuildVersion { get; private set; }

		/// <summary>
		///     Gets the engine build number the tag was compiled against.
		/// </summary>
		public int BuildNumber { get; private set; }

		/// <summary>
		///     Gets the tag payload format version.
		/// </summary>
		public uint FormatVersion { get; private set; }

		/// <summary>
		///     Gets the tag group's four-CC magic number. This is the authoritative statement of the tag's group; the group suffix
		///     on a cooked filename is only a convenience.
		/// </summary>
		public int GroupMagic { get; private set; }

		/// <summary>
		///     Gets the tag group's four-CC as a string, e.g. <c>weap</c>.
		/// </summary>
		public string GroupTag
		{
			get { return FifthGenChunk.MagicToString(GroupMagic); }
		}

		/// <summary>
		///     Gets the tag group's version number.
		/// </summary>
		public uint GroupVersion { get; private set; }

		/// <summary>
		///     Gets the header checksum. The byte span it covers has never been established and shipped payloads carry zero, so it
		///     is preserved but never verified.
		/// </summary>
		public uint Checksum { get; private set; }

		/// <summary>
		///     Gets the payload signature. Once <see cref="Endianness" /> has been applied this always spells <c>BLAM</c>.
		/// </summary>
		public int Signature { get; private set; }

		/// <summary>
		///     Gets the endianness the payload is stored in, as determined by the signature.
		/// </summary>
		public Endian Endianness { get; private set; }

		/// <summary>
		///     Determines whether a buffer looks like a Blam tag payload.
		/// </summary>
		/// <param name="payload">The decompressed buffer to test.</param>
		/// <returns><c>true</c> if the buffer is at least sixty-four bytes long and carries a tag signature.</returns>
		/// <remarks>
		///     Tag payloads are cooked into <c>.ubulk</c> files, which Unreal also uses for texture mips and streamed audio. The
		///     signature is the only reliable way to tell them apart.
		/// </remarks>
		public static bool IsTagPayload(byte[] payload)
		{
			if (payload == null || payload.Length < Size)
				return false;

			Endian ignored;
			return TryGetEndianness(payload[SignatureOffset], payload[SignatureOffset + 1], payload[SignatureOffset + 2],
				payload[SignatureOffset + 3], out ignored);
		}

		/// <summary>
		///     Determines whether a stream looks like a Blam tag payload, starting from its current position.
		/// </summary>
		/// <param name="reader">The stream to test. Its position is restored before returning.</param>
		/// <returns><c>true</c> if a tag signature is present.</returns>
		public static bool IsTagPayload(IReader reader)
		{
			Endian ignored;
			return TryDetectEndianness(reader, out ignored);
		}

		/// <summary>
		///     Reads a tag payload header.
		/// </summary>
		/// <param name="reader">The stream to read from, positioned at the start of the payload.</param>
		/// <returns>The header that was read. The stream's endianness is left set to the payload's endianness.</returns>
		public static FifthGenTagHeader Read(IReader reader)
		{
			long baseOffset = reader.Position;

			Endian endianness;
			if (!TryDetectEndianness(reader, out endianness))
			{
				throw new FifthGenFormatException(
					$"No Blam tag signature at 0x{baseOffset + SignatureOffset:X}. This is not a tag payload.");
			}

			// Everything from here on - including the group four-CC and every chunk magic in the file - is read with
			// the endianness the signature just reported, rather than an assumed byte order.
			reader.Endianness = endianness;
			reader.SeekTo(baseOffset);

			var result = new FifthGenTagHeader();
			result.Endianness = endianness;
			result.Padding = reader.ReadBlock(PaddingSize);
			result.BuildVersion = reader.ReadInt32();
			result.BuildNumber = reader.ReadInt32();
			result.FormatVersion = reader.ReadUInt32();
			result.GroupMagic = FifthGenChunk.ReadMagic(reader);
			result.GroupVersion = reader.ReadUInt32();
			result.Checksum = reader.ReadUInt32();
			result.Signature = FifthGenChunk.ReadMagic(reader);
			result.Validate();
			return result;
		}

		/// <summary>
		///     Determines the endianness of a payload from its signature.
		/// </summary>
		/// <param name="reader">The stream to inspect, positioned at the start of the payload. Its position is restored.</param>
		/// <param name="endianness">On return, the payload's endianness.</param>
		/// <returns><c>true</c> if a signature was found.</returns>
		private static bool TryDetectEndianness(IReader reader, out Endian endianness)
		{
			endianness = Endian.LittleEndian;

			long baseOffset = reader.Position;
			if (baseOffset < 0 || baseOffset + Size > reader.Length)
				return false;

			reader.SeekTo(baseOffset + SignatureOffset);
			byte[] signature = reader.ReadBlock(4);
			reader.SeekTo(baseOffset);

			return TryGetEndianness(signature[0], signature[1], signature[2], signature[3], out endianness);
		}

		private static bool TryGetEndianness(byte b0, byte b1, byte b2, byte b3, out Endian endianness)
		{
			// 'BLAM' spelled forwards is a big-endian payload; the same dword byte-reversed to 'MALB' is a
			// little-endian one. Comparing raw bytes avoids having to already know the answer in order to read it.
			if (b0 == 'B' && b1 == 'L' && b2 == 'A' && b3 == 'M')
			{
				endianness = Endian.BigEndian;
				return true;
			}
			if (b0 == 'M' && b1 == 'A' && b2 == 'L' && b3 == 'B')
			{
				endianness = Endian.LittleEndian;
				return true;
			}

			endianness = Endian.LittleEndian;
			return false;
		}

		private void Validate()
		{
			int expected = CharConstant.FromString("BLAM");
			if (Signature != expected)
			{
				throw new FifthGenFormatException(
					$"Tag payload signature is 0x{Signature:X8}, expected 0x{expected:X8} once endianness was applied.");
			}
		}
	}
}
