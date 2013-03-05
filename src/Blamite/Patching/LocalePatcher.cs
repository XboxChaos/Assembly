using System;
using System.Collections.Generic;
using Blamite.Blam;
using Blamite.Blam.ThirdGen;
using Blamite.IO;

namespace Blamite.Patching
{
    /// <summary>
    /// [DEPRECATED] Provides static methods for patching locales in a cache file.
    /// </summary>
    public static class LocalePatcher
    {
        /// <summary>
        /// Writes changes to the language data in a cache file.
        /// Note that ICacheFile.SaveChanges() may need to be called after this.
        /// </summary>
        /// <param name="changes">The languages to change.</param>
        /// <param name="cacheFile">The cache file to write changes to.</param>
        /// <param name="stream">The stream to write changes to.</param>
        public static void WriteLanguageChanges(IEnumerable<LanguageChange> changes, ICacheFile cacheFile, IStream stream)
        {
            foreach (var change in changes)
                WriteLanguageChange(change, cacheFile, stream);
        }

        /// <summary>
        /// Writes changes to a language in a cache file.
        /// Note that ICacheFile.SaveChanges() may need to be called after this.
        /// </summary>
        /// <param name="changes">The changes to make to the language.</param>
        /// <param name="cacheFile">The cache file to write changes to.</param>
        /// <param name="stream">The stream to write changes to.</param>
        public static void WriteLanguageChange(LanguageChange changes, ICacheFile cacheFile, IStream stream)
        {
            if (changes.LanguageIndex >= cacheFile.Languages.Count)
                throw new ArgumentException("Language changes cannot be applied to undefined languages in a cache file");

            // Load the language table and apply the changes
			var language = cacheFile.Languages[changes.LanguageIndex];
			var table = language.LoadStrings(stream);
            MakeLocaleChanges(changes.LocaleChanges, table);

            // Write the table back to the file
            language.SaveStrings(stream, table);
        }

        /// <summary>
        /// Makes changes to a language's locales.
        /// </summary>
        /// <param name="changes">The changes to make to the language.</param>
        /// <param name="table">The locale table to make the changes on.</param>
        public static void MakeLocaleChanges(IEnumerable<LocaleChange> changes, LocaleTable table)
        {
			foreach (var change in changes)
                MakeLocaleChange(change, table);
        }

        /// <summary>
        /// Makes a change to a locale table.
        /// </summary>
        /// <param name="change">The locale change to make.</param>
        /// <param name="table">The locale table to make the change on.</param>
        public static void MakeLocaleChange(LocaleChange change, LocaleTable table)
        {
            if (change.Index < 0 || change.Index >= table.Strings.Count)
                throw new ArgumentException("Locale changes cannot be applied beyond the boundaries of the locale table");

            table.Strings[change.Index].Value = change.NewValue;
        }
    }
}
