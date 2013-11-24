using System.Collections.Generic;
using Blamite.IO;

namespace Blamite.Blam.LanguagePack
{
	/// <summary>
	///     Interface for a class which manages language packs in cache files.
	/// </summary>
	public interface ILanguagePackLoader
	{
		/// <summary>
		///     Gets the set of available languages.
		/// </summary>
		IEnumerable<GameLanguage> AvailableLanguages { get; }

		/// <summary>
		///     Loads the language pack for a language.
		/// </summary>
		/// <param name="language">The language of the pack to load.</param>
		/// <param name="reader">The stream to read from.</param>
		/// <returns>The language pack that was loaded, or <c>null</c> if no pack exists for the language.</returns>
		ILanguagePack LoadLanguage(GameLanguage language, IReader reader);
	}
}