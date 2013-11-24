using System.Collections.Generic;

namespace Blamite.Patching
{
	/// <summary>
	///     [DEPRECATED] Represents a set of changes that should be made to a language in a cache file.
	/// </summary>
	public class LanguageChange
	{
		public LanguageChange(byte index)
		{
			LanguageIndex = index;
			LocaleChanges = new List<LocaleChange>();
		}

		/// <summary>
		///     The index of the language that the changes apply to.
		/// </summary>
		public byte LanguageIndex { get; private set; }

		/// <summary>
		///     Changes that should be made to locales in the language.
		/// </summary>
		public List<LocaleChange> LocaleChanges { get; private set; }
	}
}