namespace Blamite.Blam.Resources
{
	/// <summary>
	///     Resource page compression methods.
	/// </summary>
	public enum ResourcePageCompression
	{
		None,
		Deflate,
		Lzx
	}

	/// <summary>
	///     A page of resources in a cache file.
	/// </summary>
	public class ResourcePage
	{
		public ushort Salt { get; set; }

		/// <summary>
		///     Gets or sets the page's index.
		/// </summary>
		public int Index { get; set; }

		/// <summary>
		///     Gets or sets flags for the resource page.
		/// </summary>
		public byte Flags { get; set; }

		/// <summary>
		///     Gets or sets the path to the cache file that the resource is located in.
		///     Can be null if the resource is in the current file.
		/// </summary>
		public string FilePath { get; set; }

		/// <summary>
		///     Gets or sets the offset of the resource page from the start of the cache file's resource pool.
		/// </summary>
		public int Offset { get; set; }

		/// <summary>
		///     Gets or sets the uncompressed size of the resource page.
		/// </summary>
		public int UncompressedSize { get; set; }

		/// <summary>
		///     Gets or sets the method used to compress the resource page.
		/// </summary>
		public ResourcePageCompression CompressionMethod { get; set; }

		/// <summary>
		///     Gets or sets the compressed size of the resource page.
		///     Can be the same as <see cref="UncompressedSize" /> if the page is not compressed.
		/// </summary>
		public int CompressedSize { get; set; }

		public uint Checksum { get; set; }
		public byte[] Hash1 { get; set; }
		public byte[] Hash2 { get; set; }
		public byte[] Hash3 { get; set; }

		public int Unknown1 { get; set; }
		public int Unknown2 { get; set; }
		public int Unknown3 { get; set; }
	}
}