using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.IO;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Util;
using System.IO;


namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    public class ThirdGenLanguage : ILanguage
    {
        private BuildInformation _buildInfo;

        public ThirdGenLanguage(StructureValueCollection values, IndexOffsetConverter converter, BuildInformation buildInfo)
        {
            _buildInfo = buildInfo;
            Load(values, converter);
        }

        private void Load(StructureValueCollection values, IndexOffsetConverter converter)
        {
            StringCount = (int)values.GetNumber("string count");
            LocaleTableSize = (int)values.GetNumber("locale table size");
            LocaleIndexTableLocation = new Pointer(values.GetNumber("locale index table offset"), converter);
            LocaleDataLocation = new Pointer(values.GetNumber("locale data index offset"), converter);
        }

        public int StringCount { get; private set; }
        public int LocaleTableSize { get; private set; }
        public Pointer LocaleIndexTableLocation { get; private set; }
        public Pointer LocaleDataLocation { get; private set; }

        public List<string> LoadStrings(IReader reader)
        {
            int[] offsets = ReadLocaleOffsets(reader);
            IReader stringReader = ReadLocaleData(reader);

            // Read each string
            List<string> result = new List<string>();
            for (int i = 0; i < offsets.Length; i++)
            {
                stringReader.SeekTo(offsets[i]);
                string locale = stringReader.ReadUTF8();
                result.Add(ReplaceSymbols(locale));
            }
            return result;
        }

        private int[] ReadLocaleOffsets(IReader reader)
        {
            StructureLayout layout = _buildInfo.GetLayout("locale index table entry");

            reader.SeekTo(LocaleIndexTableLocation.AsOffset());
            int[] offsets = new int[StringCount];
            for (int i = 0; i < offsets.Length; i++)
            {
                StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
                offsets[i] = (int)values.GetNumber("offset");
            }
            return offsets;
        }

        private IReader ReadLocaleData(IReader reader)
        {
            reader.SeekTo(LocaleDataLocation.AsOffset());
            byte[] stringData = reader.ReadBlock(LocaleTableSize);
            if (_buildInfo.LocaleKey != null)
                stringData = AES.Decrypt(stringData, _buildInfo.LocaleKey.Key, _buildInfo.LocaleKey.IV);

            return new EndianReader(new MemoryStream(stringData), Endian.BigEndian);
        }

        private string ReplaceSymbols(string locale)
        {
            // :)
            return _buildInfo.LocaleSymbols.ReplaceSymbols(locale);
        }
    }
}
