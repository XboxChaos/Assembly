using System;
using System.Collections.Generic;
using Blamite.Blam;
using Blamite.IO;

namespace Blamite.Patching
{
	/// <summary>
	///     [DEPRECATED] Provides static methods for patching locales in a cache file.
	/// </summary>
	public static class LocalePatcher
	{
		/// <summary>
		///     Writes changes to the language data in a cache file.
		///     Note that ICacheFile.SaveChanges() may need to be called after this.
		/// </summary>
		/// <param name="changes">The languages to change.</param>
		/// <param name="cacheFile">The cache file to write changes to.</param>
		/// <param name="stream">The stream to write changes to.</param>
		public static void WriteLanguageChanges(IEnumerable<LanguageChange> changes, ICacheFile cacheFile, IStream stream)
		{
			foreach (LanguageChange change in changes)
				WriteLanguageChange(change, cacheFile, stream);
		}

		/// <summary>
		///     Writes changes to a language in a cache file.
		///     Note that ICacheFile.SaveChanges() may need to be called after this.
		/// </summary>
		/// <param name="changes">The changes to make to the language.</param>
		/// <param name="cacheFile">The cache file to write changes to.</param>
		/// <param name="stream">The stream to write changes to.</param>
		public static void WriteLanguageChange(LanguageChange changes, ICacheFile cacheFile, IStream stream)
		{
			throw new NotImplementedException("Legacy locale patching is not supported at the moment");
			/*if (changes.LanguageIndex >= cacheFile.Languages.Count)
                throw new ArgumentException("Language changes cannot be applied to undefined languages in a cache file");

            // Load the language table and apply the changes
			var language = cacheFile.Languages[changes.LanguageIndex];
			var table = language.LoadStrings(stream);
            MakeLocaleChanges(changes.LocaleChanges, table);

            // Write the table back to the file
            language.SaveStrings(stream, table);*/
		}
	}
}