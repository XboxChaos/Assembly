using Blamite.Blam;

namespace Blamite.Injection
{
	/// <summary>
	///     Contains information about a tag that was extracted from a cache file.
	/// </summary>
	public class ExtractedTag
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="ExtractedTag" /> class.
		/// </summary>
		/// <param name="originalIndex">The original datum index of the tag.</param>
		/// <param name="originalAddress">The original address of the tag's data.</param>
		/// <param name="tagGroup">The tag's group ID.</param>
		/// <param name="name">The tag's name.</param>
		public ExtractedTag(DatumIndex originalIndex, uint originalAddress, int tagGroup, string name)
		{
			OriginalIndex = originalIndex;
			OriginalAddress = originalAddress;
			Group = tagGroup;
			Name = name;
		}

		/// <summary>
		///     Gets the original datum index of the tag.
		/// </summary>
		public DatumIndex OriginalIndex { get; private set; }

		/// <summary>
		///     Gets the original address of the tag's data.
		///     This can be used to find its data block in the tag container.
		/// </summary>
		public uint OriginalAddress { get; private set; }

		/// <summary>
		///     Gets the tag's group ID.
		/// </summary>
		public int Group { get; private set; }

		/// <summary>
		///     Gets the tag's name.
		/// </summary>
		public string Name { get; set; }
	}
}