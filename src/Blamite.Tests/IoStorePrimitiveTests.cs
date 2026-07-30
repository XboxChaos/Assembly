using Blamite.Blam.FifthGen.Structures;
using Blamite.IO;
using Blamite.IO.IoStore;

namespace Blamite.Tests
{
	/// <summary>
	///     Pins the byte-level rules of the Campaign Evolved container and payload formats against
	///     hand-built buffers.
	/// </summary>
	/// <remarks>
	///     These are the facts about the format that were most expensive to establish and are
	///     easiest to silently undo: a mixed-endian identifier, a pair of packed bit-fields, and a
	///     four-CC stored backwards. None of them depends on a real container, so unlike
	///     <see cref="CampaignEvolvedTests" /> this whole class runs everywhere, including on a
	///     machine that has never seen the game. Each buffer below is written out byte by byte with
	///     the field boundaries called out, so that a failure says which rule broke rather than
	///     just which number differs.
	/// </remarks>
	public class IoStorePrimitiveTests
	{
		private static IReader ReaderOver(params byte[] bytes)
		{
			// Deliberately big-endian: every one of these readers overrides the endianness it
			// needs locally and is required to restore it afterwards. Starting from the endianness
			// none of them wants is what makes that restoration observable.
			return new EndianReader(new MemoryStream(bytes), Endian.BigEndian);
		}

		[Fact]
		public void ChunkIdMixesEndiannessWithinASingleIdentifier()
		{
			// A FIoChunkId is not uniformly little-endian. The package ID is, but the chunk index
			// that follows it is big-endian, so that a byte-wise comparison of the first ten bytes
			// sorts chunks by package and then by index.
			IReader reader = ReaderOver(
				0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01, // package ID, little-endian
				0x01, 0x02,                                     // chunk index, big-endian
				0x00,                                           // padding
				0x02);                                          // chunk type: BulkData

			IoChunkId id = IoChunkId.Read(reader);

			Assert.Equal(0x0123456789ABCDEFUL, id.PackageId);
			Assert.Equal((ushort) 0x0102, id.ChunkIndex);
			Assert.Equal(IoChunkType.BulkData, id.Type);
		}

		[Fact]
		public void ChunkIdRestoresTheReadersEndiannessAfterwards()
		{
			IReader reader = ReaderOver(new byte[IoChunkId.SizeInBytes]);
			IoChunkId.Read(reader);
			Assert.Equal(Endian.BigEndian, reader.Endianness);
		}

		[Fact]
		public void ChunkIdSizeMatchesWhatReadingOneConsumes()
		{
			IReader reader = ReaderOver(new byte[IoChunkId.SizeInBytes]);
			IoChunkId.Read(reader);
			Assert.Equal(IoChunkId.SizeInBytes, reader.Position);
		}

		[Fact]
		public void CompressedBlockUnpacksA40BitOffsetAndTwo24BitSizes()
		{
			// The twelve bytes of a block entry are three fields packed across two integers:
			// a 40-bit offset and a 24-bit compressed size share the first eight bytes, and a
			// 24-bit uncompressed size shares the last four with an 8-bit method index. The offset
			// being 40 bits rather than 32 is why a container's block table cannot be modelled with
			// uint offsets, and is worth a test of its own.
			IReader reader = ReaderOver(
				// (compressedSize << 40) | offset, little-endian:
				//   offset          = 0xAABBCCDDEE
				//   compressedSize  =     0x112233
				0xEE, 0xDD, 0xCC, 0xBB, 0xAA, 0x33, 0x22, 0x11,
				// (method << 24) | uncompressedSize, little-endian:
				//   uncompressedSize = 0x445566
				//   method           = 0x07
				0x66, 0x55, 0x44, 0x07);

			IoCompressedBlock block = IoCompressedBlock.Read(reader);

			Assert.Equal(0xAABBCCDDEEL, block.Offset);
			Assert.Equal(0x112233, block.CompressedSize);
			Assert.Equal(0x445566, block.UncompressedSize);
			Assert.Equal(0x07, block.CompressionMethod);
		}

		[Fact]
		public void CompressedBlockOffsetIsNotTruncatedTo32Bits()
		{
			// The regression this guards: a 40-bit offset narrowed to uint silently addresses the
			// wrong place in a .ucas larger than 4 GiB, which retail containers are.
			IReader reader = ReaderOver(
				0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, // offset = 1 << 32
				0x00, 0x00, 0x00, 0x00);

			IoCompressedBlock block = IoCompressedBlock.Read(reader);

			Assert.Equal(1L << 32, block.Offset);
		}

		[Fact]
		public void StoredBlockIsTheOneWithNoCompressionMethod()
		{
			// Method index 0 means "not compressed" and is the only case the mod containers use;
			// everything else names an entry in the container's method-name table.
			Assert.True(new IoCompressedBlock(0, 16, 16, 0).IsStored);
			Assert.False(new IoCompressedBlock(0, 8, 16, 1).IsStored);
		}

		[Theory]
		[InlineData(0x626C6179, "blay")] // schema chunk
		[InlineData(0x62646174, "bdat")] // data chunk
		[InlineData(0x74677374, "tgst")] // struct section
		public void PayloadMagicsSpellForwardsOnceRead(int magic, string expected)
		{
			// Every four-CC in a payload is stored byte-reversed on disk, and reading it as a
			// little-endian integer is what un-reverses it. MagicToString then has to spell the
			// integer back out in the order a human reads it.
			Assert.Equal(expected, FifthGenChunk.MagicToString(magic));
		}

		[Fact]
		public void ReadingAReversedMagicYieldsTheSpelledOrder()
		{
			// 'blay' as it actually sits in a container: 79 61 6C 62.
			IReader reader = ReaderOver(0x79, 0x61, 0x6C, 0x62);
			reader.Endianness = Endian.LittleEndian;

			int magic = FifthGenChunk.ReadMagic(reader);

			Assert.Equal("blay", FifthGenChunk.MagicToString(magic));
		}

		[Fact]
		public void MagicToStringRendersAnEmbeddedNulRatherThanStoppingAtIt()
		{
			// The pageable resource section magic contains a zero byte, which is why this cannot
			// just delegate to CharConstant.ToString - that stops at the first NUL.
			Assert.Equal("a.bc", FifthGenChunk.MagicToString(0x61006263));
		}
	}
}
