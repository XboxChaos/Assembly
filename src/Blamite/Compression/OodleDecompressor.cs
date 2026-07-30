using System;
using Blamite.Compression.Oodle;

namespace Blamite.Compression
{
	/// <summary>
	/// Decompresses Oodle-compressed blocks from Unreal Engine 5 IoStore containers (used by
	/// Halo: Campaign Evolved's shipped-game .utoc/.ucas pairs). Mod override containers are
	/// entirely uncompressed and never reach this class; shipped game containers name their
	/// compression method "Oodle" in the container's method-name table.
	///
	/// This is a pure-managed, from-scratch port of powzix/ooz (https://github.com/powzix/ooz),
	/// a GPLv3-licensed reimplementation of RAD Game Tools/Epic's proprietary Oodle codec --
	/// see Compression/Oodle/*.cs for the ported decoder and its license/attribution header.
	/// No proprietary Oodle binary (e.g. oo2core) is required or used.
	///
	/// Only the "Kraken" sub-codec is implemented (the sub-codec actually used is selected
	/// per 256 KiB quantum by a header embedded in the compressed bytes themselves, not by
	/// the container -- see decoder_type in Compression/Oodle/KrakenDecoder.cs). Data
	/// compressed with Mermaid, Selkie, BitKnit or Leviathan is detected and rejected with a
	/// clear exception rather than silently mis-decoded. Kraken is Oodle's most common/default
	/// compressor and is what has been verified against real compressed data (see the ported
	/// decoder's test coverage); if Campaign Evolved turns out to ship a different sub-codec,
	/// decompression will fail with an explicit "sub-codec ... not implemented" error rather
	/// than producing corrupt output.
	/// </summary>
	public class OodleDecompressor
	{
		/// <summary>
		/// Returns true if this decompressor can handle the named compression method. Matches
		/// only "Oodle" (case-insensitively) -- a container could name something else (e.g. a
		/// non-Oodle codec entirely), and this class has no business claiming those.
		/// </summary>
		public bool SupportsMethod(string methodName)
		{
			return string.Equals(methodName, "Oodle", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Decompresses a single, self-contained, headerless/unframed compressed block --
		/// exactly the codec-native bytes as they appear in an IoStore compression block --
		/// into a freshly-allocated buffer of exactly <paramref name="uncompressedSize"/>
		/// bytes. Stateless: each call is independent, with no dictionary or state carried
		/// over from any previous call.
		/// </summary>
		/// <param name="methodName">The container's name for the compression method. Must be
		/// "Oodle" (see <see cref="SupportsMethod"/>).</param>
		/// <param name="compressedData">The raw compressed bytes for this block, with no
		/// framing, magic, or checksum beyond what the Oodle bitstream itself carries.</param>
		/// <param name="uncompressedSize">The exact decompressed size of this block, as
		/// recorded by the container.</param>
		public byte[] Decompress(string methodName, byte[] compressedData, int uncompressedSize)
		{
			if (!SupportsMethod(methodName))
				throw new NotSupportedException("OodleDecompressor does not support compression method \"" + methodName + "\".");
			if (compressedData == null)
				throw new ArgumentNullException(nameof(compressedData));
			if (uncompressedSize < 0)
				throw new ArgumentOutOfRangeException(nameof(uncompressedSize));

			byte[] output = new byte[uncompressedSize];
			if (uncompressedSize == 0)
				return output;
			if (compressedData.Length == 0)
				throw new ArgumentException("compressedData must not be empty when uncompressedSize > 0.", nameof(compressedData));

			unsafe
			{
				fixed (byte* srcPtr = compressedData)
				fixed (byte* dstPtr = output)
				{
					KrakenDecoderState.Decompress(srcPtr, compressedData.Length, dstPtr, uncompressedSize);
				}
			}
			return output;
		}
	}
}
