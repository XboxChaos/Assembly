using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.ThirdGen.Structures;
using Blamite.IO;
using Blamite.Flexibility;
using Blamite.Blam.Util;

namespace Blamite.Blam.ThirdGen.Structures
{
    /// <summary>
    /// Loads language data from MATG and PATG tags.
    /// </summary>
    public class ThirdGenLanguageGlobals
    {
        private List<ThirdGenLanguage> _languages;

        public ThirdGenLanguageGlobals(StructureValueCollection values, FileSegmenter segmenter, IPointerConverter localePointerConverter, BuildInformation buildInfo)
        {
            LocaleArea = new FileSegmentGroup(localePointerConverter);
            _languages = LoadLanguages(values, segmenter, buildInfo);
        }

        /// <summary>
        /// The locale area that was loaded.
        /// </summary>
        public FileSegmentGroup LocaleArea { get; private set; }

        /// <summary>
        /// Serializes the language data into a StructureValueCollection.
        /// </summary>
        /// <returns>The StructureValueCollection that was created from the language table.</returns>
        public StructureValueCollection Serialize()
        {
            StructureValueCollection[] languageSet = new StructureValueCollection[_languages.Count];
            for (int i = 0; i < _languages.Count; i++)
                languageSet[i] = _languages[i].Serialize();

            StructureValueCollection result = new StructureValueCollection();
            result.SetArray("languages", languageSet);
            return result;
        }

        private List<ThirdGenLanguage> LoadLanguages(StructureValueCollection values, FileSegmenter segmenter, BuildInformation buildInfo)
        {
            StructureValueCollection[] languageSet = values.GetArray("languages");

            var result = from language in languageSet
                         select new ThirdGenLanguage(language, segmenter, LocaleArea, buildInfo);
            return result.ToList<ThirdGenLanguage>();
        }

        /// <summary>
        /// The languages that were loaded.
        /// </summary>
        public IList<ThirdGenLanguage> Languages
        {
            get { return _languages.AsReadOnly(); }
        }
    }
}
