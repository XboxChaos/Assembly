using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.IO;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    public class ThirdGenMapGlobalsMeta
    {
        private List<ILanguage> _languages;

        public ThirdGenMapGlobalsMeta(StructureValueCollection values, IndexOffsetConverter converter, BuildInformation buildInfo)
        {
            _languages = LoadLanguages(values, converter, buildInfo);
        }

        private List<ILanguage> LoadLanguages(StructureValueCollection values, IndexOffsetConverter converter, BuildInformation buildInfo)
        {
            StructureValueCollection[] languageSet = values.GetArray("languages");

            var result = from language in languageSet
                         select new ThirdGenLanguage(language, converter, buildInfo);
            return result.ToList<ILanguage>();
        }

        public IList<ILanguage> Languages
        {
            get { return _languages.AsReadOnly(); }
        }
    }
}
