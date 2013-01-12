using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.IO;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    /// <summary>
    /// Loads language data from MATG and PATG tags.
    /// </summary>
    public class ThirdGenLanguageGlobals
    {
        private List<ThirdGenLanguage> _languages;

        public ThirdGenLanguageGlobals(StructureValueCollection values, IndexOffsetConverter converter, BuildInformation buildInfo)
        {
            _languages = LoadLanguages(values, converter, buildInfo);
        }

        public StructureValueCollection Serialize()
        {
            StructureValueCollection[] languageSet = new StructureValueCollection[_languages.Count];
            for (int i = 0; i < _languages.Count; i++)
                languageSet[i] = _languages[i].Serialize();

            StructureValueCollection result = new StructureValueCollection();
            result.SetArray("languages", languageSet);
            return result;
        }

        private List<ThirdGenLanguage> LoadLanguages(StructureValueCollection values, IndexOffsetConverter converter, BuildInformation buildInfo)
        {
            StructureValueCollection[] languageSet = values.GetArray("languages");

            var result = from language in languageSet
                         select new ThirdGenLanguage(language, converter, buildInfo);
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
