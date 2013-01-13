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
        private ThirdGenLanguageGlobals _languageGlobals;
        private AESKey _encryptionKey;
        private StructureLayout _pointerLayout;
        private IndexOffsetConverter _converter;
        private LocaleSymbolCollection _symbols;
        private int _pageSize;

        public ThirdGenLanguage(ThirdGenLanguageGlobals languageGlobals, StructureValueCollection values, IndexOffsetConverter converter, BuildInformation buildInfo)
        {
            _languageGlobals = languageGlobals;
            _pointerLayout = buildInfo.GetLayout("locale index table entry");
            _encryptionKey = buildInfo.LocaleKey;
            _symbols = buildInfo.LocaleSymbols;
            _converter = converter;
            _pageSize = buildInfo.LocaleAlignment;
            Load(values, converter);
        }

        public int StringCount { get; private set; }
        public int LocaleTableSize { get; private set; }
        public Pointer LocaleIndexTableLocation { get; set; }
        public Pointer LocaleDataLocation { get; set; }
        public byte[] IndexTableHash { get; private set; }
        public byte[] StringDataHash { get; private set; }

        public StructureValueCollection Serialize()
        {
            StructureValueCollection result = new StructureValueCollection();
            result.SetNumber("string count", (uint)StringCount);
            result.SetNumber("locale table size", (uint)LocaleTableSize);
            result.SetNumber("locale index table offset", _converter.PointerToRaw(LocaleIndexTableLocation));
            result.SetNumber("locale data index offset", _converter.PointerToRaw(LocaleDataLocation));
            if (IndexTableHash != null)
                result.SetRaw("index table hash", IndexTableHash);
            if (StringDataHash != null)
                result.SetRaw("string data hash", StringDataHash);
            return result;
        }

        private void Load(StructureValueCollection values, IndexOffsetConverter converter)
        {
            StringCount = (int)values.GetNumber("string count");
            LocaleTableSize = (int)values.GetNumber("locale table size");
            LocaleIndexTableLocation = new Pointer(values.GetNumber("locale index table offset"), converter);
            LocaleDataLocation = new Pointer(values.GetNumber("locale data index offset"), converter);

            // H3 beta doesn't have hashes
            if (values.HasRaw("index table hash"))
                IndexTableHash = values.GetRaw("index table hash");
            if (values.HasRaw("string data hash"))
                StringDataHash = values.GetRaw("string data hash");
        }

        public LocaleTable LoadStrings(IReader reader)
        {
            LocaleTable result = new LocaleTable(this);

            byte[] stringData = ReadLocaleData(reader);
            using (EndianReader stringReader = new EndianReader(new MemoryStream(stringData), Endian.BigEndian))
            {
                reader.SeekTo(LocaleIndexTableLocation.AsOffset());

                // Read each locale
                for (int i = 0; i < StringCount; i++)
                {
                    // Read the offset and stringID
                    StringID id;
                    int offset;
                    ReadLocalePointer(reader, out id, out offset);

                    if (offset >= stringReader.Length)
                        break; // Bad table - bail out so we don't end up in a huge memory-hogging loop

                    stringReader.SeekTo(offset);
                    string locale = stringReader.ReadUTF8();
                    result.Strings.Add(new Locale(id, locale));
                    //result.Add(ReplaceSymbols(locale));
                }
            }
            return result;
        }

        public void SaveStrings(IStream stream, LocaleTable locales)
        {
            MemoryStream offsetData = new MemoryStream();
            MemoryStream stringData = new MemoryStream();
            IWriter offsetWriter = new EndianWriter(offsetData, Endian.BigEndian);
            IWriter stringWriter = new EndianWriter(stringData, Endian.BigEndian);

            try
            {
                // Write the string and offset data to buffers
                foreach (Locale locale in locales.Strings)
                {
                    WriteLocalePointer(offsetWriter, locale.ID, (int)stringWriter.Position);
                    stringWriter.WriteUTF8(locale.Value);
                }

                // Round the size of the string data up to the nearest multiple of 0x10 (AES block size)
                stringData.SetLength((stringData.Position + 0xF) & ~0xF);

                // Update the two locale data hashes if we need to
                // (the hash arrays are set to null if the build doesn't need them)
                if (IndexTableHash != null)
                    IndexTableHash = SHA1.Transform(offsetData.GetBuffer(), 0, (int)offsetData.Length);
                if (StringDataHash != null)
                    StringDataHash = SHA1.Transform(stringData.GetBuffer(), 0, (int)stringData.Length);

                // Make sure there's free space for the offset table and then write it to the file
                LocaleDataLocation += StreamUtil.MakeFreeSpace(stream, LocaleIndexTableLocation.AsOffset(), LocaleDataLocation.AsOffset(), offsetData.Length, _pageSize);
                stream.SeekTo(LocaleIndexTableLocation.AsOffset());
                stream.WriteBlock(offsetData.GetBuffer(), 0, (int)offsetData.Length);

                // Encrypt the string data if necessary
                byte[] strings = stringData.GetBuffer();
                if (_encryptionKey != null)
                    strings = AES.Encrypt(strings, 0, (int)stringData.Length, _encryptionKey.Key, _encryptionKey.IV);

                // Make free space for the string data
                uint oldDataEnd = (uint)((LocaleDataLocation.AsOffset() + LocaleTableSize + _pageSize - 1) & ~(_pageSize - 1)); // Add the old table size and round it up
                StreamUtil.MakeFreeSpace(stream, LocaleDataLocation.AsOffset(), oldDataEnd, stringData.Length, _pageSize);
                LocaleTableSize = (int)stringData.Length;

                // Write it to the file
                stream.SeekTo(LocaleDataLocation.AsOffset());
                stream.WriteBlock(strings, 0, (int)stringData.Length);

                // Update the string count and recalculate the language table offsets
                StringCount = locales.Strings.Count;

                int localePointerSize = (int)(offsetData.Length / locales.Strings.Count);
                _languageGlobals.RecalculateLanguageOffsets(localePointerSize);
            }
            finally
            {
                offsetWriter.Close();
                stringWriter.Close();
            }
        }

        private void ReadLocalePointer(IReader reader, out StringID id, out int offset)
        {
            StructureValueCollection values = StructureReader.ReadStructure(reader, _pointerLayout);
            id = new StringID((int)values.GetNumber("stringid"));
            offset = (int)values.GetNumber("offset");
        }

        private void WriteLocalePointer(IWriter writer, StringID id, int offset)
        {
            StructureValueCollection values = new StructureValueCollection();
            values.SetNumber("stringid", (uint)id.Value);
            values.SetNumber("offset", (uint)offset);
            StructureWriter.WriteStructure(values, _pointerLayout, writer);
        }

        private byte[] ReadLocaleData(IReader reader)
        {
            // Read the string data
            reader.SeekTo(LocaleDataLocation.AsOffset());
            byte[] stringData = reader.ReadBlock(LocaleTableSize);

            // Decrypt it if necessary
            if (_encryptionKey != null)
                stringData = AES.Decrypt(stringData, _encryptionKey.Key, _encryptionKey.IV);

            return stringData;
        }

        private string ReplaceSymbols(string locale)
        {
            // :)
            return _symbols.ReplaceSymbols(locale);
        }
    }
}
