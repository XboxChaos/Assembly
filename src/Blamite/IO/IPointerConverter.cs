namespace Blamite.IO
{
	/// <summary>
	///     Interface for a class which converts pointers to and from file offsets.
	/// </summary>
	public interface IPointerConverter
	{
		/// <summary>
		///     Converts a pointer to a file offset.
		///     Note that this may be inaccurate if segments in the file have shifted around since the converter was created.
		/// </summary>
		/// <param name="pointer">The pointer to convert.</param>
		/// <returns>The pointer's equivalent file offset.</returns>
		uint PointerToOffset(long pointer);

		/// <summary>
		///     Converts a pointer to a file offset pointing to an area in the file.
		/// </summary>
		/// <param name="pointer">The pointer to convert.</param>
		/// <param name="areaStartOffset">The file offset of the start of the area in the file that the pointer points to.</param>
		/// <returns>The pointer's equivalent file offset.</returns>
		uint PointerToOffset(long pointer, uint areaStartOffset);

		/// <summary>
		///     Converts a file offset to a pointer.
		///     Note that this may be inaccurate if segments in the file have shifted around since the converter was created.
		/// </summary>
		/// <param name="offset">The offset to convert.</param>
		/// <returns>The offset's equivalent pointer.</returns>
		long OffsetToPointer(uint offset);

		/// <summary>
		///     Converts a file offset from an area of the file to a pointer.
		/// </summary>
		/// <param name="offset">The file offset to convert.</param>
		/// <param name="areaStartOffset">The start offset of the area in the file that the pointer should point to.</param>
		/// <returns>The offset's equivalent pointer.</returns>
		long OffsetToPointer(uint offset, uint areaStartOffset);
	}
}