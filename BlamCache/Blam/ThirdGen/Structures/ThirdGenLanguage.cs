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
        private AESKey _encryptionKey;
        private StructureLayout _pointerLayout;
        private IndexOffsetConverter _converter;
        private LocaleSymbolCollection _symbols;

        public ThirdGenLanguage(StructureValueCollection values, IndexOffsetConverter converter, BuildInformation buildInfo)
        {
            _pointerLayout = buildInfo.GetLayout("locale index table entry");
            _encryptionKey = buildInfo.LocaleKey;
            _symbols = buildInfo.LocaleSymbols;
            _converter = converter;
            Load(values, converter);
        }

        public int StringCount { get; private set; }
        public int LocaleTableSize { get; private set; }
        public Pointer LocaleIndexTableLocation { get; private set; }
        public Pointer LocaleDataLocation { get; private set; }

        public StructureValueCollection Serialize()
        {
            StructureValueCollection result = new StructureValueCollection();
            result.SetNumber("string count", (uint)StringCount);
            result.SetNumber("locale table size", (uint)LocaleTableSize);
            result.SetNumber("locale table index offset", _converter.PointerToRaw(LocaleIndexTableLocation));
            result.SetNumber("locale data index offset", _converter.PointerToRaw(LocaleDataLocation));
            return result;
        }

        private void Load(StructureValueCollection values, IndexOffsetConverter converter)
        {
            StringCount = (int)values.GetNumber("string count");
            LocaleTableSize = (int)values.GetNumber("locale table size");
            LocaleIndexTableLocation = new Pointer(values.GetNumber("locale index table offset"), converter);
            LocaleDataLocation = new Pointer(values.GetNumber("locale data index offset"), converter);
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
            MemoryStream stringData = new MemoryStream();
            IWriter stringWriter = new EndianWriter(stringData, Endian.BigEndian);

            // Write the offset table to the file and the string table to the buffer
            // TODO: Check that the offset table doesn't overflow
            stream.SeekTo(LocaleIndexTableLocation.AsOffset());
            foreach (Locale locale in locales.Strings)
            {
                WriteLocalePointer(stream, locale.ID, (int)stringWriter.Position);
                stringWriter.WriteUTF8(locale.Value);
            }

            // Round the size of the string data up to the nearest multiple of 0x10 (AES block size)
            stringData.SetLength((stringData.Length + 0xF) & ~0xF);

            // Encrypt it if necessary
            byte[] strings = stringData.GetBuffer();
            if (_encryptionKey != null)
                strings = AES.Encrypt(strings, _encryptionKey.Key, _encryptionKey.IV);

            // Make free space for the string data
            uint oldDataEnd = (uint)((LocaleDataLocation.AsOffset() + LocaleTableSize + 0xFFF) & ~0xFFF);
            MakeFreeSpace(stream, LocaleDataLocation.AsOffset(), oldDataEnd, strings.Length, LocalePageSize);
            LocaleTableSize = strings.Length;

            // Write it to the file
            stream.SeekTo(LocaleDataLocation.AsOffset());
            stream.WriteBlock(strings);
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

        /// <summary>
        /// Expands an area in a stream to ensure that it is large enough to hold a given amount of data.
        /// </summary>
        /// <param name="stream">The stream to expand.</param>
        /// <param name="startOffset">The start offset of the area that needs to be expanded.</param>
        /// <param name="originalEndOffset">The original end offset of the area that needs to be expanded.</param>
        /// <param name="requestedSize">The size of the data that needs to fit in the defined area.</param>
        /// <param name="pageSize">The size of each page that should be injected into the stream.</param>
        /// <returns>The number of bytes inserted into the stream at originalEndOffset.</returns>
        private static int MakeFreeSpace(IStream stream, long startOffset, long originalEndOffset, long requestedSize, int pageSize)
        {
            // Calculate the number of bytes that the requested size overflows the area by,
            // and then insert pages if necessary
            int overflow = (int)(startOffset + requestedSize - originalEndOffset);
            if (overflow > 0)
                return InsertPages(stream, originalEndOffset, overflow, pageSize);
            return 0;
        }

        /// <summary>
        /// Inserts pages into a stream so that a specified amount of data can fit, pushing everything past them back.
        /// </summary>
        /// <param name="stream">The stream to insert pages into.</param>
        /// <param name="offset">The offset to insert the pages at.</param>
        /// <param name="minSpace">The minimum amount of free space that needs to be available after the pages have been inserted.</param>
        /// <param name="pageSize">The size of each page to insert.</param>
        /// <returns>The number of bytes that were inserted into the stream at the specified offset.</returns>
        private static int InsertPages(IStream stream, long offset, int minSpace, int pageSize)
        {
            // Round the minimum space up to the next multiple of the page size
            minSpace = (minSpace + pageSize - 1) & ~(pageSize - 1);

            // Push the data back by that amount
            stream.SeekTo(offset);
            StreamUtil.Insert(stream, minSpace);

            return minSpace;
        }

        private const int LocalePageSize = 0x1000;
    }
}
