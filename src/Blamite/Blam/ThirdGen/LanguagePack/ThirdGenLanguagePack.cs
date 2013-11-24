using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.LanguagePack;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.LanguagePack
{
	/// <summary>
	///     Implementation of <see cref="ILanguagePack" /> for third-gen engines.
	/// </summary>
	public class ThirdGenLanguagePack : ILanguagePack
	{
		private readonly List<ThirdGenMultilingualStringList> _groups;
		private readonly ThirdGenLanguage _language;
		private readonly int _languageIndex;
		private readonly List<LocalizedStringList> _stringLists = new List<LocalizedStringList>();

		/// <summary>
		///     Initializes a new instance of the <see cref="ThirdGenLanguagePack" /> class.
		/// </summary>
		/// <param name="language">The language data to use.</param>
		/// <param name="groups">The string groups to use.</param>
		/// <param name="reader">The stream to read from.</param>
		public ThirdGenLanguagePack(ThirdGenLanguage language, List<ThirdGenMultilingualStringList> groups, IReader reader)
		{
			_language = language;
			_languageIndex = (int) language.Language;

			_groups = new List<ThirdGenMultilingualStringList>(groups);
			SortGroups();

			BuildStringLists(reader);
		}

		/// <summary>
		///     Gets the language that the pack targets.
		/// </summary>
		public GameLanguage Language
		{
			get { return _language.Language; }
		}

		/// <summary>
		///     Gets the string lists belonging to the language pack.
		/// </summary>
		public IList<LocalizedStringList> StringLists { get; private set; }

		/// <summary>
		///     Saves changes made to the language pack.
		/// </summary>
		/// <param name="stream">The stream to manipulate.</param>
		public void SaveChanges(IStream stream)
		{
			UpdateGroups(stream);
			List<LocalizedString> strings = BuildStringTable();
			_language.SaveStrings(stream, strings);
		}

		/// <summary>
		///     Sorts the groups in the language pack in ascending order by their starting index.
		/// </summary>
		private void SortGroups()
		{
			_groups.Sort((g1, g2) =>
			{
				int index1 = g1.Ranges[_languageIndex].StartIndex;
				int index2 = g2.Ranges[_languageIndex].StartIndex;
				return index1.CompareTo(index2);
			});
		}

		/// <summary>
		///     Builds the string list table.
		/// </summary>
		/// <param name="reader">The stream to read strings from.</param>
		private void BuildStringLists(IReader reader)
		{
			List<LocalizedString> table = _language.LoadStrings(reader);

			// Extract each group's section of the string table into a separate list
			foreach (ThirdGenMultilingualStringList group in _groups)
			{
				int startIndex = group.Ranges[_languageIndex].StartIndex;
				int size = group.Ranges[_languageIndex].Size;
				List<LocalizedString> strings = table.Skip(startIndex).Take(size).ToList();
				var list = new LocalizedStringList(group.Tag, strings);
				_stringLists.Add(list);
			}
			StringLists = _stringLists.AsReadOnly();
		}

		/// <summary>
		///     Updates each string list and writes the data back to the cache file.
		/// </summary>
		/// <param name="writer">The stream to write to.</param>
		private void UpdateGroups(IWriter writer)
		{
			// Put the group lists back together sequentially
			int currentIndex = 0;
			for (int i = 0; i < _stringLists.Count; i++)
			{
				int size = _stringLists[i].Strings.Count;
				_groups[i].Ranges[_languageIndex].StartIndex = currentIndex;
				_groups[i].Ranges[_languageIndex].Size = size;
				_groups[i].SaveChanges(writer);
				currentIndex += size;
			}
		}

		/// <summary>
		///     Builds the string table from the group data.
		/// </summary>
		/// <returns>The strings that were read.</returns>
		private List<LocalizedString> BuildStringTable()
		{
			return StringLists.SelectMany(l => l.Strings).ToList();
		}
	}
}