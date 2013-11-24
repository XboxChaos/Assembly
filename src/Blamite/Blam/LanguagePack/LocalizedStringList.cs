using System.Collections.Generic;

namespace Blamite.Blam.LanguagePack
{
	/// <summary>
	///     Contains localized strings loaded from a tag.
	/// </summary>
	public class LocalizedStringList
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="LocalizedStringList" /> class.
		/// </summary>
		/// <param name="sourceTag">The source tag.</param>
		public LocalizedStringList(ITag sourceTag)
		{
			SourceTag = sourceTag;
			Strings = new List<LocalizedString>();
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="LocalizedStringList" /> class.
		///     Strings will be copied from another collection.
		/// </summary>
		/// <param name="sourceTag">The source tag.</param>
		/// <param name="strings">The strings.</param>
		public LocalizedStringList(ITag sourceTag, IEnumerable<LocalizedString> strings)
		{
			SourceTag = sourceTag;
			Strings = new List<LocalizedString>(strings);
		}

		/// <summary>
		///     Gets the tag that the string list was created from.
		/// </summary>
		public ITag SourceTag { get; private set; }

		/// <summary>
		///     Gets the strings in the list.
		/// </summary>
		public List<LocalizedString> Strings { get; private set; }
	}
}