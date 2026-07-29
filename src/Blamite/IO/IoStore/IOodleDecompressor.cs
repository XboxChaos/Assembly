namespace Blamite.IO.IoStore
{
	/// <summary>
	///     Expands a single compressed IoStore block into a buffer whose size is already known.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Every compressed block in a container records both its compressed and its
	///         uncompressed size, so the output size is never in doubt and no implementation needs
	///         to grow a buffer or guess. Blocks are independent of one another and no larger than
	///         the container's compression block size (64 KiB in practice), so implementations need
	///         no cross-call state.
	///     </para>
	///     <para>
	///         Campaign Evolved compresses with Oodle, but the method is named per container rather
	///         than assumed, so <paramref name="methodName" /> is handed to the implementation and a
	///         single decompressor can serve several codecs.
	///     </para>
	///     <para>
	///         Blocks whose method is 0 are stored verbatim and never reach a decompressor at all,
	///         which is why a container built entirely from stored blocks reads end to end with the
	///         default <see cref="UnsupportedOodleDecompressor" /> in place.
	///     </para>
	/// </remarks>
	public interface IOodleDecompressor
	{
		/// <summary>
		///     Determines whether this decompressor can expand blocks compressed with a given method.
		/// </summary>
		/// <param name="methodName">
		///     The compression method name taken from the container's method name table.
		/// </param>
		/// <returns><c>true</c> if <see cref="Decompress" /> can handle the method.</returns>
		bool SupportsMethod(string methodName);

		/// <summary>
		///     Expands one compressed block.
		/// </summary>
		/// <param name="methodName">
		///     The compression method name taken from the container's method name table.
		/// </param>
		/// <param name="compressedData">The block's bytes exactly as they appear in the .ucas file.</param>
		/// <param name="uncompressedSize">The exact number of bytes the block expands to.</param>
		/// <returns>
		///     The expanded block, which must be exactly <paramref name="uncompressedSize" /> bytes long.
		/// </returns>
		byte[] Decompress(string methodName, byte[] compressedData, int uncompressedSize);
	}
}
