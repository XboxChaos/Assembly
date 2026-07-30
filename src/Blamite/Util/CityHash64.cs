using System;

namespace Blamite.Util
{
	/// <summary>
	///     A from-scratch implementation of the 64-bit CityHash algorithm (the "v1.1" revision:
	///     the one still shipped at the head of google/cityhash, and the one Unreal Engine's own
	///     <c>Core/Private/Hash/CityHash.cpp</c> is a port of).
	/// </summary>
	/// <remarks>
	///     <para>
	///         Unreal's IoStore identifies a package by a 64-bit <c>FPackageId</c>, computed as
	///         <c>CityHash64</c> of the package's full path, lowercased and encoded as UTF-16LE with
	///         no terminator (see <see cref="Compute(string)" />). Campaign Evolved tags are Blam data
	///         wrapped in a UE package under <c>/Game/Tags/...</c>, so recovering a tag's package ID
	///         from its path - or confirming a path guess against an observed ID - both go through
	///         this hash.
	///     </para>
	///     <para>
	///         This is a generic, publicly documented hashing algorithm with no connection to
	///         Campaign Evolved's on-disk format; it carries none of the clean-room restrictions
	///         that apply to the handful of unlicensed community repos describing that format.
	///     </para>
	///     <para>
	///         Verified against one real value pulled from a shipped container: hashing the lowercase
	///         UTF-16LE form of
	///         <c>/Game/Tags/objects/vehicles/human/pelican/attachments/pelican_chin_gun/weapons/pelican_chin_gun-weapon</c>
	///         reproduces the package ID <c>0x1FBC7BBC2072A331</c> found in a real mod's
	///         <c>pelican_chin_gun-weapon_P.utoc</c>.
	///     </para>
	/// </remarks>
	public static class CityHash64
	{
		private const ulong K0 = 0xc3a5c85c97cb3127UL;
		private const ulong K1 = 0xb492b66fbe98f273UL;
		private const ulong K2 = 0x9ae16a3b2f90404fUL;

		/// <summary>
		///     Computes the <c>FPackageId</c> of a UE package name: CityHash64 of the name, lowercased
		///     (ASCII range only, matching Unreal's own case folding) and encoded as UTF-16LE with no
		///     null terminator.
		/// </summary>
		/// <param name="packageName">
		///     The full package name, e.g.
		///     <c>/Game/Tags/objects/characters/spartans/spartans-biped</c>.
		/// </param>
		/// <returns>The package's 64-bit ID.</returns>
		public static ulong Compute(string packageName)
		{
			if (packageName == null)
				throw new ArgumentNullException("packageName");

			var lowered = new char[packageName.Length];
			for (int i = 0; i < packageName.Length; i++)
			{
				char c = packageName[i];

				// Unreal's own FPackageId::FromName only folds plain ASCII 'A'-'Z' - not the full
				// Unicode-aware ToLower() - so matching that exactly is what makes package names
				// with no non-ASCII characters (every one Campaign Evolved ships) hash correctly.
				lowered[i] = (c >= 'A' && c <= 'Z') ? (char) (c + 32) : c;
			}

			var bytes = new byte[lowered.Length*2];
			for (int i = 0; i < lowered.Length; i++)
			{
				bytes[i*2] = (byte) lowered[i];
				bytes[i*2 + 1] = (byte) (lowered[i] >> 8);
			}
			return Compute(bytes);
		}

		/// <summary>
		///     Computes the CityHash64 of a byte buffer.
		/// </summary>
		/// <param name="data">The buffer to hash.</param>
		/// <returns>The buffer's 64-bit hash.</returns>
		public static ulong Compute(byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			return Compute(data, 0, data.Length);
		}

		/// <summary>
		///     Computes the CityHash64 of a range of a byte buffer.
		/// </summary>
		/// <param name="data">The buffer to hash.</param>
		/// <param name="offset">The offset of the range to hash.</param>
		/// <param name="length">The length of the range to hash.</param>
		/// <returns>The range's 64-bit hash.</returns>
		public static ulong Compute(byte[] data, int offset, int length)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			if (offset < 0 || length < 0 || offset + length > data.Length)
				throw new ArgumentOutOfRangeException("length");

			if (length <= 32)
				return length <= 16 ? HashLen0To16(data, offset, length) : HashLen17To32(data, offset, length);
			if (length <= 64)
				return HashLen33To64(data, offset, length);
			return HashLong(data, offset, length);
		}

		private static uint Fetch32(byte[] s, int o)
		{
			// Explicit little-endian assembly rather than BitConverter, which follows host
			// endianness - every other multi-byte read in this codebase is just as deliberate
			// about not trusting that.
			return (uint) (s[o] | (s[o + 1] << 8) | (s[o + 2] << 16) | (s[o + 3] << 24));
		}

		private static ulong Fetch64(byte[] s, int o)
		{
			uint lo = Fetch32(s, o);
			uint hi = Fetch32(s, o + 4);
			return lo | ((ulong) hi << 32);
		}

		private static ulong Rotate(ulong val, int shift)
		{
			return shift == 0 ? val : ((val >> shift) | (val << (64 - shift)));
		}

		private static ulong ShiftMix(ulong val)
		{
			return val ^ (val >> 47);
		}

		private static ulong Bswap64(ulong val)
		{
			val = ((val << 8) & 0xFF00FF00FF00FF00UL) | ((val >> 8) & 0x00FF00FF00FF00FFUL);
			val = ((val << 16) & 0xFFFF0000FFFF0000UL) | ((val >> 16) & 0x0000FFFF0000FFFFUL);
			return (val << 32) | (val >> 32);
		}

		/// <summary>
		///     Combines two 64-bit values into one, the same way a 128-bit CityHash result is folded
		///     down to 64 bits.
		/// </summary>
		private static ulong Hash128To64(ulong low, ulong high)
		{
			const ulong kMul = 0x9ddfea08eb382d69UL;
			ulong a = (low ^ high)*kMul;
			a ^= (a >> 47);
			ulong b = (high ^ a)*kMul;
			b ^= (b >> 47);
			b *= kMul;
			return b;
		}

		private static ulong HashLen16(ulong u, ulong v)
		{
			return Hash128To64(u, v);
		}

		private static ulong HashLen16(ulong u, ulong v, ulong mul)
		{
			ulong a = (u ^ v)*mul;
			a ^= (a >> 47);
			ulong b = (v ^ a)*mul;
			b ^= (b >> 47);
			b *= mul;
			return b;
		}

		private static ulong HashLen0To16(byte[] s, int o, int len)
		{
			if (len >= 8)
			{
				ulong mul = K2 + (ulong) len*2;
				ulong a = Fetch64(s, o) + K2;
				ulong b = Fetch64(s, o + len - 8);
				ulong c = Rotate(b, 37)*mul + a;
				ulong d = (Rotate(a, 25) + b)*mul;
				return HashLen16(c, d, mul);
			}
			if (len >= 4)
			{
				ulong mul = K2 + (ulong) len*2;
				ulong a = Fetch32(s, o);
				return HashLen16((ulong) len + (a << 3), Fetch32(s, o + len - 4), mul);
			}
			if (len > 0)
			{
				byte a = s[o];
				byte b = s[o + (len >> 1)];
				byte c = s[o + len - 1];
				var y = (uint) a + ((uint) b << 8);
				var z = (uint) len + ((uint) c << 2);
				return ShiftMix((y*K2) ^ (z*K0))*K2;
			}
			return K2;
		}

		private static ulong HashLen17To32(byte[] s, int o, int len)
		{
			ulong mul = K2 + (ulong) len*2;
			ulong a = Fetch64(s, o)*K1;
			ulong b = Fetch64(s, o + 8);
			ulong c = Fetch64(s, o + len - 8)*mul;
			ulong d = Fetch64(s, o + len - 16)*K2;
			return HashLen16(Rotate(a + b, 43) + Rotate(c, 30) + d, a + Rotate(b + K2, 18) + c, mul);
		}

		private static ulong HashLen33To64(byte[] s, int o, int len)
		{
			ulong mul = K2 + (ulong) len*2;
			ulong a = Fetch64(s, o)*K2;
			ulong b = Fetch64(s, o + 8);
			ulong c = Fetch64(s, o + len - 24);
			ulong d = Fetch64(s, o + len - 32);
			ulong e = Fetch64(s, o + 16)*K2;
			ulong f = Fetch64(s, o + 24)*9;
			ulong g = Fetch64(s, o + len - 8);
			ulong h = Fetch64(s, o + len - 16)*mul;

			ulong u = Rotate(a + g, 43) + (Rotate(b, 30) + c)*9;
			ulong v = ((a + g) ^ d) + f + 1;
			ulong w = Bswap64((u + v)*mul) + h;
			ulong x = Rotate(e + f, 42) + c;
			ulong y = (Bswap64((v + w)*mul) + g)*mul;
			ulong z = e + f + c;
			a = Bswap64((x + z)*mul + y) + b;
			b = ShiftMix((z + a)*mul + d + h)*mul;
			return b + x;
		}

		/// <summary>
		///     A pair of 64-bit hash accumulators, as produced by <see cref="WeakHashLen32WithSeeds" />.
		/// </summary>
		private struct HashPair
		{
			public ulong First;
			public ulong Second;

			public HashPair(ulong first, ulong second)
			{
				First = first;
				Second = second;
			}
		}

		private static HashPair WeakHashLen32WithSeeds(ulong w, ulong x, ulong y, ulong z, ulong a, ulong b)
		{
			a += w;
			b = Rotate(b + a + z, 21);
			ulong c = a;
			a += x;
			a += y;
			b += Rotate(a, 44);
			return new HashPair(a + z, b + c);
		}

		private static HashPair WeakHashLen32WithSeeds(byte[] s, int o, ulong a, ulong b)
		{
			return WeakHashLen32WithSeeds(Fetch64(s, o), Fetch64(s, o + 8), Fetch64(s, o + 16), Fetch64(s, o + 24), a, b);
		}

		/// <summary>
		///     Hashes buffers longer than 64 bytes, which no Campaign Evolved package name is ever
		///     going to be, but which the algorithm still has to handle correctly to be CityHash64 at
		///     all.
		/// </summary>
		private static ulong HashLong(byte[] s, int o, int len)
		{
			ulong x = Fetch64(s, o + len - 40);
			ulong y = Fetch64(s, o + len - 16) + Fetch64(s, o + len - 56);
			ulong z = HashLen16(Fetch64(s, o + len - 48) + (ulong) len, Fetch64(s, o + len - 24));
			HashPair v = WeakHashLen32WithSeeds(s, o + len - 64, (ulong) len, z);
			HashPair w = WeakHashLen32WithSeeds(s, o + len - 32, y + K1, x);
			x = x*K1 + Fetch64(s, o);

			int remaining = (len - 1) & ~63;
			int pos = o;
			do
			{
				x = Rotate(x + y + v.First + Fetch64(s, pos + 8), 37)*K1;
				y = Rotate(y + v.Second + Fetch64(s, pos + 48), 42)*K1;
				x ^= w.Second;
				y += v.First + Fetch64(s, pos + 40);
				z = Rotate(z + w.First, 33)*K1;
				v = WeakHashLen32WithSeeds(s, pos, v.Second*K1, x + w.First);
				w = WeakHashLen32WithSeeds(s, pos + 32, z + w.Second, y + Fetch64(s, pos + 16));

				ulong t = z;
				z = x;
				x = t;

				pos += 64;
				remaining -= 64;
			} while (remaining != 0);

			return HashLen16(HashLen16(v.First, w.First) + ShiftMix(y)*K1 + z, HashLen16(v.Second, w.Second) + x);
		}
	}
}
