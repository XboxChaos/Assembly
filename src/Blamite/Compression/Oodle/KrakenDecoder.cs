// Ported from powzix/ooz (https://github.com/powzix/ooz), a from-scratch reimplementation
// of RAD Game Tools/Epic's Oodle Kraken decompressor. Original C++ source copyright (C)
// 2016, Powzix, licensed GNU GPL v3 (or later); ported to C# here under the same terms,
// which is compatible with Assembly/Blamite's own GPLv3 license (see the LICENSE file at
// the repository root). This only implements decompression of the "Kraken" sub-codec
// (decoder_type 6); Mermaid/Selkie, BitKnit and Leviathan are not ported.

using System;

namespace Blamite.Compression.Oodle
{
	// Ported from powzix/ooz's kraken.cpp: per-block header parsing and the top-level
	// Kraken_DecodeStep/Kraken_Decompress driver loop. Only decoder_type 6 ("Kraken" proper)
	// is implemented; the other Oodle LZ family members that share this same outer framing
	// (Mermaid/Selkie = 10, BitKnit = 11, Leviathan = 12, LZNA = 5) are not, and are reported
	// via a clear exception rather than silently mis-decoded.
	internal static unsafe class KrakenDecoderState
	{
		private const int ScratchSize = 0x6C000;

		private static byte* ParseHeader(byte* p, byte* pEnd, out KrakenHeader hdr)
		{
			hdr = default;
			if (pEnd - p < 2)
				return null;
			int b = p[0];
			if ((b & 0xF) == 0xC)
			{
				if (((b >> 4) & 3) != 0)
					return null;
				hdr.RestartDecoder = ((b >> 7) & 1) != 0;
				hdr.Uncompressed = ((b >> 6) & 1) != 0;
				b = p[1];
				hdr.DecoderType = b & 0x7F;
				hdr.UseChecksums = (b >> 7) != 0;
				if (hdr.DecoderType != 6 && hdr.DecoderType != 10 && hdr.DecoderType != 5 && hdr.DecoderType != 11 && hdr.DecoderType != 12)
					return null;
				return p + 2;
			}
			return null;
		}

		private static byte* ParseQuantumHeader(byte* p, byte* pEnd, bool useChecksum, out KrakenQuantumHeader hdr)
		{
			hdr = default;
			if (pEnd - p < 3)
				return null;
			uint v = (uint)((p[0] << 16) | (p[1] << 8) | p[2]);
			uint size = v & 0x3FFFF;
			if (size != 0x3ffff)
			{
				hdr.CompressedSize = size + 1;
				hdr.Flag1 = (byte)((v >> 18) & 1);
				hdr.Flag2 = (byte)((v >> 19) & 1);
				if (useChecksum)
				{
					if (pEnd - p < 6)
						return null;
					hdr.Checksum = (uint)((p[3] << 16) | (p[4] << 8) | p[5]);
					return p + 6;
				}
				return p + 3;
			}
			v >>= 18;
			if (v == 1)
			{
				if (pEnd - p < 4)
					return null;
				hdr.Checksum = p[3];
				hdr.CompressedSize = 0;
				hdr.WholeMatchDistance = 0;
				return p + 4;
			}
			return null;
		}

		// Kraken_GetCrc is a stub in the upstream reference decoder (it always returns 0),
		// so checksum verification never actually validates against real content there
		// either. Matched here rather than guessing at an undocumented CRC variant: if a
		// container enables per-quantum checksums with a non-zero checksum, decoding will
		// fail fast (same as it would against the reference).
		private static uint GetCrc(byte* p, long pSize) => 0;

		private static void CopyWholeMatch(byte* dst, uint offset, long length)
		{
			long i = 0;
			byte* src = dst - offset;
			if (offset >= 8)
			{
				for (; i + 8 <= length; i += 8)
					*(ulong*)(dst + i) = *(ulong*)(src + i);
			}
			for (; i < length; i++)
				dst[i] = src[i];
		}

		private static string DecoderTypeName(int decoderType)
		{
			switch (decoderType)
			{
				case 5: return "LZNA";
				case 10: return "Mermaid/Selkie";
				case 11: return "BitKnit";
				case 12: return "Leviathan";
				default: return "type " + decoderType;
			}
		}

		private class State
		{
			public byte* Scratch;
			public long ScratchSize;
			public KrakenHeader Hdr;
			public int SrcUsed, DstUsed;
		}

		private static bool DecodeStep(State dec, byte* dstStart, int offset, long dstBytesLeftIn, byte* src, long srcBytesLeft)
		{
			byte* srcIn = src;
			byte* srcEnd = src + srcBytesLeft;
			KrakenQuantumHeader qhdr;

			if ((offset & 0x3FFFF) == 0)
			{
				byte* newSrc = ParseHeader(src, srcEnd, out dec.Hdr);
				if (newSrc == null)
					throw new OodleDecoderException("Kraken: malformed per-block header.");
				src = newSrc;
			}

			if (dec.Hdr.DecoderType != 6)
				throw new OodleDecoderException(
					"Oodle sub-codec " + DecoderTypeName(dec.Hdr.DecoderType) + " is not implemented by this decoder (only Kraken/decoder_type 6 is supported).");

			int dstBytesLeft = (int)Math.Min(0x40000L, dstBytesLeftIn);

			if (dec.Hdr.Uncompressed)
			{
				if (srcEnd - src < dstBytesLeft)
				{
					dec.SrcUsed = dec.DstUsed = 0;
					return true;
				}
				for (int i = 0; i < dstBytesLeft; i++)
					(dstStart + offset)[i] = src[i];
				dec.SrcUsed = (int)(src - srcIn) + dstBytesLeft;
				dec.DstUsed = dstBytesLeft;
				return true;
			}

			byte* afterQuantum = ParseQuantumHeader(src, srcEnd, dec.Hdr.UseChecksums, out qhdr);
			if (afterQuantum == null || afterQuantum > srcEnd)
				throw new OodleDecoderException("Kraken: malformed quantum header.");
			src = afterQuantum;

			if (srcEnd - src < qhdr.CompressedSize)
			{
				dec.SrcUsed = dec.DstUsed = 0;
				return true;
			}

			if (qhdr.CompressedSize > (uint)dstBytesLeft)
				throw new OodleDecoderException("Kraken: quantum compressed size exceeds remaining destination space.");

			if (qhdr.CompressedSize == 0)
			{
				if (qhdr.WholeMatchDistance != 0)
				{
					if (qhdr.WholeMatchDistance > (uint)offset)
						throw new OodleDecoderException("Kraken: whole-match distance exceeds current offset.");
					CopyWholeMatch(dstStart + offset, qhdr.WholeMatchDistance, dstBytesLeft);
				}
				else
				{
					for (int i = 0; i < dstBytesLeft; i++)
						(dstStart + offset)[i] = (byte)qhdr.Checksum;
				}
				dec.SrcUsed = (int)(src - srcIn);
				dec.DstUsed = dstBytesLeft;
				return true;
			}

			if (dec.Hdr.UseChecksums && (GetCrc(src, qhdr.CompressedSize) & 0xFFFFFF) != qhdr.Checksum)
				throw new OodleDecoderException("Kraken: quantum checksum mismatch.");

			if (qhdr.CompressedSize == dstBytesLeft)
			{
				for (int i = 0; i < dstBytesLeft; i++)
					(dstStart + offset)[i] = src[i];
				dec.SrcUsed = (int)(src - srcIn) + dstBytesLeft;
				dec.DstUsed = dstBytesLeft;
				return true;
			}

			int n = KrakenCore.Kraken_DecodeQuantum(dstStart + offset, dstStart + offset + dstBytesLeft, dstStart,
				src, src + qhdr.CompressedSize,
				dec.Scratch, dec.Scratch + dec.ScratchSize);

			if (n != qhdr.CompressedSize)
				throw new OodleDecoderException("Kraken: quantum decode consumed an unexpected number of source bytes (corrupt input?).");

			dec.SrcUsed = (int)(src - srcIn) + n;
			dec.DstUsed = dstBytesLeft;
			return true;
		}

		/// <summary>
		/// Decompresses a raw Kraken bitstream (no outer framing/size header -- exactly the
		/// bytes as they appear in an IoStore compression block) into a buffer of exactly
		/// <paramref name="dst"/>.Length bytes.
		/// </summary>
		public static void Decompress(byte* src, long srcLen, byte* dst, long dstLen)
		{
			// The scratch working area is ~432 KiB -- heap-allocate it (matching the
			// reference's malloc'd Kraken_Create scratch buffer) rather than stackalloc,
			// which would risk a stack overflow. It is allocated fresh per call and
			// discarded afterwards, so decoding stays stateless across calls/blocks.
			byte[] scratchArray = new byte[ScratchSize];
			fixed (byte* scratch = scratchArray)
			{
				var dec = new State
				{
					Scratch = scratch,
					ScratchSize = ScratchSize,
				};

				int offset = 0;
				long remainingDst = dstLen;
				long remainingSrc = srcLen;
				byte* srcCur = src;

				while (remainingDst != 0)
				{
					if (!DecodeStep(dec, dst, offset, remainingDst, srcCur, remainingSrc))
						throw new OodleDecoderException("Kraken: decode step failed.");
					if (dec.SrcUsed == 0)
						throw new OodleDecoderException("Kraken: decoder made no forward progress (truncated input?).");
					srcCur += dec.SrcUsed;
					remainingSrc -= dec.SrcUsed;
					remainingDst -= dec.DstUsed;
					offset += dec.DstUsed;
				}
				if (remainingSrc != 0)
					throw new OodleDecoderException("Kraken: trailing bytes left over after decoding (corrupt input?).");
			}
		}
	}
}
