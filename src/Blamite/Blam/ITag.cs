using Blamite.IO;

namespace Blamite.Blam
{
	/// <summary>
	///     Represents a single tag in a cache file.
	/// </summary>
	public interface ITag
	{
		/// <summary>
		///     The tag's group. Can be null.
		/// </summary>
		ITagGroup Group { get; set; }

		/// <summary>
		///     The pointer to the tag's metadata. Can be null.
		/// </summary>
		SegmentPointer MetaLocation { get; set; }

		/// <summary>
		///     The tag's datum index.
		/// </summary>
		DatumIndex Index { get; }

		/// <summary>
		///     The source of the tag's metadata.
		/// </summary>
		TagSource Source { get; set; }
	}
}