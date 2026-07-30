// Ported from powzix/ooz (https://github.com/powzix/ooz), a from-scratch reimplementation
// of RAD Game Tools/Epic's Oodle Kraken decompressor. Original C++ source copyright (C)
// 2016, Powzix, licensed GNU GPL v3 (or later); ported to C# here under the same terms,
// which is compatible with Assembly/Blamite's own GPLv3 license (see the LICENSE file at
// the repository root). This only implements decompression of the "Kraken" sub-codec
// (decoder_type 6); Mermaid/Selkie, BitKnit and Leviathan are not ported.

namespace Blamite.Compression.Oodle
{
	// Ported from powzix/ooz's kraken.cpp: the Kraken (decoder_type == 6) LZ backend and the
	// shared "Kraken_DecodeBytes" entropy-coded byte-stream dispatcher it (and, in the
	// original, the other Oodle LZ variants) builds on.
	internal static unsafe class KrakenCore
	{
		private static long Min(long a, long b) => a < b ? a : b;

		// 8-byte raw copy, unaligned.
		private static void Copy64(byte* d, byte* s)
		{
			*(ulong*)d = *(ulong*)s;
		}

		// 64-byte raw copy, unaligned (4x16 in the original via SSE2; plain here).
		private static void Copy64Bytes(byte* d, byte* s)
		{
			for (int i = 0; i < 8; i++)
				((ulong*)d)[i] = ((ulong*)s)[i];
		}

		// Per-byte (mod 256) add of two 8-byte spans into d. Mirrors the SSE2
		// _mm_add_epi8 used by COPY_64_ADD -- NOT a 64-bit integer add.
		private static void Copy64Add(byte* d, byte* s, byte* t)
		{
			for (int i = 0; i < 8; i++)
				d[i] = (byte)(s[i] + t[i]);
		}

		public static int Kraken_GetBlockSize(byte* src, byte* srcEnd, out int destSize, int destCapacity)
		{
			byte* srcOrg = src;
			int srcSize, dstSize;
			destSize = 0;

			if (srcEnd - src < 2)
				return -1;

			int chunkType = (src[0] >> 4) & 0x7;
			if (chunkType == 0)
			{
				if (src[0] >= 0x80)
				{
					srcSize = ((src[0] << 8) | src[1]) & 0xFFF;
					src += 2;
				}
				else
				{
					if (srcEnd - src < 3)
						return -1;
					srcSize = (src[0] << 16) | (src[1] << 8) | src[2];
					if ((srcSize & ~0x3ffff) != 0)
						return -1;
					src += 3;
				}
				if (srcSize > destCapacity || srcEnd - src < srcSize)
					return -1;
				destSize = srcSize;
				return (int)(src + srcSize - srcOrg);
			}

			if (chunkType >= 6)
				return -1;

			if (src[0] >= 0x80)
			{
				if (srcEnd - src < 3)
					return -1;
				uint bits = (uint)((src[0] << 16) | (src[1] << 8) | src[2]);
				srcSize = (int)(bits & 0x3ff);
				dstSize = srcSize + (int)((bits >> 10) & 0x3ff) + 1;
				src += 3;
			}
			else
			{
				if (srcEnd - src < 5)
					return -1;
				uint bits = (uint)((src[1] << 24) | (src[2] << 16) | (src[3] << 8) | src[4]);
				srcSize = (int)(bits & 0x3ffff);
				dstSize = (int)((((bits >> 18) | ((uint)src[0] << 14)) & 0x3FFFF) + 1);
				if (srcSize >= dstSize)
					return -1;
				src += 5;
			}
			if (srcEnd - src < srcSize || dstSize > destCapacity)
				return -1;
			destSize = dstSize;
			return srcSize;
		}

		public static int Kraken_DecodeBytes(ref byte* output, byte* src, byte* srcEnd, out int decodedSize, long outputSize, bool forceMemmove, byte* scratch, byte* scratchEnd)
		{
			byte* srcOrg = src;
			int srcSize, dstSize;
			decodedSize = 0;

			if (srcEnd - src < 2)
				return -1;

			int chunkType = (src[0] >> 4) & 0x7;
			if (chunkType == 0)
			{
				if (src[0] >= 0x80)
				{
					srcSize = ((src[0] << 8) | src[1]) & 0xFFF;
					src += 2;
				}
				else
				{
					if (srcEnd - src < 3)
						return -1;
					srcSize = (src[0] << 16) | (src[1] << 8) | src[2];
					if ((srcSize & ~0x3ffff) != 0)
						return -1;
					src += 3;
				}
				if (srcSize > outputSize || srcEnd - src < srcSize)
					return -1;
				decodedSize = srcSize;
				if (forceMemmove)
				{
					for (int i = 0; i < srcSize; i++)
						output[i] = src[i];
				}
				else
				{
					output = src;
				}
				return (int)(src + srcSize - srcOrg);
			}

			if (src[0] >= 0x80)
			{
				if (srcEnd - src < 3)
					return -1;
				uint bits = (uint)((src[0] << 16) | (src[1] << 8) | src[2]);
				srcSize = (int)(bits & 0x3ff);
				dstSize = srcSize + (int)((bits >> 10) & 0x3ff) + 1;
				src += 3;
			}
			else
			{
				if (srcEnd - src < 5)
					return -1;
				uint bits = (uint)((src[1] << 24) | (src[2] << 16) | (src[3] << 8) | src[4]);
				srcSize = (int)(bits & 0x3ffff);
				dstSize = (int)((((bits >> 18) | ((uint)src[0] << 14)) & 0x3FFFF) + 1);
				if (srcSize >= dstSize)
					return -1;
				src += 5;
			}
			if (srcEnd - src < srcSize || dstSize > outputSize)
				return -1;

			byte* dst = output;
			if (dst == scratch)
			{
				if (scratchEnd - scratch < dstSize)
					return -1;
				scratch += dstSize;
			}

			int srcUsed = -1;
			switch (chunkType)
			{
				case 2:
				case 4:
					srcUsed = HuffmanDecoder.Kraken_DecodeBytes_Type12(src, srcSize, dst, dstSize, chunkType >> 1);
					break;
				case 5:
					srcUsed = Krak_DecodeRecursive(src, srcSize, dst, dstSize, scratch, scratchEnd);
					break;
				case 3:
					srcUsed = Krak_DecodeRLE(src, srcSize, dst, dstSize, scratch, scratchEnd);
					break;
				case 1:
					srcUsed = TansDecoder.Krak_DecodeTans(src, srcSize, dst, dstSize, scratch, scratchEnd);
					break;
			}
			if (srcUsed != srcSize)
				return -1;
			decodedSize = dstSize;
			return (int)(src + srcSize - srcOrg);
		}

		public static int Krak_DecodeRecursive(byte* src, long srcSize, byte* output, int outputSize, byte* scratch, byte* scratchEnd)
		{
			byte* srcOrg = src;
			byte* outputEnd = output + outputSize;
			byte* srcEnd = src + srcSize;

			if (srcSize < 6)
				return -1;

			int n = src[0] & 0x7f;
			if (n < 2)
				return -1;

			if ((src[0] & 0x80) == 0)
			{
				src++;
				do
				{
					int decodedSize;
					int dec = Kraken_DecodeBytes(ref output, src, srcEnd, out decodedSize, outputEnd - output, true, scratch, scratchEnd);
					if (dec < 0)
						return -1;
					output += decodedSize;
					src += dec;
				} while (--n != 0);
				if (output != outputEnd)
					return -1;
				return (int)(src - srcOrg);
			}
			else
			{
				// Multi-array recursive decode with a single array is not exercised by any
				// of the containers this decoder targets (it requires an encoder that emits
				// the "array count > 1"-oriented recursive framing). Left unimplemented,
				// matching the scope of the rest of this port.
				throw new OodleDecoderException("Kraken: DecodeMultiArray-based recursive byte decode is not implemented.");
			}
		}

		public static int Krak_DecodeRLE(byte* src, long srcSize, byte* dst, int dstSize, byte* scratch, byte* scratchEnd)
		{
			if (srcSize <= 1)
			{
				if (srcSize != 1)
					return -1;
				for (int i = 0; i < dstSize; i++)
					dst[i] = src[0];
				return 1;
			}
			byte* dstEnd = dst + dstSize;
			byte* cmdPtr = src + 1, cmdPtrEnd = src + srcSize;
			if (src[0] != 0)
			{
				byte* dstPtr = scratch;
				int decSize;
				int n = Kraken_DecodeBytes(ref dstPtr, src, src + srcSize, out decSize, scratchEnd - scratch, true, scratch, scratchEnd);
				if (n <= 0)
					return -1;
				int cmdLen = (int)(srcSize - n + decSize);
				if (cmdLen > scratchEnd - scratch)
					return -1;
				for (int i = 0; i < srcSize - n; i++)
					dstPtr[decSize + i] = src[n + i];
				cmdPtr = dstPtr;
				cmdPtrEnd = &dstPtr[cmdLen];
			}

			int rleByte = 0;

			while (cmdPtr < cmdPtrEnd)
			{
				uint cmd = cmdPtrEnd[-1];
				if (cmd - 1 >= 0x2f)
				{
					cmdPtrEnd--;
					uint bytesToCopy = (uint)((-1 - cmd) & 0xF);
					uint bytesToRle = cmd >> 4;
					if (dstEnd - dst < bytesToCopy + bytesToRle || cmdPtrEnd - cmdPtr < bytesToCopy)
						return -1;
					for (uint i = 0; i < bytesToCopy; i++)
						dst[i] = cmdPtr[i];
					cmdPtr += bytesToCopy;
					dst += bytesToCopy;
					for (uint i = 0; i < bytesToRle; i++)
						dst[i] = (byte)rleByte;
					dst += bytesToRle;
				}
				else if (cmd >= 0x10)
				{
					uint data = (uint)(*(ushort*)(cmdPtrEnd - 2) - 4096);
					cmdPtrEnd -= 2;
					uint bytesToCopy = data & 0x3F;
					uint bytesToRle = data >> 6;
					if (dstEnd - dst < bytesToCopy + bytesToRle || cmdPtrEnd - cmdPtr < bytesToCopy)
						return -1;
					for (uint i = 0; i < bytesToCopy; i++)
						dst[i] = cmdPtr[i];
					cmdPtr += bytesToCopy;
					dst += bytesToCopy;
					for (uint i = 0; i < bytesToRle; i++)
						dst[i] = (byte)rleByte;
					dst += bytesToRle;
				}
				else if (cmd == 1)
				{
					rleByte = *cmdPtr++;
					cmdPtrEnd--;
				}
				else if (cmd >= 9)
				{
					uint bytesToRle = (uint)((*(ushort*)(cmdPtrEnd - 2) - 0x8ff) * 128);
					cmdPtrEnd -= 2;
					if (dstEnd - dst < bytesToRle)
						return -1;
					for (uint i = 0; i < bytesToRle; i++)
						dst[i] = (byte)rleByte;
					dst += bytesToRle;
				}
				else
				{
					uint bytesToCopy = (uint)((*(ushort*)(cmdPtrEnd - 2) - 511) * 64);
					cmdPtrEnd -= 2;
					if (cmdPtrEnd - cmdPtr < bytesToCopy || dstEnd - dst < bytesToCopy)
						return -1;
					for (uint i = 0; i < bytesToCopy; i++)
						dst[i] = cmdPtr[i];
					dst += bytesToCopy;
					cmdPtr += bytesToCopy;
				}
			}
			if (cmdPtrEnd != cmdPtr)
				return -1;

			if (dst != dstEnd)
				return -1;

			return (int)srcSize;
		}

		private static void CombineScaledOffsetArrays(int* offsStream, long offsStreamSize, int scale, byte* lowBits)
		{
			for (long i = 0; i != offsStreamSize; i++)
				offsStream[i] = scale * offsStream[i] - lowBits[i];
		}

		public static bool Kraken_UnpackOffsets(byte* src, byte* srcEnd,
			byte* packedOffsStream, byte* packedOffsStreamExtra, int packedOffsStreamSize,
			int multiDistScale,
			byte* packedLitlenStream, int packedLitlenStreamSize,
			int* offsStream, int* lenStream)
		{
			BitReader bitsA = new BitReader();
			BitReader bitsB = new BitReader();
			int n, i;
			int u32LenStreamSize = 0;

			bitsA.Bitpos = 24;
			bitsA.Bits = 0;
			bitsA.P = src;
			bitsA.PEnd = srcEnd;
			bitsA.Refill();

			bitsB.Bitpos = 24;
			bitsB.Bits = 0;
			bitsB.P = srcEnd;
			bitsB.PEnd = src;
			bitsB.RefillBackwards();

			{
				if (bitsB.Bits < 0x2000)
					return false;
				n = 31 - BitUtil.Bsr(bitsB.Bits);
				bitsB.Bitpos += n;
				bitsB.Bits <<= n;
				bitsB.RefillBackwards();
				n++;
				u32LenStreamSize = (int)(bitsB.Bits >> (32 - n)) - 1;
				bitsB.Bitpos += n;
				bitsB.Bits <<= n;
				bitsB.RefillBackwards();
			}

			int* offsStreamCur = offsStream;
			if (multiDistScale == 0)
			{
				byte* packedOffsStreamEnd = packedOffsStream + packedOffsStreamSize;
				while (packedOffsStream != packedOffsStreamEnd)
				{
					*offsStreamCur++ = -(int)bitsA.ReadDistance(*packedOffsStream++);
					if (packedOffsStream == packedOffsStreamEnd)
						break;
					*offsStreamCur++ = -(int)bitsB.ReadDistanceB(*packedOffsStream++);
				}
			}
			else
			{
				int* offsStreamOrg = offsStreamCur;
				byte* packedOffsStreamEnd = packedOffsStream + packedOffsStreamSize;
				uint cmd, offs;
				while (packedOffsStream != packedOffsStreamEnd)
				{
					cmd = *packedOffsStream++;
					if ((cmd >> 3) > 26)
						return false;
					offs = ((8u + (cmd & 7)) << (int)(cmd >> 3)) | bitsA.ReadMoreThan24Bits((int)(cmd >> 3));
					*offsStreamCur++ = 8 - (int)offs;
					if (packedOffsStream == packedOffsStreamEnd)
						break;
					cmd = *packedOffsStream++;
					if ((cmd >> 3) > 26)
						return false;
					offs = ((8u + (cmd & 7)) << (int)(cmd >> 3)) | bitsB.ReadMoreThan24BitsB((int)(cmd >> 3));
					*offsStreamCur++ = 8 - (int)offs;
				}
				if (multiDistScale != 1)
				{
					CombineScaledOffsetArrays(offsStreamOrg, offsStreamCur - offsStreamOrg, multiDistScale, packedOffsStreamExtra);
				}
			}

			if (u32LenStreamSize > 512)
				return false;

			uint* u32LenStreamBuf = stackalloc uint[512];
			uint* u32LenStream = u32LenStreamBuf;
			uint* u32LenStreamEnd = u32LenStreamBuf + u32LenStreamSize;
			for (i = 0; i + 1 < u32LenStreamSize; i += 2)
			{
				if (!bitsA.ReadLength(out u32LenStreamBuf[i + 0]))
					return false;
				if (!bitsB.ReadLengthB(out u32LenStreamBuf[i + 1]))
					return false;
			}
			if (i < u32LenStreamSize)
			{
				if (!bitsA.ReadLength(out u32LenStreamBuf[i + 0]))
					return false;
			}

			bitsA.P -= (24 - bitsA.Bitpos) >> 3;
			bitsB.P += (24 - bitsB.Bitpos) >> 3;

			if (bitsA.P != bitsB.P)
				return false;

			for (i = 0; i < packedLitlenStreamSize; i++)
			{
				uint v = packedLitlenStream[i];
				if (v == 255)
					v = *u32LenStream++ + 255;
				lenStream[i] = (int)(v + 3);
			}
			if (u32LenStream != u32LenStreamEnd)
				return false;

			return true;
		}

		public static bool Kraken_ReadLzTable(int mode,
			byte* src, byte* srcEnd,
			byte* dst, int dstSize, int offset,
			byte* scratch, byte* scratchEnd, KrakenLzTable* lztable)
		{
			byte* outp;
			int decodeCount, n;
			byte* packedOffsStream, packedLenStream;

			if (mode > 1)
				return false;

			if (srcEnd - src < 13)
				return false;

			if (offset == 0)
			{
				Copy64(dst, src);
				dst += 8;
				src += 8;
			}

			if ((*src & 0x80) != 0)
			{
				byte flag = *src++;
				if ((flag & 0xc0) != 0x80)
					return false;

				return false; // excess bytes not supported
			}

			// Disable no-copy optimization if source and dest overlap.
			bool forceCopy = dst <= srcEnd && src <= dst + dstSize;

			outp = scratch;
			n = Kraken_DecodeBytes(ref outp, src, srcEnd, out decodeCount, Min(scratchEnd - scratch, dstSize), forceCopy, scratch, scratchEnd);
			if (n < 0)
				return false;
			src += n;
			lztable->LitStream = outp;
			lztable->LitStreamSize = decodeCount;
			scratch += decodeCount;

			outp = scratch;
			n = Kraken_DecodeBytes(ref outp, src, srcEnd, out decodeCount, Min(scratchEnd - scratch, dstSize), forceCopy, scratch, scratchEnd);
			if (n < 0)
				return false;
			src += n;
			lztable->CmdStream = outp;
			lztable->CmdStreamSize = decodeCount;
			scratch += decodeCount;

			if (srcEnd - src < 3)
				return false;

			int offsScaling = 0;
			byte* packedOffsStreamExtra = null;

			if ((src[0] & 0x80) != 0)
			{
				offsScaling = src[0] - 127;
				src++;

				packedOffsStream = scratch;
				int offsStreamSize;
				n = Kraken_DecodeBytes(ref packedOffsStream, src, srcEnd, out offsStreamSize,
					Min(scratchEnd - scratch, lztable->CmdStreamSize), false, scratch, scratchEnd);
				lztable->OffsStreamSize = offsStreamSize;
				if (n < 0)
					return false;
				src += n;
				scratch += lztable->OffsStreamSize;

				if (offsScaling != 1)
				{
					packedOffsStreamExtra = scratch;
					n = Kraken_DecodeBytes(ref packedOffsStreamExtra, src, srcEnd, out decodeCount,
						Min(scratchEnd - scratch, lztable->OffsStreamSize), false, scratch, scratchEnd);
					if (n < 0 || decodeCount != lztable->OffsStreamSize)
						return false;
					src += n;
					scratch += decodeCount;
				}
			}
			else
			{
				packedOffsStream = scratch;
				int offsStreamSize;
				n = Kraken_DecodeBytes(ref packedOffsStream, src, srcEnd, out offsStreamSize,
					Min(scratchEnd - scratch, lztable->CmdStreamSize), false, scratch, scratchEnd);
				lztable->OffsStreamSize = offsStreamSize;
				if (n < 0)
					return false;
				src += n;
				scratch += lztable->OffsStreamSize;
			}

			packedLenStream = scratch;
			{
				int lenStreamSize;
				n = Kraken_DecodeBytes(ref packedLenStream, src, srcEnd, out lenStreamSize,
					Min(scratchEnd - scratch, dstSize >> 2), false, scratch, scratchEnd);
				lztable->LenStreamSize = lenStreamSize;
				if (n < 0)
					return false;
				src += n;
				scratch += lztable->LenStreamSize;
			}

			scratch = (byte*)(((long)scratch + 15) & ~15L);
			lztable->OffsStream = (int*)scratch;
			scratch += lztable->OffsStreamSize * 4;

			scratch = (byte*)(((long)scratch + 15) & ~15L);
			lztable->LenStream = (int*)scratch;
			scratch += lztable->LenStreamSize * 4;

			if (scratch + 64 > scratchEnd)
				return false;

			return Kraken_UnpackOffsets(src, srcEnd, packedOffsStream, packedOffsStreamExtra,
				lztable->OffsStreamSize, offsScaling,
				packedLenStream, lztable->LenStreamSize,
				lztable->OffsStream, lztable->LenStream);
		}

		// Note: may access memory out of bounds on invalid input (matches the reference).
		public static bool Kraken_ProcessLzRuns_Type0(KrakenLzTable* lzt, byte* dst, byte* dstEnd, byte* dstStart)
		{
			byte* cmdStream = lzt->CmdStream, cmdStreamEnd = cmdStream + lzt->CmdStreamSize;
			int* lenStream = lzt->LenStream;
			int* lenStreamEnd = lzt->LenStream + lzt->LenStreamSize;
			byte* litStream = lzt->LitStream;
			byte* litStreamEnd = lzt->LitStream + lzt->LitStreamSize;
			int* offsStream = lzt->OffsStream;
			int* offsStreamEnd = lzt->OffsStream + lzt->OffsStreamSize;
			byte* copyfrom;
			uint finalLen;
			int offset;
			int* recentOffs = stackalloc int[7];
			int lastOffset;

			recentOffs[3] = -8;
			recentOffs[4] = -8;
			recentOffs[5] = -8;
			lastOffset = -8;

			while (cmdStream < cmdStreamEnd)
			{
				uint f = *cmdStream++;
				uint litlen = f & 3;
				uint offsIndex = f >> 6;
				uint matchlen = (f >> 2) & 0xF;

				uint nextLongLength = (uint)*lenStream;
				int* nextLenStream = lenStream + 1;

				lenStream = (litlen == 3) ? nextLenStream : lenStream;
				litlen = (litlen == 3) ? nextLongLength : litlen;
				recentOffs[6] = *offsStream;

				Copy64Add(dst, litStream, &dst[lastOffset]);
				if (litlen > 8)
				{
					Copy64Add(dst + 8, litStream + 8, &dst[lastOffset + 8]);
					if (litlen > 16)
					{
						Copy64Add(dst + 16, litStream + 16, &dst[lastOffset + 16]);
						if (litlen > 24)
						{
							do
							{
								Copy64Add(dst + 24, litStream + 24, &dst[lastOffset + 24]);
								litlen -= 8;
								dst += 8;
								litStream += 8;
							} while (litlen > 24);
						}
					}
				}
				dst += litlen;
				litStream += litlen;

				offset = recentOffs[offsIndex + 3];
				recentOffs[offsIndex + 3] = recentOffs[offsIndex + 2];
				recentOffs[offsIndex + 2] = recentOffs[offsIndex + 1];
				recentOffs[offsIndex + 1] = recentOffs[offsIndex + 0];
				recentOffs[3] = offset;
				lastOffset = offset;

				offsStream = (int*)((byte*)offsStream + (((offsIndex + 1) & 4) * 1));

				if ((ulong)(long)offset < (ulong)(long)(dstStart - dst))
					return false;

				copyfrom = dst + offset;
				if (matchlen != 15)
				{
					Copy64(dst, copyfrom);
					Copy64(dst + 8, copyfrom + 8);
					dst += matchlen + 2;
				}
				else
				{
					matchlen = (uint)(14 + *lenStream++);
					if ((ulong)matchlen > (ulong)(long)(dstEnd - dst))
						return false;
					Copy64(dst, copyfrom);
					Copy64(dst + 8, copyfrom + 8);
					Copy64(dst + 16, copyfrom + 16);
					do
					{
						Copy64(dst + 24, copyfrom + 24);
						matchlen -= 8;
						dst += 8;
						copyfrom += 8;
					} while (matchlen > 24);
					dst += matchlen;
				}
			}

			if (offsStream != offsStreamEnd || lenStream != lenStreamEnd)
				return false;

			finalLen = (uint)(dstEnd - dst);
			if (finalLen != litStreamEnd - litStream)
				return false;

			if (finalLen >= 8)
			{
				do
				{
					Copy64Add(dst, litStream, &dst[lastOffset]);
					dst += 8; litStream += 8; finalLen -= 8;
				} while (finalLen >= 8);
			}
			if (finalLen > 0)
			{
				do
				{
					*dst = (byte)(*litStream++ + dst[lastOffset]);
					dst++;
				} while (--finalLen != 0);
			}
			return true;
		}

		// Note: may access memory out of bounds on invalid input (matches the reference).
		public static bool Kraken_ProcessLzRuns_Type1(KrakenLzTable* lzt, byte* dst, byte* dstEnd, byte* dstStart)
		{
			byte* cmdStream = lzt->CmdStream, cmdStreamEnd = cmdStream + lzt->CmdStreamSize;
			int* lenStream = lzt->LenStream;
			int* lenStreamEnd = lzt->LenStream + lzt->LenStreamSize;
			byte* litStream = lzt->LitStream;
			byte* litStreamEnd = lzt->LitStream + lzt->LitStreamSize;
			int* offsStream = lzt->OffsStream;
			int* offsStreamEnd = lzt->OffsStream + lzt->OffsStreamSize;
			byte* copyfrom;
			uint finalLen;
			int offset;
			int* recentOffs = stackalloc int[7];

			recentOffs[3] = -8;
			recentOffs[4] = -8;
			recentOffs[5] = -8;

			while (cmdStream < cmdStreamEnd)
			{
				uint f = *cmdStream++;
				uint litlen = f & 3;
				uint offsIndex = f >> 6;
				uint matchlen = (f >> 2) & 0xF;

				uint nextLongLength = (uint)*lenStream;
				int* nextLenStream = lenStream + 1;

				lenStream = (litlen == 3) ? nextLenStream : lenStream;
				litlen = (litlen == 3) ? nextLongLength : litlen;
				recentOffs[6] = *offsStream;

				Copy64(dst, litStream);
				if (litlen > 8)
				{
					Copy64(dst + 8, litStream + 8);
					if (litlen > 16)
					{
						Copy64(dst + 16, litStream + 16);
						if (litlen > 24)
						{
							do
							{
								Copy64(dst + 24, litStream + 24);
								litlen -= 8;
								dst += 8;
								litStream += 8;
							} while (litlen > 24);
						}
					}
				}
				dst += litlen;
				litStream += litlen;

				offset = recentOffs[offsIndex + 3];
				recentOffs[offsIndex + 3] = recentOffs[offsIndex + 2];
				recentOffs[offsIndex + 2] = recentOffs[offsIndex + 1];
				recentOffs[offsIndex + 1] = recentOffs[offsIndex + 0];
				recentOffs[3] = offset;

				offsStream = (int*)((byte*)offsStream + (((offsIndex + 1) & 4) * 1));

				if ((ulong)(long)offset < (ulong)(long)(dstStart - dst))
					return false;

				copyfrom = dst + offset;
				if (matchlen != 15)
				{
					Copy64(dst, copyfrom);
					Copy64(dst + 8, copyfrom + 8);
					dst += matchlen + 2;
				}
				else
				{
					matchlen = (uint)(14 + *lenStream++);
					if ((ulong)matchlen > (ulong)(long)(dstEnd - dst))
						return false;
					Copy64(dst, copyfrom);
					Copy64(dst + 8, copyfrom + 8);
					Copy64(dst + 16, copyfrom + 16);
					do
					{
						Copy64(dst + 24, copyfrom + 24);
						matchlen -= 8;
						dst += 8;
						copyfrom += 8;
					} while (matchlen > 24);
					dst += matchlen;
				}
			}

			if (offsStream != offsStreamEnd || lenStream != lenStreamEnd)
				return false;

			finalLen = (uint)(dstEnd - dst);
			if (finalLen != litStreamEnd - litStream)
				return false;

			if (finalLen >= 64)
			{
				do
				{
					Copy64Bytes(dst, litStream);
					dst += 64; litStream += 64; finalLen -= 64;
				} while (finalLen >= 64);
			}
			if (finalLen >= 8)
			{
				do
				{
					Copy64(dst, litStream);
					dst += 8; litStream += 8; finalLen -= 8;
				} while (finalLen >= 8);
			}
			if (finalLen > 0)
			{
				do
				{
					*dst++ = *litStream++;
				} while (--finalLen != 0);
			}
			return true;
		}

		public static bool Kraken_ProcessLzRuns(int mode, byte* dst, int dstSize, int offset, KrakenLzTable* lztable)
		{
			byte* dstEnd = dst + dstSize;

			if (mode == 1)
				return Kraken_ProcessLzRuns_Type1(lztable, dst + (offset == 0 ? 8 : 0), dstEnd, dst - offset);

			if (mode == 0)
				return Kraken_ProcessLzRuns_Type0(lztable, dst + (offset == 0 ? 8 : 0), dstEnd, dst - offset);

			return false;
		}

		// Decode one 256kb quantum. It's divided into two 128k blocks internally that are
		// compressed separately but with a shared history.
		public static int Kraken_DecodeQuantum(byte* dst, byte* dstEnd, byte* dstStart,
			byte* src, byte* srcEnd,
			byte* scratch, byte* scratchEnd)
		{
			byte* srcIn = src;
			int mode, chunkhdr, dstCount, srcUsed, writtenBytes;

			while (dstEnd - dst != 0)
			{
				dstCount = (int)(dstEnd - dst);
				if (dstCount > 0x20000) dstCount = 0x20000;
				if (srcEnd - src < 4)
					return -1;
				chunkhdr = src[2] | (src[1] << 8) | (src[0] << 16);
				if ((chunkhdr & 0x800000) == 0)
				{
					// Stored as entropy without any match copying.
					byte* outp = dst;
					srcUsed = Kraken_DecodeBytes(ref outp, src, srcEnd, out writtenBytes, dstCount, false, scratch, scratchEnd);
					if (srcUsed < 0 || writtenBytes != dstCount)
						return -1;
				}
				else
				{
					src += 3;
					srcUsed = chunkhdr & 0x7FFFF;
					mode = (chunkhdr >> 19) & 0xF;
					if (srcEnd - src < srcUsed)
						return -1;
					if (srcUsed < dstCount)
					{
						long scratchUsage = Min(Min(3L * dstCount + 32 + 0xd000, 0x6C000), scratchEnd - scratch);
						if (scratchUsage < sizeof(KrakenLzTable))
							return -1;
						KrakenLzTable lzTable = default;
						if (!Kraken_ReadLzTable(mode,
								src, src + srcUsed,
								dst, dstCount,
								(int)(dst - dstStart),
								scratch + sizeof(KrakenLzTable), scratch + scratchUsage,
								&lzTable))
							return -1;
						if (!Kraken_ProcessLzRuns(mode, dst, dstCount, (int)(dst - dstStart), &lzTable))
							return -1;
					}
					else if (srcUsed > dstCount || mode != 0)
					{
						return -1;
					}
					else
					{
						for (int i = 0; i < dstCount; i++)
							dst[i] = src[i];
					}
				}
				src += srcUsed;
				dst += dstCount;
			}
			return (int)(src - srcIn);
		}
	}
}
