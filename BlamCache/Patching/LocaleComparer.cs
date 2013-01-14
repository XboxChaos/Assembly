using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Patching
{
    public class LocaleComparer
    {
        /// <summary>
        /// Compares the locale data of two cache files and adds the results to a patch.
        /// </summary>
        /// <param name="originalFile">The unmodified cache file.</param>
        /// <param name="originalReader">A stream open on the unmodified cache file.</param>
        /// <param name="newFile">The modified cache file.</param>
        /// <param name="newReader">A stream open on the modified cache file.</param>
        /// <param name="output">The Patch to add results to.</param>
        public static void CompareLocales(ICacheFile originalFile, IReader originalReader, ICacheFile newFile, IReader newReader, Patch output)
        {
            if (originalFile.Languages.Count != newFile.Languages.Count)
                throw new InvalidOperationException("Cannot compare locales between cache files with different language counts.");

            // Compare each language
            for (int i = 0; i < originalFile.Languages.Count; i++)
            {
                // Compare the strings in the two language
                LanguageChange change = CompareLanguages((byte)i, originalFile.Languages[i], originalReader, newFile.Languages[i], newReader);

                // Only add the info if anything actually changed between the two languages
                if (change.LocaleChanges.Count > 0)
                    output.LanguageChanges.Add(change);
            }
        }

        private static LanguageChange CompareLanguages(byte index, ILanguage original, IReader originalReader, ILanguage modified, IReader newReader)
        {
            LocaleTable originalLocales = original.LoadStrings(originalReader);
            LocaleTable newLocales = modified.LoadStrings(newReader);

            // Compare each locale in the two tables
            LanguageChange result = new LanguageChange(index);
            for (int i = 0; i < originalLocales.Strings.Count; i++)
            {
                string oldValue = originalLocales.Strings[i].Value;
                string newValue = newLocales.Strings[i].Value;
                if (oldValue != newValue)
                    result.LocaleChanges.Add(new LocaleChange(i, newValue));
            }
            return result;
        }
    }
}
