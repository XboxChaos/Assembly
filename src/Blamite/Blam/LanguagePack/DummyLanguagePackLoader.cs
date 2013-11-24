using System.Collections.Generic;
using System.Linq;
using Blamite.IO;

namespace Blamite.Blam.LanguagePack
{
	/// <summary>
	///     Dummy implementation of <see cref="ILanguagePackLoader" /> which claims no languages are available.
	/// </summary>
	public class DummyLanguagePackLoader : ILanguagePackLoader
	{
		/// <summary>
		///     Gets the set of available languages.
		/// </summary>
		public IEnumerable<GameLanguage> AvailableLanguages
		{
			get { return Enumerable.Empty<GameLanguage>(); }
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
			return null;
		}
	}
}