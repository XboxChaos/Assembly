using Blamite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Localization
{
	/// <summary>
	/// Wraps a language pack loader so that language packs can be cached and not loaded from disk each time.
	/// </summary>
	public class CachedLanguagePackLoader : ILanguagePackLoader
	{
		private ILanguagePackLoader _baseLoader;
		private Dictionary<GameLanguage, LanguagePack> _cachedPacks = new Dictionary<GameLanguage, LanguagePack>();

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedLanguagePackLoader"/> class.
		/// </summary>
		/// <param name="baseLoader">The loader to wrap.</param>
		public CachedLanguagePackLoader(ILanguagePackLoader baseLoader)
		{
			_baseLoader = baseLoader;
		}

		/// <summary>
		/// Determines whether or not a language pack is in the cache.
		/// </summary>
		/// <param name="language">The language to check.</param>
		/// <returns><c>true</c> if the language's pack is in the cache.</returns>
		public bool IsCached(GameLanguage language)
		{
			return _cachedPacks.ContainsKey(language);
		}

		/// <summary>
		/// Clears the cache. All language packs will be loaded from disk the next time they are requested.
		/// </summary>
		public void ClearCache()
		{
			_cachedPacks.Clear();
		}
		
		/// <summary>
		/// Saves all cached languages back to the file.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public void SaveAll(IStream stream)
		{
			foreach (var pack in _cachedPacks.Values)
				_baseLoader.SaveLanguage(pack, stream);
		}

		/// <summary>
		/// Gets the set of available languages.
		/// </summary>
		public IEnumerable<GameLanguage> AvailableLanguages
		{
			get { return _baseLoader.AvailableLanguages; }
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
			LanguagePack result;
			if (!_cachedPacks.TryGetValue(language, out result))
			{
				// Not in the cache - load it from disk and store it
				result = _baseLoader.LoadLanguage(language, reader);
				if (result != null)
					_cachedPacks[language] = result;
			}
			return result;
		}

		/// <summary>
		/// Saves the language pack for a language.
		/// </summary>
		/// <param name="pack">The pack to save.</param>
		/// <param name="stream">The stream to write to.</param>
		public void SaveLanguage(LanguagePack pack, IStream stream)
		{
			_cachedPacks[pack.Language] = pack;
			_baseLoader.SaveLanguage(pack, stream);
		}
	}
}
