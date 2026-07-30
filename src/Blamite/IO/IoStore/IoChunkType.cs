namespace Blamite.IO.IoStore
{
	/// <summary>
	///     The kind of data held by an IoStore chunk, stored in the last byte of an <see cref="IoChunkId" />.
	/// </summary>
	/// <remarks>
	///     Only the values actually observed in Campaign Evolved containers are named. Any other
	///     value is passed through unchanged by <see cref="IoChunkId.RawType" /> - an unnamed type
	///     is not an error, just a chunk this library has nothing to say about yet.
	/// </remarks>
	public enum IoChunkType : byte
	{
		/// <summary>
		///     Not a valid chunk type.
		/// </summary>
		Invalid = 0,

		/// <summary>
		///     A cooked Unreal package, which is what a .uasset file holds.
		/// </summary>
		ExportBundleData = 1,

		/// <summary>
		///     Bulk data attached to a package, which is what a .ubulk file holds.
		/// </summary>
		BulkData = 2,

		/// <summary>
		///     Bulk data which is only loaded on demand.
		/// </summary>
		OptionalBulkData = 3,

		/// <summary>
		///     Bulk data which is mapped into memory rather than read.
		/// </summary>
		MemoryMappedBulkData = 4,

		/// <summary>
		///     The container's own header chunk. At most one of these exists per container.
		/// </summary>
		ContainerHeader = 6,

		/// <summary>
		///     A library describing the shaders held by the container.
		/// </summary>
		ShaderCodeLibrary = 8,

		/// <summary>
		///     A single compiled shader.
		/// </summary>
		ShaderCode = 9
	}
}
