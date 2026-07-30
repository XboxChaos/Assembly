// Ported from powzix/ooz (https://github.com/powzix/ooz), a from-scratch reimplementation
// of RAD Game Tools/Epic's Oodle Kraken decompressor. Original C++ source copyright (C)
// 2016, Powzix, licensed GNU GPL v3 (or later); ported to C# here under the same terms,
// which is compatible with Assembly/Blamite's own GPLv3 license (see the LICENSE file at
// the repository root). This only implements decompression of the "Kraken" sub-codec
// (decoder_type 6); Mermaid/Selkie, BitKnit and Leviathan are not ported.

using System;

namespace Blamite.Compression.Oodle
{
	/// <summary>
	/// Forward (or backward) MSB-first bit reader over a raw byte buffer. Ported from
	/// powzix/ooz's BitReader (kraken.cpp). Bits accumulate in <see cref="Bits"/> with the
	/// next byte landing at bit position <see cref="Bitpos"/>; refilling keeps at least 24
	/// bits available so callers can consume up to 24 bits without an intermediate refill.
	/// </summary>
	internal unsafe class BitReader
	{
		public byte* P;
		public byte* PEnd;
		public uint Bits;
		public int Bitpos;

		public void Refill()
		{
			while (Bitpos > 0)
			{
				Bits |= (uint)(P < PEnd ? *P : 0) << Bitpos;
				Bitpos -= 8;
				P++;
			}
		}

		public void RefillBackwards()
		{
			while (Bitpos > 0)
			{
				P--;
				Bits |= (uint)(P >= PEnd ? *P : 0) << Bitpos;
				Bitpos -= 8;
			}
		}

		public int ReadBit()
		{
			Refill();
			int r = (int)(Bits >> 31);
			Bits <<= 1;
			Bitpos += 1;
			return r;
		}

		public int ReadBitNoRefill()
		{
			int r = (int)(Bits >> 31);
			Bits <<= 1;
			Bitpos += 1;
			return r;
		}

		public int ReadBitsNoRefill(int n)
		{
			int r = (int)(Bits >> (32 - n));
			Bits <<= n;
			Bitpos += n;
			return r;
		}

		public int ReadBitsNoRefillZero(int n)
		{
			int r = (int)((Bits >> 1) >> (31 - n));
			Bits <<= n;
			Bitpos += n;
			return r;
		}

		public uint ReadMoreThan24Bits(int n)
		{
			uint rv;
			if (n <= 24)
			{
				rv = (uint)ReadBitsNoRefillZero(n);
			}
			else
			{
				rv = (uint)ReadBitsNoRefill(24) << (n - 24);
				Refill();
				rv += (uint)ReadBitsNoRefill(n - 24);
			}
			Refill();
			return rv;
		}

		public uint ReadMoreThan24BitsB(int n)
		{
			uint rv;
			if (n <= 24)
			{
				rv = (uint)ReadBitsNoRefillZero(n);
			}
			else
			{
				rv = (uint)ReadBitsNoRefill(24) << (n - 24);
				RefillBackwards();
				rv += (uint)ReadBitsNoRefill(n - 24);
			}
			RefillBackwards();
			return rv;
		}

		// Reads a gamma value. Assumes bitreader is already filled with at least 23 bits.
		public int ReadGamma()
		{
			int n;
			if (Bits != 0)
			{
				n = 31 - BitUtil.Bsr(Bits);
			}
			else
			{
				n = 32;
			}
			n = 2 * n + 2;
			Bitpos += n;
			int r = (int)(Bits >> (32 - n));
			Bits <<= n;
			return r - 2;
		}

		// Reads a gamma value with |forced| number of forced bits.
		public int ReadGammaX(int forced)
		{
			if (Bits != 0)
			{
				int lz = 31 - BitUtil.Bsr(Bits);
				int r = (int)(Bits >> (31 - lz - forced)) + ((lz - 1) << forced);
				Bits <<= lz + forced + 1;
				Bitpos += lz + forced + 1;
				return r;
			}
			return 0;
		}

		// Reads an offset code parametrized by |v|.
		public uint ReadDistance(uint v)
		{
			uint w, m, n, rv;
			if (v < 0xF0)
			{
				n = (v >> 4) + 4;
				w = BitUtil.Rotl(Bits | 1, (int)n);
				Bitpos += (int)n;
				m = (2u << (int)n) - 1;
				Bits = w & ~m;
				rv = ((w & m) << 4) + (v & 0xF) - 248;
			}
			else
			{
				n = v - 0xF0 + 4;
				w = BitUtil.Rotl(Bits | 1, (int)n);
				Bitpos += (int)n;
				m = (2u << (int)n) - 1;
				Bits = w & ~m;
				rv = 8322816 + ((w & m) << 12);
				Refill();
				rv += Bits >> 20;
				Bitpos += 12;
				Bits <<= 12;
			}
			Refill();
			return rv;
		}

		// Reads an offset code parametrized by |v|, backwards.
		public uint ReadDistanceB(uint v)
		{
			uint w, m, n, rv;
			if (v < 0xF0)
			{
				n = (v >> 4) + 4;
				w = BitUtil.Rotl(Bits | 1, (int)n);
				Bitpos += (int)n;
				m = (2u << (int)n) - 1;
				Bits = w & ~m;
				rv = ((w & m) << 4) + (v & 0xF) - 248;
			}
			else
			{
				n = v - 0xF0 + 4;
				w = BitUtil.Rotl(Bits | 1, (int)n);
				Bitpos += (int)n;
				m = (2u << (int)n) - 1;
				Bits = w & ~m;
				rv = 8322816 + ((w & m) << 12);
				RefillBackwards();
				rv += Bits >> (32 - 12);
				Bitpos += 12;
				Bits <<= 12;
			}
			RefillBackwards();
			return rv;
		}

		// Reads a length code.
		public bool ReadLength(out uint v)
		{
			int n = 31 - BitUtil.Bsr(Bits);
			if (n > 12)
			{
				v = 0;
				return false;
			}
			Bitpos += n;
			Bits <<= n;
			Refill();
			n += 7;
			Bitpos += n;
			uint rv = (Bits >> (32 - n)) - 64;
			Bits <<= n;
			v = rv;
			Refill();
			return true;
		}

		// Reads a length code, backwards.
		public bool ReadLengthB(out uint v)
		{
			int n = 31 - BitUtil.Bsr(Bits);
			if (n > 12)
			{
				v = 0;
				return false;
			}
			Bitpos += n;
			Bits <<= n;
			RefillBackwards();
			n += 7;
			Bitpos += n;
			uint rv = (Bits >> (32 - n)) - 64;
			Bits <<= n;
			v = rv;
			RefillBackwards();
			return true;
		}

		public int ReadFluff(int numSymbols)
		{
			if (numSymbols == 256)
				return 0;

			int x = 257 - numSymbols;
			if (x > numSymbols)
				x = numSymbols;

			x *= 2;

			int y = BitUtil.Bsr((uint)(x - 1)) + 1;

			uint v = Bits >> (32 - y);
			uint z = (1u << y) - (uint)x;

			if ((v >> 1) >= z)
			{
				Bits <<= y;
				Bitpos += y;
				return (int)(v - z);
			}
			else
			{
				Bits <<= (y - 1);
				Bitpos += (y - 1);
				return (int)(v >> 1);
			}
		}
	}

	// A second, simpler bit reader used by the Golomb-Rice code length decoder.
	// Reads whole bytes from |P| into a running window based on |Bitpos| (0-7).
	internal unsafe struct BitReader2
	{
		public byte* P;
		public byte* PEnd;
		public uint Bitpos;
	}

	// Manual, dependency-free bit tricks. Deliberately avoids System.Numerics.BitOperations
	// and System.Buffers.Binary.BinaryPrimitives: neither is available on net48 without
	// pulling in an extra package, and this project multi-targets net48;net10.0.
	internal static class BitUtil
	{
		// Index (0-31) of the highest set bit. Mirrors _BitScanReverse. Undefined for 0,
		// same as the intrinsic it replaces -- callers always guard against a zero input.
		public static int Bsr(uint value)
		{
			int result = 0;
			if ((value & 0xFFFF0000u) != 0) { value >>= 16; result += 16; }
			if ((value & 0xFF00u) != 0) { value >>= 8; result += 8; }
			if ((value & 0xF0u) != 0) { value >>= 4; result += 4; }
			if ((value & 0xCu) != 0) { value >>= 2; result += 2; }
			if ((value & 0x2u) != 0) { result += 1; }
			return result;
		}

		// Index (0-31) of the lowest set bit. Mirrors _BitScanForward. Undefined for 0.
		public static int Bsf(uint value)
		{
			int result = 0;
			if ((value & 0xFFFFu) == 0) { value >>= 16; result += 16; }
			if ((value & 0xFFu) == 0) { value >>= 8; result += 8; }
			if ((value & 0xFu) == 0) { value >>= 4; result += 4; }
			if ((value & 0x3u) == 0) { value >>= 2; result += 2; }
			if ((value & 0x1u) == 0) { result += 1; }
			return result;
		}

		// Mirrors the x86 _rotl intrinsic. C#'s shift operators already mask the shift
		// count to 0-31 for a uint operand, which is what makes shift == 0 behave (32 masks
		// down to 0, so the "or" term degenerates to `value`, matching the ROL instruction).
		public static uint Rotl(uint value, int shift)
		{
			return (value << shift) | (value >> (32 - shift));
		}

		public static uint ByteSwap(uint v)
		{
			return (v >> 24) | ((v >> 8) & 0x0000FF00u) | ((v << 8) & 0x00FF0000u) | (v << 24);
		}

		public static ulong ByteSwap(ulong v)
		{
			return ((ulong)ByteSwap((uint)v) << 32) | ByteSwap((uint)(v >> 32));
		}

		public static ushort ByteSwap(ushort v)
		{
			return (ushort)((v >> 8) | (v << 8));
		}

		public static int CountLeadingZeros(uint bits)
		{
			return 31 - Bsr(bits);
		}
	}
}
