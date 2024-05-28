namespace Blamite.Blam.Localization
{
	/// <summary>
	///     Languages that language packs can contain localized strings for.
	/// </summary>
	public enum GameLanguage
	{
		English,
		Japanese,
		German,
		French,
		Spanish,
		LatinAmericanSpanish,
		Italian,
		Korean,
		ChineseTrad,
		ChineseSimp,
		Portuguese,
		Polish,
		Russian, // H4 only
		Danish, // H4 only
		Finnish, // H4 only
		Dutch, // H4 only
		Norwegian // H4 only
	}

	/// <summary>
	/// Tools for doing things with the <see cref="GameLanguage"/> enum.
	/// </summary>
	public static class GameLanguageTools
	{
		/// <summary>
		/// Get the pretty name of a language.
		/// </summary>
		/// <param name="langauge">Langaue to get.</param>
		/// <returns>The pretty (proper case, spacing, etc) for a language.</returns>
		public static string GetPrettyName(GameLanguage langauge)
		{
			switch (langauge)
			{
				case GameLanguage.ChineseTrad:
					return "Chinese (Traditional)";
				case GameLanguage.ChineseSimp:
					return "Chinese (Simplified)";
				case GameLanguage.LatinAmericanSpanish:
					return "Spanish (Latin American)";

				// name is one word so
				default:
					return langauge.ToString();
			}
		}
	}
}