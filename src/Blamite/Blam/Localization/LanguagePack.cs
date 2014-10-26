using System.Collections.Generic;
using Blamite.IO;

namespace Blamite.Blam.Localization
{
	/// <summary>
	/// A pack of localized strings.
	/// </summary>
	public class LanguagePack
	{
		private Dictionary<ITag, LocalizedStringList> _listsByTag = new Dictionary<ITag, LocalizedStringList>();

		/// <summary>
		/// Initializes a new instance of the <see cref="LanguagePack"/> class.
		/// </summary>
		/// <param name="language">The language that the pack targets.</param>
		public LanguagePack(GameLanguage language)
		{
			Language = language;
		}

		/// <summary>
		/// Gets the language that the pack targets.
		/// </summary>
		public GameLanguage Language { get; private set; }

		/// <summary>
		/// Gets the string lists belonging to the language pack.
		/// </summary>
		public IEnumerable<LocalizedStringList> StringLists
		{
			get { return _listsByTag.Values; }
		}

		/// <summary>
		/// Adds a localized string list to the language pack.
		/// </summary>
		/// <param name="list">The list to add.</param>
		public void AddStringList(LocalizedStringList list)
		{
			_listsByTag[list.SourceTag] = list;
		}

		/// <summary>
		/// Finds the <see cref="LocalizedStringList"/> corresponding to a tag.
		/// </summary>
		/// <param name="tag">The tag to get the string list for.</param>
		/// <returns>The string list if found, or <c>null</c> otherwise.</returns>
		public LocalizedStringList FindStringListByTag(ITag tag)
		{
			LocalizedStringList result;
			_listsByTag.TryGetValue(tag, out result);
			return result;
		}
	}
}