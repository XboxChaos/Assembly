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
	/// Thrown when Oodle-compressed data cannot be decoded, either because it is malformed
	/// or because it uses a sub-codec this decoder does not implement.
	/// </summary>
	internal class OodleDecoderException : Exception
	{
		public OodleDecoderException(string message) : base(message) { }
	}

	// Header in front of each 256k block.
	internal struct KrakenHeader
	{
		// Type of decoder used, 6 means kraken.
		public int DecoderType;

		// Whether to restart the decoder.
		public bool RestartDecoder;

		// Whether this block is uncompressed.
		public bool Uncompressed;

		// Whether this block uses checksums.
		public bool UseChecksums;
	}

	// Additional header in front of each 256k block ("quantum").
	internal struct KrakenQuantumHeader
	{
		// The compressed size of this quantum. If this value is 0 it means
		// the quantum is a special quantum such as memset.
		public uint CompressedSize;
		// If checksums are enabled, holds the checksum.
		public uint Checksum;
		// Two flags.
		public byte Flag1;
		public byte Flag2;
		// Whether the whole block matched a previous block.
		public uint WholeMatchDistance;
	}

	// Kraken decompression happens in two phases: first it decodes all the literals
	// and copy lengths using huffman/tANS, and the second phase runs the copy loop.
	// This holds the pointers/lengths needed by phase 2. All pointers point into the
	// shared scratch buffer for the current decode call.
	internal unsafe struct KrakenLzTable
	{
		public byte* CmdStream;
		public int CmdStreamSize;

		public int* OffsStream;
		public int OffsStreamSize;

		public byte* LitStream;
		public int LitStreamSize;

		public int* LenStream;
		public int LenStreamSize;
	}
}
