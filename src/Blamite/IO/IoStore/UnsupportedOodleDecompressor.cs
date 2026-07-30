using System;

namespace Blamite.IO.IoStore
{
	/// <summary>
	///     The decompressor an <see cref="IoStoreContainer" /> uses when no real one has been
	///     supplied. It reports that it supports nothing and throws if it is ever called.
	/// </summary>
	/// <remarks>
	///     Stored blocks never reach a decompressor, so this placeholder is enough to read a
	///     container built from stored blocks in full. Reading a compressed block requires a real
	///     Oodle implementation to be passed to the container instead.
	/// </remarks>
	public class UnsupportedOodleDecompressor : IOodleDecompressor
	{
		/// <summary>
		///     Gets the shared instance used as the default for containers with no decompressor.
		/// </summary>
		public static readonly UnsupportedOodleDecompressor Instance = new UnsupportedOodleDecompressor();

		/// <summary>
		///     Always reports that the method is unsupported.
		/// </summary>
		/// <param name="methodName">The compression method name to test.</param>
		/// <returns>Always <c>false</c>.</returns>
		public bool SupportsMethod(string methodName)
		{
			return false;
		}

		/// <summary>
		///     Always throws, explaining what needs to be supplied.
		/// </summary>
		/// <param name="methodName">The compression method name of the block.</param>
		/// <param name="compressedData">The block's compressed bytes.</param>
		/// <param name="uncompressedSize">The number of bytes the block expands to.</param>
		/// <returns>Never returns.</returns>
		/// <exception cref="NotSupportedException">Always thrown.</exception>
		public byte[] Decompress(string methodName, byte[] compressedData, int uncompressedSize)
		{
			throw new NotSupportedException(string.Format(
				"This container holds a block compressed with \"{0}\" ({1} bytes expanding to {2}), but no " +
				"decompressor for that method is available. Blamite ships no Oodle decoder; pass an " +
				"IOodleDecompressor implementation to the IoStoreContainer to read compressed blocks. " +
				"Blocks stored verbatim (method 0) do not need one and are readable as-is.",
				methodName ?? "(unnamed)", compressedData != null ? compressedData.Length : 0, uncompressedSize));
		}
	}
}
