using System.Collections.Generic;
using Blamite.IO;

namespace Blamite.Blam.LanguagePack
{
	/// <summary>
	///     A pack of localized strings.
	/// </summary>
	public interface ILanguagePack
	{
		/// <summary>
		///     Gets the language that the pack targets.
		/// </summary>
		GameLanguage Language { get; }

		/// <summary>
		///     Gets the string lists belonging to the language pack.
		/// </summary>
		IList<LocalizedStringList> StringLists { get; }

		/// <summary>
		///     Saves changes made to the language pack.
		/// </summary>
		/// <param name="stream">The stream to manipulate.</param>
		void SaveChanges(IStream stream);
	}
}