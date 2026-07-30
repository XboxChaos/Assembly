// Ported from powzix/ooz (https://github.com/powzix/ooz), a from-scratch reimplementation
// of RAD Game Tools/Epic's Oodle Kraken decompressor. Original C++ source copyright (C)
// 2016, Powzix, licensed GNU GPL v3 (or later); ported to C# here under the same terms,
// which is compatible with Assembly/Blamite's own GPLv3 license (see the LICENSE file at
// the repository root). This only implements decompression of the "Kraken" sub-codec
// (decoder_type 6); Mermaid/Selkie, BitKnit and Leviathan are not ported.

namespace Blamite.Compression.Oodle
{
	internal unsafe struct TansData
	{
		public uint AUsed;
		public uint BUsed;
		public fixed byte A[256];
		public fixed uint B[256];
	}

	internal struct TansLutEnt
	{
		public uint X;
		public byte BitsX;
		public byte Symbol;
		public ushort W;
	}

	internal unsafe struct TansDecoderParams
	{
		public TansLutEnt* Lut;
		public byte* Dst, DstEnd;
		public byte* PtrF, PtrB;
		public uint BitsF, BitsB;
		public int BitposF, BitposB;
		public uint State0, State1, State2, State3, State4;
	}

	// Ported from powzix/ooz's kraken.cpp: the tANS ("chunk_type" 1) entropy backend.
	internal static unsafe class TansDecoder
	{
		private static void SimpleSortBytes(byte* p, byte* pend)
		{
			if (p == pend)
				return;
			for (byte* lp = p + 1; lp != pend; lp++)
			{
				byte t = *lp;
				byte* rp = lp;
				while (rp > p && t < rp[-1])
				{
					rp[0] = rp[-1];
					rp--;
				}
				rp[0] = t;
			}
		}

		private static void SimpleSortUInt(uint* p, uint* pend)
		{
			if (p == pend)
				return;
			for (uint* lp = p + 1; lp != pend; lp++)
			{
				uint t = *lp;
				uint* rp = lp;
				while (rp > p && t < rp[-1])
				{
					rp[0] = rp[-1];
					rp--;
				}
				rp[0] = t;
			}
		}

		public static bool Tans_DecodeTable(BitReader bits, int lBits, TansData* tansData)
		{
			bits.Refill();
			if (bits.ReadBitNoRefill() != 0)
			{
				int q = bits.ReadBitsNoRefill(3);
				int numSymbols = bits.ReadBitsNoRefill(8) + 1;
				if (numSymbols < 2)
					return false;
				int fluff = bits.ReadFluff(numSymbols);
				int totalRiceValues = fluff + numSymbols;
				byte* rice = stackalloc byte[512 + 16];
				BitReader2 br2;

				br2.P = bits.P - (uint)((24 - bits.Bitpos + 7) >> 3);
				br2.PEnd = bits.PEnd;
				br2.Bitpos = (uint)((bits.Bitpos - 24) & 7);

				if (!GolombRice.DecodeGolombRiceLengths(rice, totalRiceValues, ref br2))
					return false;
				for (int i = 0; i < 16; i++)
					rice[totalRiceValues + i] = 0;

				bits.Bitpos = 24;
				bits.P = br2.P;
				bits.Bits = 0;
				bits.Refill();
				bits.Bits <<= (int)br2.Bitpos;
				bits.Bitpos += (int)br2.Bitpos;

				HuffRange* range = stackalloc HuffRange[133];
				fluff = HuffmanDecoder.Huff_ConvertToRanges(range, numSymbols, fluff, &rice[numSymbols], bits);
				if (fluff < 0)
					return false;

				bits.Refill();

				uint l = 1u << lBits;
				byte* curRicePtr = rice;
				int average = 6;
				int somesum = 0;
				byte* tanstableA = tansData->A;
				uint* tanstableB = tansData->B;

				for (int ri = 0; ri < fluff; ri++)
				{
					int symbol = range[ri].Symbol;
					int num = range[ri].Num;
					do
					{
						bits.Refill();

						int nextra = q + *curRicePtr++;
						if (nextra > 15)
							return false;
						int v = bits.ReadBitsNoRefillZero(nextra) + (1 << nextra) - (1 << q);

						int averageDiv4 = average >> 2;
						int limit = 2 * averageDiv4;
						if (v <= limit)
							v = averageDiv4 + (-(v & 1) ^ (v >> 1));
						if (limit > v)
							limit = v;
						v += 1;
						average += limit - averageDiv4;
						*tanstableA = (byte)symbol;
						*tanstableB = (uint)((symbol << 16) + v);
						tanstableA += (v == 1) ? 1 : 0;
						tanstableB += (v >= 2) ? 1 : 0;
						somesum += v;
						symbol += 1;
					} while (--num != 0);
				}
				tansData->AUsed = (uint)(tanstableA - tansData->A);
				tansData->BUsed = (uint)(tanstableB - tansData->B);
				if (somesum != l)
					return false;

				return true;
			}
			else
			{
				bool* seen = stackalloc bool[256];
				for (int i = 0; i < 256; i++)
					seen[i] = false;
				uint l = 1u << lBits;

				int count = bits.ReadBitsNoRefill(3) + 1;

				int bitsPerSym = BitUtil.Bsr((uint)lBits) + 1;
				int maxDeltaBits = bits.ReadBitsNoRefill(bitsPerSym);

				if (maxDeltaBits == 0 || maxDeltaBits > lBits)
					return false;

				byte* tanstableA = tansData->A;
				uint* tanstableB = tansData->B;

				int weight = 0;
				int totalWeights = 0;

				do
				{
					bits.Refill();

					int sym = bits.ReadBitsNoRefill(8);
					if (seen[sym])
						return false;

					int delta = bits.ReadBitsNoRefill(maxDeltaBits);

					weight += delta;

					if (weight == 0)
						return false;

					seen[sym] = true;
					if (weight == 1)
					{
						*tanstableA++ = (byte)sym;
					}
					else
					{
						*tanstableB++ = (uint)((sym << 16) + weight);
					}

					totalWeights += weight;
				} while (--count != 0);

				bits.Refill();

				int finalSym = bits.ReadBitsNoRefill(8);
				if (seen[finalSym])
					return false;

				if (l - totalWeights < weight || l - totalWeights <= 1)
					return false;

				*tanstableB++ = (uint)((finalSym << 16) + (l - totalWeights));

				tansData->AUsed = (uint)(tanstableA - tansData->A);
				tansData->BUsed = (uint)(tanstableB - tansData->B);

				SimpleSortBytes(tansData->A, tanstableA);
				SimpleSortUInt(tansData->B, tanstableB);
				return true;
			}
		}

		public static void Tans_InitLut(TansData* tansData, int lBits, TansLutEnt* lut)
		{
			TansLutEnt** pointers = stackalloc TansLutEnt*[4];

			int l = 1 << lBits;
			int aUsed = (int)tansData->AUsed;

			uint slotsLeftToAlloc = (uint)(l - aUsed);

			uint sa = slotsLeftToAlloc >> 2;
			pointers[0] = lut;
			uint sb = sa + (uint)(((slotsLeftToAlloc & 3) > 0) ? 1 : 0);
			pointers[1] = lut + sb;
			sb += sa + (uint)(((slotsLeftToAlloc & 3) > 1) ? 1 : 0);
			pointers[2] = lut + sb;
			sb += sa + (uint)(((slotsLeftToAlloc & 3) > 2) ? 1 : 0);
			pointers[3] = lut + sb;

			// Set up the single entries with weight == 1.
			{
				TansLutEnt* lutSingles = lut + slotsLeftToAlloc;
				TansLutEnt le = default;
				le.W = 0;
				le.BitsX = (byte)lBits;
				le.X = (uint)((1 << lBits) - 1);
				for (int i = 0; i < aUsed; i++)
				{
					lutSingles[i] = le;
					lutSingles[i].Symbol = tansData->A[i];
				}
			}

			// Set up the entries with weight >= 2.
			int weightsSum = 0;
			for (int i = 0; i < tansData->BUsed; i++)
			{
				int weight = (int)(tansData->B[i] & 0xffff);
				int symbol = (int)(tansData->B[i] >> 16);
				if (weight > 4)
				{
					uint symBits = (uint)BitUtil.Bsr((uint)weight);
					int z = lBits - (int)symBits;
					TansLutEnt le = default;
					le.Symbol = (byte)symbol;
					le.BitsX = (byte)z;
					le.X = (uint)((1 << z) - 1);
					le.W = (ushort)((l - 1) & (weight << z));
					int whatToAdd = 1 << z;
					int x = (1 << ((int)symBits + 1)) - weight;

					for (int j = 0; j < 4; j++)
					{
						TansLutEnt* dst = pointers[j];

						int y = (weight + ((weightsSum - j - 1) & 3)) >> 2;
						if (x >= y)
						{
							for (int n = y; n != 0; n--)
							{
								*dst++ = le;
								le.W = (ushort)(le.W + whatToAdd);
							}
							x -= y;
						}
						else
						{
							for (int n = x; n != 0; n--)
							{
								*dst++ = le;
								le.W = (ushort)(le.W + whatToAdd);
							}
							z--;

							whatToAdd >>= 1;
							le.BitsX = (byte)z;
							le.W = 0;
							le.X >>= 1;
							for (int n = y - x; n != 0; n--)
							{
								*dst++ = le;
								le.W = (ushort)(le.W + whatToAdd);
							}
							x = weight;
						}
						pointers[j] = dst;
					}
				}
				else
				{
					uint bitsMask = (uint)(((1 << weight) - 1) << (weightsSum & 3));
					bitsMask |= bitsMask >> 4;
					int n = weight, ww = weight;
					do
					{
						uint idx = (uint)BitUtil.Bsf(bitsMask);
						bitsMask &= bitsMask - 1;
						TansLutEnt* dst = pointers[idx]++;
						dst->Symbol = (byte)symbol;
						uint weightBits = (uint)BitUtil.Bsr((uint)ww);
						dst->BitsX = (byte)(lBits - weightBits);
						dst->X = (uint)((1 << (int)(lBits - weightBits)) - 1);
						dst->W = (ushort)((l - 1) & (ww++ << (int)(lBits - weightBits)));
					} while (--n != 0);
				}
				weightsSum += weight;
			}
		}

		public static bool Tans_Decode(ref TansDecoderParams p)
		{
			TansLutEnt* lut = p.Lut, e;
			byte* dst = p.Dst, dstEnd = p.DstEnd;
			byte* ptrF = p.PtrF, ptrB = p.PtrB;
			uint bitsF = p.BitsF, bitsB = p.BitsB;
			int bitposF = p.BitposF, bitposB = p.BitposB;
			uint state0 = p.State0, state1 = p.State1;
			uint state2 = p.State2, state3 = p.State3;
			uint state4 = p.State4;

			if (ptrF > ptrB)
				return false;

			if (dst < dstEnd)
			{
				for (;;)
				{
					// forward bits
					bitsF |= *(uint*)ptrF << bitposF;
					ptrF += (31 - bitposF) >> 3;
					bitposF |= 24;

					e = &lut[state0];
					*dst++ = e->Symbol;
					bitposF -= e->BitsX;
					state0 = (bitsF & e->X) + e->W;
					bitsF >>= e->BitsX;
					if (dst >= dstEnd) break;

					e = &lut[state1];
					*dst++ = e->Symbol;
					bitposF -= e->BitsX;
					state1 = (bitsF & e->X) + e->W;
					bitsF >>= e->BitsX;
					if (dst >= dstEnd) break;

					// forward bits
					bitsF |= *(uint*)ptrF << bitposF;
					ptrF += (31 - bitposF) >> 3;
					bitposF |= 24;

					e = &lut[state2];
					*dst++ = e->Symbol;
					bitposF -= e->BitsX;
					state2 = (bitsF & e->X) + e->W;
					bitsF >>= e->BitsX;
					if (dst >= dstEnd) break;

					e = &lut[state3];
					*dst++ = e->Symbol;
					bitposF -= e->BitsX;
					state3 = (bitsF & e->X) + e->W;
					bitsF >>= e->BitsX;
					if (dst >= dstEnd) break;

					// forward bits
					bitsF |= *(uint*)ptrF << bitposF;
					ptrF += (31 - bitposF) >> 3;
					bitposF |= 24;

					e = &lut[state4];
					*dst++ = e->Symbol;
					bitposF -= e->BitsX;
					state4 = (bitsF & e->X) + e->W;
					bitsF >>= e->BitsX;
					if (dst >= dstEnd) break;

					// backward bits
					bitsB |= BitUtil.ByteSwap(((uint*)ptrB)[-1]) << bitposB;
					ptrB -= (31 - bitposB) >> 3;
					bitposB |= 24;

					e = &lut[state0];
					*dst++ = e->Symbol;
					bitposB -= e->BitsX;
					state0 = (bitsB & e->X) + e->W;
					bitsB >>= e->BitsX;
					if (dst >= dstEnd) break;

					e = &lut[state1];
					*dst++ = e->Symbol;
					bitposB -= e->BitsX;
					state1 = (bitsB & e->X) + e->W;
					bitsB >>= e->BitsX;
					if (dst >= dstEnd) break;

					// backward bits
					bitsB |= BitUtil.ByteSwap(((uint*)ptrB)[-1]) << bitposB;
					ptrB -= (31 - bitposB) >> 3;
					bitposB |= 24;

					e = &lut[state2];
					*dst++ = e->Symbol;
					bitposB -= e->BitsX;
					state2 = (bitsB & e->X) + e->W;
					bitsB >>= e->BitsX;
					if (dst >= dstEnd) break;

					e = &lut[state3];
					*dst++ = e->Symbol;
					bitposB -= e->BitsX;
					state3 = (bitsB & e->X) + e->W;
					bitsB >>= e->BitsX;
					if (dst >= dstEnd) break;

					// backward bits
					bitsB |= BitUtil.ByteSwap(((uint*)ptrB)[-1]) << bitposB;
					ptrB -= (31 - bitposB) >> 3;
					bitposB |= 24;

					e = &lut[state4];
					*dst++ = e->Symbol;
					bitposB -= e->BitsX;
					state4 = (bitsB & e->X) + e->W;
					bitsB >>= e->BitsX;
					if (dst >= dstEnd) break;
				}
			}

			if (ptrB - ptrF + (bitposF >> 3) + (bitposB >> 3) != 0)
				return false;

			uint statesOr = state0 | state1 | state2 | state3 | state4;
			if ((statesOr & ~0xFFu) != 0)
				return false;

			dstEnd[0] = (byte)state0;
			dstEnd[1] = (byte)state1;
			dstEnd[2] = (byte)state2;
			dstEnd[3] = (byte)state3;
			dstEnd[4] = (byte)state4;
			return true;
		}

		public static int Krak_DecodeTans(byte* src, long srcSize, byte* dst, int dstSize, byte* scratch, byte* scratchEnd)
		{
			if (srcSize < 8 || dstSize < 5)
				return -1;

			byte* srcEnd = src + srcSize;

			BitReader br = new BitReader();
			TansData tansData = default;

			br.Bitpos = 24;
			br.Bits = 0;
			br.P = src;
			br.PEnd = srcEnd;
			br.Refill();

			// reserved bit
			if (br.ReadBitNoRefill() != 0)
				return -1;

			int lBits = br.ReadBitsNoRefill(2) + 8;

			if (!Tans_DecodeTable(br, lBits, &tansData))
				return -1;

			src = br.P - (24 - br.Bitpos) / 8;

			if (src >= srcEnd)
				return -1;

			uint lutSpaceRequired = (uint)((sizeof(TansLutEnt) << lBits) + 15) & ~15u;
			if (lutSpaceRequired > (ulong)(scratchEnd - scratch))
				return -1;

			TansDecoderParams parms = default;
			parms.Dst = dst;
			parms.DstEnd = dst + dstSize - 5;

			parms.Lut = (TansLutEnt*)((((long)scratch) + 15) & ~15L);
			Tans_InitLut(&tansData, lBits, parms.Lut);

			uint lMask = (1u << lBits) - 1;
			uint bitsF = *(uint*)src;
			src += 4;
			uint bitsB = BitUtil.ByteSwap(*(uint*)(srcEnd - 4));
			srcEnd -= 4;
			uint bitposF = 32, bitposB = 32;

			parms.State0 = bitsF & lMask;
			parms.State1 = bitsB & lMask;
			bitsF >>= lBits; bitposF -= (uint)lBits;
			bitsB >>= lBits; bitposB -= (uint)lBits;

			parms.State2 = bitsF & lMask;
			parms.State3 = bitsB & lMask;
			bitsF >>= lBits; bitposF -= (uint)lBits;
			bitsB >>= lBits; bitposB -= (uint)lBits;

			bitsF |= *(uint*)src << (int)bitposF;
			src += (31 - (int)bitposF) >> 3;
			bitposF |= 24;

			parms.State4 = bitsF & lMask;
			bitsF >>= lBits; bitposF -= (uint)lBits;

			parms.BitsF = bitsF;
			parms.PtrF = src - ((int)bitposF >> 3);
			parms.BitposF = (int)bitposF & 7;

			parms.BitsB = bitsB;
			parms.PtrB = srcEnd + ((int)bitposB >> 3);
			parms.BitposB = (int)bitposB & 7;

			if (!Tans_Decode(ref parms))
				return -1;

			return (int)srcSize;
		}
	}
}
