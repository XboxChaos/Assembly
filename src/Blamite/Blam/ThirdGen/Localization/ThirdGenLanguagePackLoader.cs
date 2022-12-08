using System.Collections.Generic;
using System.Linq;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Blam.Localization;

namespace Blamite.Blam.ThirdGen.Localization
{
	/// <summary>
	/// Implementation of <see cref="ILanguagePackLoader" /> for third-generation engines.
	/// </summary>
	public class ThirdGenLanguagePackLoader : ILanguagePackLoader
	{
		private readonly Dictionary<ITag, ThirdGenMultilingualStringList> _groupsByTag = new Dictionary<ITag, ThirdGenMultilingualStringList>();
		private readonly ThirdGenLanguageGlobals _languageGlobals;
		private readonly List<GameLanguage> _languages;
		private readonly EngineDescription _buildInfo;

		/// <summary>
		/// Initializes a new instance of the <see cref="ThirdGenLanguagePackLoader" /> class.
		/// No language packs will be loadable.
		/// </summary>
		public ThirdGenLanguagePackLoader()
		{
			_languages = new List<GameLanguage>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ThirdGenLanguagePackLoader" /> class.
		/// </summary>
		/// <param name="cacheFile">The cache file.</param>
		/// <param name="languageGlobals">The language globals.</param>
		/// <param name="buildInfo">Information about the cache file's engine.</param>
		/// <param name="reader">The stream to read from.</param>
		public ThirdGenLanguagePackLoader(ICacheFile cacheFile, ThirdGenLanguageGlobals languageGlobals,
			EngineDescription buildInfo, IReader reader)
		{
			_buildInfo = buildInfo;
			_languageGlobals = languageGlobals;
			_languages = languageGlobals.Languages.Where(l => l.StringCount != 0).Select(l => l.Language).ToList();
			LoadGroups(reader, cacheFile);
		}

		/// <summary>
		/// Gets the set of available languages.
		/// </summary>
		public IEnumerable<GameLanguage> AvailableLanguages
		{
			get { return _languages; }
		}

		/// <summary>
		/// Loads the language pack for a language.
		/// </summary>
		/// <param name="language">The language of the pack to load.</param>
		/// <param name="reader">The stream to read from.</param>
		/// <returns>
		/// The language pack that was loaded, or <c>null</c> if no pack exists for the language.
		/// </returns>
		public LanguagePack LoadLanguage(GameLanguage language, IReader reader)
		{
			if (_languageGlobals == null)
				return null;

			var data = _languageGlobals.Languages[(int)language];
			var table = data.LoadStrings(reader);
			var pack = new LanguagePack(language);
			foreach (var group in _groupsByTag.Values)
				pack.AddStringList(CreateList(group, (int)language, table));
			return pack;
		}

		/// <summary>
		/// Saves the language pack for a language.
		/// </summary>
		/// <param name="pack">The pack to save.</param>
		/// <param name="stream">The stream to write to.</param>
		public void SaveLanguage(LanguagePack pack, IStream stream)
		{
			// Null out all of the language's string lists
			var languageIndex = (int)pack.Language;
			ResetLanguage(languageIndex);

			// Merge lists from the pack
			var currentIndex = 0;
			foreach (var stringList in pack.StringLists)
				currentIndex += MergeStringList(stringList, languageIndex, currentIndex);

			// Rebuild the table and save
			var strings = pack.StringLists.SelectMany(l => _buildInfo.SortLocalesByStringID ? l.Strings.OrderBy(s => s.Key.Value).ToList() : l.Strings).ToList();
			_languageGlobals.Languages[languageIndex].SaveStrings(strings, stream);
			SaveGroups(stream);
		}

		/// <summary>
		/// Loads group info from a cache file.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="cacheFile">The cache file.</param>
		private void LoadGroups(IReader reader, ICacheFile cacheFile)
		{
			foreach (var tag in cacheFile.Tags.FindTagsByGroup("unic"))
			{
				var group = new ThirdGenMultilingualStringList(reader, tag, _buildInfo);
				_groupsByTag[tag] = group;
			}
		}

		/// <summary>
		/// Resets the string lists for a language so that all of them have no strings in them.
		/// </summary>
		/// <param name="languageIndex">Index of the language.</param>
		private void ResetLanguage(int languageIndex)
		{
			foreach (var group in _groupsByTag.Values)
			{
				if (languageIndex < group.Ranges.Length)
				{
					group.Ranges[languageIndex].StartIndex = 0;
					group.Ranges[languageIndex].Size = 0;
				}
			}
		}

		/// <summary>
		/// Merges a <see cref="LocalizedStringList"/> with string list information for a language.
		/// </summary>
		/// <param name="stringList">The string list.</param>
		/// <param name="languageIndex">Index of the language.</param>
		/// <param name="startIndex">The index where strings in the list will start at.</param>
		/// <returns>The number of strings in the string list.</returns>
		private int MergeStringList(LocalizedStringList stringList, int languageIndex, int startIndex)
		{
			// TODO: Assert that the tag exists
			ThirdGenMultilingualStringList group;
			_groupsByTag.TryGetValue(stringList.SourceTag, out group);
			if (group == null)
			{
				// Create a new string list
				// Assume there's going to be at least one group already and use that to get the number of languages...
				// TODO: Read this from build info or something
				var ranges = _groupsByTag.Values.First().Ranges.Select(r => new StringRange(0, 0)).ToArray();
				group = new ThirdGenMultilingualStringList(stringList.SourceTag, ranges, _buildInfo);
				_groupsByTag[stringList.SourceTag] = group;
			}
			group.Ranges[languageIndex].StartIndex = startIndex;
			group.Ranges[languageIndex].Size = stringList.Strings.Count;
			return stringList.Strings.Count;
		}

		/// <summary>
		/// Saves string list info back to the file.
		/// </summary>
		/// <param name="writer">The stream to write to.</param>
		private void SaveGroups(IWriter writer)
		{
			foreach (var group in _groupsByTag.Values)
			{
				// TODO: Assert that the group's tag exists, and skip it if not
				group.SaveChanges(writer);
			}
		}

		/// <summary>
		/// Creates a <see cref="LocalizedStringList"/> from a <see cref="ThirdGenMultilingualStringList"/>.
		/// </summary>
		/// <param name="group">The string list to load from.</param>
		/// <param name="languageIndex">Index of the language.</param>
		/// <param name="table">The string table to load from.</param>
		/// <returns>The created string list.</returns>
		private static LocalizedStringList CreateList(ThirdGenMultilingualStringList group, int languageIndex, List<LocalizedString> table)
		{
			var startIndex = group.Ranges[languageIndex].StartIndex;
			var size = group.Ranges[languageIndex].Size;
			var strings = table.Skip(startIndex).Take(size).ToList();
			return new LocalizedStringList(group.Tag, strings);
		}
	}
}