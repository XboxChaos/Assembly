using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.IO;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.Blam.Util;

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    /// <summary>
    /// Loads language data from MATG and PATG tags.
    /// </summary>
    public class ThirdGenLanguageGlobals
    {
        private ThirdGenCacheFile _cacheFile;
        private List<ThirdGenLanguage> _languages;
        private IndexOffsetConverter _converter;
        private int _alignment;

        public ThirdGenLanguageGlobals(ThirdGenCacheFile cacheFile, StructureValueCollection values, IndexOffsetConverter converter, BuildInformation buildInfo)
        {
            _cacheFile = cacheFile;
            _converter = converter;
            _languages = LoadLanguages(values, converter, buildInfo);
            _alignment = buildInfo.LocaleAlignment;
        }

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

        private List<ThirdGenLanguage> LoadLanguages(StructureValueCollection values, IndexOffsetConverter converter, BuildInformation buildInfo)
        {
            StructureValueCollection[] languageSet = values.GetArray("languages");

            var result = from language in languageSet
                         select new ThirdGenLanguage(this, language, converter, buildInfo);
            return result.ToList<ThirdGenLanguage>();
        }

        /// <summary>
        /// Recalculates locale data offsets in case tables have been resized.
        /// </summary>
        /// <param name="localePointerSize">The size of a locale pointer in bytes.</param>
        public void RecalculateLanguageOffsets(int localePointerSize)
        {
            // The tables after the first language are aligned to 0x1000-byte boundaries, in order by language,
            // and the offset table always precedes the string data for each language
            uint startOffset = _languages[0].LocaleIndexTableLocation.AsOffset();
            uint currentOffset = startOffset;
            foreach (ThirdGenLanguage language in _languages)
            {
                language.LocaleIndexTableLocation = new Pointer(_converter.OffsetToPointer(currentOffset), _converter);
                currentOffset += (uint)((language.StringCount * localePointerSize + _alignment - 1) & ~(_alignment - 1));

                language.LocaleDataLocation = new Pointer(_converter.OffsetToPointer(currentOffset), _converter);
                currentOffset += (uint)((language.LocaleTableSize + _alignment - 1) & ~(_alignment - 1));
            }

            // Recalculate the size of the locale data
            int newSize = (int)(currentOffset - startOffset);
            int sizeChange = newSize - _cacheFile.Info.LocaleDataSize;
            _cacheFile.Info.FileSize += (uint)sizeChange;
            _cacheFile.Info.LocaleDataSize = newSize;

            // Adjust the header in case the locale tables aren't at the end of the file
            ThirdGenHeader header = _cacheFile.FullHeader;

            // -- Tagname offsets
            if (startOffset <= header.FileNameDataLocation.AsOffset())
                header.FileNameDataLocation += sizeChange;
            if (startOffset <= header.FileNameIndexTableLocation.AsOffset())
                header.FileNameIndexTableLocation += sizeChange;
            
            // -- StringID offsets
            if (startOffset <= header.StringIDDataLocation.AsOffset())
                header.StringIDDataLocation += sizeChange;
            if (startOffset <= header.StringIDIndexTableLocation.AsOffset())
                header.StringIDIndexTableLocation += sizeChange;

            // -- Raw table offset and meta offset
            if (startOffset <= header.RawTableOffset)
                header.RawTableOffset += (uint)sizeChange;
            if (startOffset <= header.MetaOffset)
                header.MetaOffset += (uint)sizeChange;
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
