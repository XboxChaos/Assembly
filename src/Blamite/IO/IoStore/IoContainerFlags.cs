using System;

namespace Blamite.IO.IoStore
{
	/// <summary>
	///     Describes which optional container features a .utoc file makes use of.
	/// </summary>
	[Flags]
	public enum IoContainerFlags
	{
		/// <summary>
		///     No optional features are in use.
		/// </summary>
		None = 0,

		/// <summary>
		///     At least some of the container's blocks are compressed.
		/// </summary>
		Compressed = 0x1,

		/// <summary>
		///     The container's blocks are AES encrypted. Campaign Evolved never sets this.
		/// </summary>
		Encrypted = 0x2,

		/// <summary>
		///     The container carries signature hashes between the compression method names and the
		///     directory index.
		/// </summary>
		Signed = 0x4,

		/// <summary>
		///     The container carries a directory index mapping file paths to chunks.
		/// </summary>
		Indexed = 0x8
	}
}
