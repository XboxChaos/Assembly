using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.LanguagePack;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.LanguagePack
{
	/// <summary>
	///     Implementation of <see cref="ILanguagePackLoader" /> for third-generation engines.
	/// </summary>
	public class ThirdGenLanguagePackLoader : ILanguagePackLoader
	{
		private readonly List<ThirdGenMultilingualStringList> _groups = new List<ThirdGenMultilingualStringList>();
		private readonly ThirdGenLanguageGlobals _languageGlobals;
		private readonly List<GameLanguage> _languages;

		/// <summary>
		///     Initializes a new instance of the <see cref="ThirdGenLanguagePackLoader" /> class.
		/// </summary>
		/// <param name="cacheFile">The cache file.</param>
		/// <param name="languageGlobals">The language globals.</param>
		/// <param name="buildInfo">Information about the cache file's engine.</param>
		/// <param name="reader">The stream to read from.</param>
		public ThirdGenLanguagePackLoader(ICacheFile cacheFile, ThirdGenLanguageGlobals languageGlobals,
			EngineDescription buildInfo, IReader reader)
		{
			_languageGlobals = languageGlobals;
			_languages = languageGlobals.Languages.Where(l => l.StringCount != 0).Select(l => l.Language).ToList();
			LoadGroups(reader, cacheFile, buildInfo);
		}

		/// <summary>
		///     Gets the set of available languages.
		/// </summary>
		public IEnumerable<GameLanguage> AvailableLanguages
		{
			get { return _languages; }
		}

		/// <summary>
		///     Loads the language pack for a language.
		/// </summary>
		/// <param name="language">The language of the pack to load.</param>
		/// <param name="reader">The stream to read from.</param>
		/// <returns>
		///     The language pack that was loaded, or <c>null</c> if no pack exists for the language.
		/// </returns>
		public ILanguagePack LoadLanguage(GameLanguage language, IReader reader)
		{
			ThirdGenLanguage data = _languageGlobals.Languages[(int) language];
			return new ThirdGenLanguagePack(data, _groups, reader);
		}

		/// <summary>
		///     Loads group info from a cache file.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="cacheFile">The cache file.</param>
		/// <param name="buildInfo">Information about the cache file's engine.</param>
		private void LoadGroups(IReader reader, ICacheFile cacheFile, EngineDescription buildInfo)
		{
			foreach (ITag tag in cacheFile.Tags.FindTagsByClass("unic"))
			{
				var group = new ThirdGenMultilingualStringList(reader, tag, buildInfo);
				_groups.Add(group);
			}
		}
	}
}