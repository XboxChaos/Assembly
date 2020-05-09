namespace Blamite.Blam
{
	/// <summary>
	///     Information about a single tag group in a cache file.
	/// </summary>
	public interface ITagGroup
	{
		/// <summary>
		///     The group's magic as a character string constant.
		/// </summary>
		int Magic { get; set; }

		/// <summary>
		///     The parent group's magic, or -1 if none.
		/// </summary>
		int ParentMagic { get; set; }

		/// <summary>
		///     The magic of the parent group's parent, or -1 if none.
		/// </summary>
		int GrandparentMagic { get; set; }

		/// <summary>
		///     The stringID describing the group's purpose, if available.
		/// </summary>
		StringID Description { get; set; }
	}
}