// Ported from powzix/ooz (https://github.com/powzix/ooz), a from-scratch reimplementation
// of RAD Game Tools/Epic's Oodle Kraken decompressor. Original C++ source copyright (C)
// 2016, Powzix, licensed GNU GPL v3 (or later); ported to C# here under the same terms,
// which is compatible with Assembly/Blamite's own GPLv3 license (see the LICENSE file at
// the repository root). This only implements decompression of the "Kraken" sub-codec
// (decoder_type 6); Mermaid/Selkie, BitKnit and Leviathan are not ported.

namespace Blamite.Compression.Oodle
{
	internal unsafe struct HuffRevLut
	{
		public fixed byte Bits2Len[2048];
		public fixed byte Bits2Sym[2048];
	}

	internal unsafe struct HuffReader
	{
		// Array to hold the output of the huffman read array operation.
		public byte* Output;
		public byte* OutputEnd;
		// We decode three parallel streams, two forwards (|Src| and |SrcMid|)
		// while |SrcEnd| is decoded backwards.
		public byte* Src;
		public byte* SrcMid;
		public byte* SrcEnd;
		public byte* SrcMidOrg;
		public int SrcBitpos;
		public int SrcMidBitpos;
		public int SrcEndBitpos;
		public uint SrcBits;
		public uint SrcMidBits;
		public uint SrcEndBits;
	}

	internal struct HuffRange
	{
		public ushort Symbol;
		public ushort Num;
	}

	internal unsafe struct NewHuffLut
	{
		// Mapping that maps a bit pattern to a code length. May overflow 16 bytes past 2048.
		public fixed byte Bits2Len[2048 + 16];
		// Mapping that maps a bit pattern to a symbol. May overflow 16 bytes past 2048.
		public fixed byte Bits2Sym[2048 + 16];
	}

	// Ported from powzix/ooz's kraken.cpp (Kraken_DecodeBytes_Type12 and its helpers).
	// This is the huffman-coded entropy backend used for Kraken (and shared by the other
	// Oodle LZ variants) "chunk_type" 2 and 4 byte streams.
	internal static unsafe class HuffmanDecoder
	{
		// Precomputed 11-bit index bit-reversal table (2048 = 2^11 entries).
		private static readonly ushort[] ReverseBits11Table = BuildReverseBits11Table();

		private static ushort[] BuildReverseBits11Table()
		{
			var table = new ushort[2048];
			for (int i = 0; i < 2048; i++)
			{
				int x = i, r = 0;
				for (int b = 0; b < 11; b++)
				{
					r = (r << 1) | (x & 1);
					x >>= 1;
				}
				table[i] = (ushort)r;
			}
			return table;
		}

		// Rearranges elements in the input array so that bits in the index get flipped
		// (a full 11-bit bit-reversal permutation over the 2048-entry table). The original
		// does this with an 8x8 SSE2 byte-transpose network; this is the scalar equivalent.
		private static void ReverseBitsArray2048(byte* input, byte* output)
		{
			for (int i = 0; i < 2048; i++)
				output[ReverseBits11Table[i]] = input[i];
		}

		public static bool Kraken_DecodeBytesCore(ref HuffReader hr, ref HuffRevLut lut)
		{
			byte* src = hr.Src;
			uint srcBits = hr.SrcBits;
			int srcBitpos = hr.SrcBitpos;

			byte* srcMid = hr.SrcMid;
			uint srcMidBits = hr.SrcMidBits;
			int srcMidBitpos = hr.SrcMidBitpos;

			byte* srcEnd = hr.SrcEnd;
			uint srcEndBits = hr.SrcEndBits;
			int srcEndBitpos = hr.SrcEndBitpos;

			int k, n;

			byte* dst = hr.Output;
			byte* dstEnd = hr.OutputEnd;

			if (src > srcMid)
				return false;

			fixed (byte* bits2len = lut.Bits2Len)
			fixed (byte* bits2sym = lut.Bits2Sym)
			{
				if (hr.SrcEnd - srcMid >= 4 && dstEnd - dst >= 6)
				{
					dstEnd -= 5;
					srcEnd -= 4;

					while (dst < dstEnd && src <= srcMid && srcMid <= srcEnd)
					{
						srcBits |= *(uint*)src << srcBitpos;
						src += (31 - srcBitpos) >> 3;

						srcEndBits |= BitUtil.ByteSwap(*(uint*)srcEnd) << srcEndBitpos;
						srcEnd -= (31 - srcEndBitpos) >> 3;

						srcMidBits |= *(uint*)srcMid << srcMidBitpos;
						srcMid += (31 - srcMidBitpos) >> 3;

						srcBitpos |= 0x18;
						srcEndBitpos |= 0x18;
						srcMidBitpos |= 0x18;

						k = (int)(srcBits & 0x7FF);
						n = bits2len[k];
						srcBits >>= n;
						srcBitpos -= n;
						dst[0] = bits2sym[k];

						k = (int)(srcEndBits & 0x7FF);
						n = bits2len[k];
						srcEndBits >>= n;
						srcEndBitpos -= n;
						dst[1] = bits2sym[k];

						k = (int)(srcMidBits & 0x7FF);
						n = bits2len[k];
						srcMidBits >>= n;
						srcMidBitpos -= n;
						dst[2] = bits2sym[k];

						k = (int)(srcBits & 0x7FF);
						n = bits2len[k];
						srcBits >>= n;
						srcBitpos -= n;
						dst[3] = bits2sym[k];

						k = (int)(srcEndBits & 0x7FF);
						n = bits2len[k];
						srcEndBits >>= n;
						srcEndBitpos -= n;
						dst[4] = bits2sym[k];

						k = (int)(srcMidBits & 0x7FF);
						n = bits2len[k];
						srcMidBits >>= n;
						srcMidBitpos -= n;
						dst[5] = bits2sym[k];
						dst += 6;
					}
					dstEnd += 5;

					src -= srcBitpos >> 3;
					srcBitpos &= 7;

					srcEnd += 4 + (srcEndBitpos >> 3);
					srcEndBitpos &= 7;

					srcMid -= srcMidBitpos >> 3;
					srcMidBitpos &= 7;
				}
				for (;;)
				{
					if (dst >= dstEnd)
						break;

					if (srcMid - src <= 1)
					{
						if (srcMid - src == 1)
							srcBits |= (uint)(*src) << srcBitpos;
					}
					else
					{
						srcBits |= (uint)(*(ushort*)src) << srcBitpos;
					}
					k = (int)(srcBits & 0x7FF);
					n = bits2len[k];
					srcBitpos -= n;
					srcBits >>= n;
					*dst++ = bits2sym[k];
					src += (7 - srcBitpos) >> 3;
					srcBitpos &= 7;

					if (dst < dstEnd)
					{
						if (srcEnd - srcMid <= 1)
						{
							if (srcEnd - srcMid == 1)
							{
								srcEndBits |= (uint)(*srcMid) << srcEndBitpos;
								srcMidBits |= (uint)(*srcMid) << srcMidBitpos;
							}
						}
						else
						{
							uint v = *(ushort*)(srcEnd - 2);
							srcEndBits |= (((v >> 8) | (v << 8)) & 0xffff) << srcEndBitpos;
							srcMidBits |= (uint)(*(ushort*)srcMid) << srcMidBitpos;
						}
						n = bits2len[srcEndBits & 0x7FF];
						*dst++ = bits2sym[srcEndBits & 0x7FF];
						srcEndBitpos -= n;
						srcEndBits >>= n;
						srcEnd -= (7 - srcEndBitpos) >> 3;
						srcEndBitpos &= 7;
						if (dst < dstEnd)
						{
							n = bits2len[srcMidBits & 0x7FF];
							*dst++ = bits2sym[srcMidBits & 0x7FF];
							srcMidBitpos -= n;
							srcMidBits >>= n;
							srcMid += (7 - srcMidBitpos) >> 3;
							srcMidBitpos &= 7;
						}
					}
					if (src > srcMid || srcMid > srcEnd)
						return false;
				}
				if (src != hr.SrcMidOrg || srcEnd != srcMid)
					return false;
				return true;
			}
		}

		public static int Huff_ReadCodeLengthsOld(BitReader bits, byte* syms, uint* codePrefix)
		{
			if (bits.ReadBitNoRefill() != 0)
			{
				int n, sym = 0, codelen, numSymbols = 0;
				int avgBitsX4 = 32;
				int forcedBits = bits.ReadBitsNoRefill(2);

				uint thresForValidGammaBits = 1u << (31 - (int)(20u >> forcedBits));
				// The reference implementation jumps into the middle of the loop below
				// (past the leading "run of zeros" section) on the first iteration when
				// the initial bit is set. C# forbids jumping into a loop body, so a flag
				// stands in for that goto instead.
				bool skipInitialZeros = bits.ReadBit() != 0;
				do
				{
					if (!skipInitialZeros)
					{
						if ((bits.Bits & 0xff000000) == 0)
							return -1;
						sym += bits.ReadBitsNoRefill(2 * (BitUtil.CountLeadingZeros(bits.Bits) + 1)) - 2 + 1;
						if (sym >= 256)
							break;
					}
					skipInitialZeros = false;
					bits.Refill();
					if ((bits.Bits & 0xff000000) == 0)
						return -1;
					n = bits.ReadBitsNoRefill(2 * (BitUtil.CountLeadingZeros(bits.Bits) + 1)) - 2 + 1;
					if (sym + n > 256)
						return -1;
					bits.Refill();
					numSymbols += n;
					do
					{
						if (bits.Bits < thresForValidGammaBits)
							return -1;

						int lz = BitUtil.CountLeadingZeros(bits.Bits);
						int v = bits.ReadBitsNoRefill(lz + forcedBits + 1) + ((lz - 1) << forcedBits);
						codelen = (-(v & 1) ^ (v >> 1)) + ((avgBitsX4 + 2) >> 2);
						if (codelen < 1 || codelen > 11)
							return -1;
						avgBitsX4 = codelen + ((3 * avgBitsX4 + 2) >> 2);
						bits.Refill();
						syms[codePrefix[codelen]++] = (byte)sym++;
					} while (--n != 0);
				} while (sym != 256);
				return (sym == 256) && (numSymbols >= 2) ? numSymbols : -1;
			}
			else
			{
				int numSymbols = bits.ReadBitsNoRefill(8);
				if (numSymbols == 0)
					return -1;
				if (numSymbols == 1)
				{
					syms[0] = (byte)bits.ReadBitsNoRefill(8);
				}
				else
				{
					int codelenBits = bits.ReadBitsNoRefill(3);
					if (codelenBits > 4)
						return -1;
					for (int i = 0; i < numSymbols; i++)
					{
						bits.Refill();
						int sym = bits.ReadBitsNoRefill(8);
						int codelen = bits.ReadBitsNoRefillZero(codelenBits) + 1;
						if (codelen > 11)
							return -1;
						syms[codePrefix[codelen]++] = (byte)sym;
					}
				}
				return numSymbols;
			}
		}

		public static int Huff_ConvertToRanges(HuffRange* range, int numSymbols, int p, byte* symlen, BitReader bits)
		{
			int numRanges = p >> 1, v, symIdx = 0;

			if ((p & 1) != 0)
			{
				bits.Refill();
				v = *symlen++;
				if (v >= 8)
					return -1;
				symIdx = bits.ReadBitsNoRefill(v + 1) + (1 << (v + 1)) - 1;
			}
			int symsUsed = 0;

			for (int i = 0; i < numRanges; i++)
			{
				bits.Refill();
				v = symlen[0];
				if (v >= 9)
					return -1;
				int num = bits.ReadBitsNoRefillZero(v) + (1 << v);
				v = symlen[1];
				if (v >= 8)
					return -1;
				int space = bits.ReadBitsNoRefill(v + 1) + (1 << (v + 1)) - 1;
				range[i].Symbol = (ushort)symIdx;
				range[i].Num = (ushort)num;
				symsUsed += num;
				symIdx += num + space;
				symlen += 2;
			}

			if (symIdx >= 256 || symsUsed >= numSymbols || symIdx + numSymbols - symsUsed > 256)
				return -1;

			range[numRanges].Symbol = (ushort)symIdx;
			range[numRanges].Num = (ushort)(numSymbols - symsUsed);

			return numRanges + 1;
		}

		public static int Huff_ReadCodeLengthsNew(BitReader bits, byte* syms, uint* codePrefix)
		{
			int forcedBits = bits.ReadBitsNoRefill(2);

			int numSymbols = bits.ReadBitsNoRefill(8) + 1;

			int fluff = bits.ReadFluff(numSymbols);

			byte* codeLen = stackalloc byte[512];
			BitReader2 br2;
			br2.Bitpos = (uint)((bits.Bitpos - 24) & 7);
			br2.PEnd = bits.PEnd;
			br2.P = bits.P - (uint)((24 - bits.Bitpos + 7) >> 3);

			if (!GolombRice.DecodeGolombRiceLengths(codeLen, numSymbols + fluff, ref br2))
				return -1;
			for (int i = 0; i < 16; i++)
				codeLen[numSymbols + fluff + i] = 0;
			if (!GolombRice.DecodeGolombRiceBits(codeLen, (uint)numSymbols, (uint)forcedBits, ref br2))
				return -1;

			// Reset the bits decoder.
			bits.Bitpos = 24;
			bits.P = br2.P;
			bits.Bits = 0;
			bits.Refill();
			bits.Bits <<= (int)br2.Bitpos;
			bits.Bitpos += (int)br2.Bitpos;

			{
				uint runningSum = 0x1e;
				for (int i = 0; i < numSymbols; i++)
				{
					int v = codeLen[i];
					v = -(v & 1) ^ (v >> 1);
					codeLen[i] = (byte)(v + (runningSum >> 2) + 1);
					if (codeLen[i] < 1 || codeLen[i] > 11)
						return -1;
					runningSum = (uint)((int)runningSum + v);
				}
			}

			HuffRange* range = stackalloc HuffRange[128];
			int ranges = Huff_ConvertToRanges(range, numSymbols, fluff, &codeLen[numSymbols], bits);
			if (ranges <= 0)
				return -1;

			byte* cp = codeLen;
			for (int i = 0; i < ranges; i++)
			{
				int sym = range[i].Symbol;
				int n = range[i].Num;
				do
				{
					syms[codePrefix[*cp++]++] = (byte)sym++;
				} while (--n != 0);
			}

			return numSymbols;
		}

		// May write up to 16 bytes past dst+n.
		private static void FillByteOverflow16(byte* dst, byte v, uint n)
		{
			for (uint i = 0; i < n; i++)
				dst[i] = v;
		}

		public static bool Huff_MakeLut(uint* prefixOrg, uint* prefixCur, ref NewHuffLut hufflut, byte* syms)
		{
			uint currslot = 0;
			fixed (byte* bits2len = hufflut.Bits2Len)
			fixed (byte* bits2sym = hufflut.Bits2Sym)
			{
				for (uint i = 1; i < 11; i++)
				{
					uint start = prefixOrg[i];
					uint count = prefixCur[i] - start;
					if (count != 0)
					{
						uint stepsize = 1u << (int)(11 - i);
						uint numToSet = count << (int)(11 - i);
						if (currslot + numToSet > 2048)
							return false;
						FillByteOverflow16(&bits2len[currslot], (byte)i, numToSet);

						byte* p = &bits2sym[currslot];
						for (uint j = 0; j != count; j++, p += stepsize)
							FillByteOverflow16(p, syms[start + j], stepsize);
						currslot += numToSet;
					}
				}
				if (prefixCur[11] - prefixOrg[11] != 0)
				{
					uint numToSet = prefixCur[11] - prefixOrg[11];
					if (currslot + numToSet > 2048)
						return false;
					FillByteOverflow16(&bits2len[currslot], 11, numToSet);
					for (uint i = 0; i < numToSet; i++)
						bits2sym[currslot + i] = syms[prefixOrg[11] + i];
					currslot += numToSet;
				}
				return currslot == 2048;
			}
		}

		public static int Kraken_DecodeBytes_Type12(byte* src, long srcSize, byte* output, int outputSize, int type)
		{
			BitReader bits = new BitReader();
			int halfOutputSize;
			uint splitLeft, splitMid, splitRight;
			byte* srcMid;
			NewHuffLut huffLut = default;
			HuffReader hr = default;
			HuffRevLut revLut = default;
			byte* srcEnd = src + srcSize;

			bits.Bitpos = 24;
			bits.Bits = 0;
			bits.P = src;
			bits.PEnd = srcEnd;
			bits.Refill();

			uint* codePrefixOrg = stackalloc uint[12] { 0x0, 0x0, 0x2, 0x6, 0xE, 0x1E, 0x3E, 0x7E, 0xFE, 0x1FE, 0x2FE, 0x3FE };
			uint* codePrefix = stackalloc uint[12] { 0x0, 0x0, 0x2, 0x6, 0xE, 0x1E, 0x3E, 0x7E, 0xFE, 0x1FE, 0x2FE, 0x3FE };
			byte* syms = stackalloc byte[1280];
			int numSyms;
			if (bits.ReadBitNoRefill() == 0)
			{
				numSyms = Huff_ReadCodeLengthsOld(bits, syms, codePrefix);
			}
			else if (bits.ReadBitNoRefill() == 0)
			{
				numSyms = Huff_ReadCodeLengthsNew(bits, syms, codePrefix);
			}
			else
			{
				return -1;
			}

			if (numSyms < 1)
				return -1;
			src = bits.P - ((24 - bits.Bitpos) / 8);

			if (numSyms == 1)
			{
				for (int i = 0; i < outputSize; i++)
					output[i] = syms[0];
				return (int)(src - srcEnd);
			}

			if (!Huff_MakeLut(codePrefixOrg, codePrefix, ref huffLut, syms))
				return -1;

			// huffLut/revLut are locals, not ref parameters, so their fixed-size buffer
			// fields are already stack-resident and addressable without another `fixed`.
			ReverseBitsArray2048(huffLut.Bits2Len, revLut.Bits2Len);
			ReverseBitsArray2048(huffLut.Bits2Sym, revLut.Bits2Sym);

			if (type == 1)
			{
				if (src + 3 > srcEnd)
					return -1;
				splitMid = *(ushort*)src;
				src += 2;
				hr.Output = output;
				hr.OutputEnd = output + outputSize;
				hr.Src = src;
				hr.SrcEnd = srcEnd;
				hr.SrcMidOrg = hr.SrcMid = src + splitMid;
				hr.SrcBitpos = 0;
				hr.SrcBits = 0;
				hr.SrcMidBitpos = 0;
				hr.SrcMidBits = 0;
				hr.SrcEndBitpos = 0;
				hr.SrcEndBits = 0;
				if (!Kraken_DecodeBytesCore(ref hr, ref revLut))
					return -1;
			}
			else
			{
				if (src + 6 > srcEnd)
					return -1;

				halfOutputSize = (outputSize + 1) >> 1;
				splitMid = *(uint*)src & 0xFFFFFF;
				src += 3;
				if (splitMid > (uint)(srcEnd - src))
					return -1;
				srcMid = src + splitMid;
				splitLeft = *(ushort*)src;
				src += 2;
				if (srcMid - src < splitLeft + 2 || srcEnd - srcMid < 3)
					return -1;
				splitRight = *(ushort*)srcMid;
				if (srcEnd - (srcMid + 2) < splitRight + 2)
					return -1;

				hr.Output = output;
				hr.OutputEnd = output + halfOutputSize;
				hr.Src = src;
				hr.SrcEnd = srcMid;
				hr.SrcMidOrg = hr.SrcMid = src + splitLeft;
				hr.SrcBitpos = 0;
				hr.SrcBits = 0;
				hr.SrcMidBitpos = 0;
				hr.SrcMidBits = 0;
				hr.SrcEndBitpos = 0;
				hr.SrcEndBits = 0;
				if (!Kraken_DecodeBytesCore(ref hr, ref revLut))
					return -1;

				hr.Output = output + halfOutputSize;
				hr.OutputEnd = output + outputSize;
				hr.Src = srcMid + 2;
				hr.SrcEnd = srcEnd;
				hr.SrcMidOrg = hr.SrcMid = srcMid + 2 + splitRight;
				hr.SrcBitpos = 0;
				hr.SrcBits = 0;
				hr.SrcMidBitpos = 0;
				hr.SrcMidBits = 0;
				hr.SrcEndBitpos = 0;
				hr.SrcEndBits = 0;
				if (!Kraken_DecodeBytesCore(ref hr, ref revLut))
					return -1;
			}
			return (int)srcSize;
		}
	}
}
